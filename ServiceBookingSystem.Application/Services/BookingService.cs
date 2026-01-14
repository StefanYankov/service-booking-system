using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceBookingSystem.Application.DTOs.Booking;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Core.Constants;
using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.Application.Services;

public class BookingService : IBookingService
{
    private readonly ApplicationDbContext dbContext;
    private readonly ILogger<BookingService> logger;
    private readonly IServiceService serviceService;
    private readonly IAvailabilityService availabilityService;
    private readonly IUsersService usersService;
    private readonly INotificationService notificationService;

    public BookingService(
        ApplicationDbContext dbContext,
        ILogger<BookingService> logger,
        IServiceService serviceService,
        IAvailabilityService availabilityService,
        IUsersService usersService,
        INotificationService notificationService)
    {
        this.dbContext = dbContext;
        this.logger = logger;
        this.serviceService = serviceService;
        this.availabilityService = availabilityService;
        this.usersService = usersService;
        this.notificationService = notificationService;
    }

    /// <inheritdoc/>
    public async Task<BookingViewDto> CreateBookingAsync(BookingCreateDto dto, string customerId,
        CancellationToken cancellationToken = default)
    {
        this.logger
            .LogDebug("Attempting to create a new booking");
        ArgumentNullException.ThrowIfNull(dto);
        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new ArgumentException(ExceptionMessages.InvalidProviderId, nameof(customerId));
        }

        this.logger
            .LogDebug(
                "Attempting to create a booking for ServiceId {ServiceId} by CustomerId {CustomerId} at {BookingStart}",
                dto.ServiceId, customerId, dto.BookingStart);

        var serviceDto = await this.serviceService.GetServiceByIdAsync(dto.ServiceId, cancellationToken);
        if (serviceDto == null)
        {
            this.logger.LogWarning("Booking creation failed: Service {ServiceId} not found.", dto.ServiceId);
            throw new EntityNotFoundException(nameof(Service), dto.ServiceId);
        }

        if (!serviceDto.IsActive)
        {
            this.logger.LogWarning("Booking creation failed: Service {ServiceId} is not active.", dto.ServiceId);
            throw new ServiceNotActiveException(dto.ServiceId, serviceDto.Name);
        }

        // You are not authorized to book your own service.
        if (serviceDto.ProviderId == customerId)
        {
            this.logger.LogWarning(
                "Booking creation failed: Provider {ProviderId} attempted to book their own service {ServiceId}.",
                customerId, dto.ServiceId);
            throw new AuthorizationException(customerId, $"Book own Service {dto.ServiceId}");
        }

        var isAvailable = await this.availabilityService.IsSlotAvailableAsync(dto.ServiceId, dto.BookingStart,
            serviceDto.DurationInMinutes, cancellationToken);
        if (!isAvailable)
        {
            this.logger.LogWarning(
                "Booking creation failed: Slot unavailable for Service {ServiceId} at {BookingStart}.", dto.ServiceId,
                dto.BookingStart);
            throw new SlotUnavailableException(dto.ServiceId, dto.BookingStart);
        }

        var customerDto = await this.usersService.GetUserByIdAsync(customerId);
        if (customerDto == null)
        {
            this.logger.LogWarning("Booking creation failed: Customer {CustomerId} not found.", customerId);
            throw new EntityNotFoundException(nameof(ApplicationUser), customerId);
        }

        var booking = new Booking
        {
            ServiceId = dto.ServiceId,
            CustomerId = customerId,
            BookingStart = dto.BookingStart,
            Notes = dto.Notes,
            Status = BookingStatus.Pending
        };

        await this.dbContext.Bookings.AddAsync(booking, cancellationToken);

        try
        {
            await this.dbContext.SaveChangesAsync(cancellationToken);
            this.logger
                .LogInformation(
                    "Booking {BookingId} created successfully for Service {ServiceId} by Customer {CustomerId}.",
                    booking.Id, dto.ServiceId, customerId);
            
            // Reload booking with navigation properties for notification
            var bookingForNotification = await this.dbContext.Bookings
                .Include(b => b.Service)
                .ThenInclude(s => s.Provider)
                .Include(b => b.Customer)
                .FirstAsync(b => b.Id == booking.Id, cancellationToken);
            
            await this.notificationService.NotifyBookingCreatedAsync(bookingForNotification);
        }
        catch (Exception ex)
        {
            this.logger
                .LogError(ex, "Failed to save booking for Service {ServiceId}.",
                    dto.ServiceId);
            throw;
        }

        var bookingDto = new BookingViewDto
        {
            Id = booking.Id,
            ServiceId = booking.ServiceId,
            ServiceName = serviceDto.Name,
            ServicePrice = serviceDto.Price,
            CustomerId = booking.CustomerId,
            CustomerName = $"{customerDto.FirstName} {customerDto.LastName}",
            CustomerEmail = customerDto.Email,
            CustomerPhone = customerDto.PhoneNumber,
            ProviderId = serviceDto.ProviderId,
            ProviderName = serviceDto.ProviderName,
            BookingStart = booking.BookingStart,
            Status = booking.Status.ToString(),
            Notes = booking.Notes,
            CreatedOn = booking.CreatedOn
        };

        return bookingDto;
    }

    /// <inheritdoc/>
    public async Task<BookingViewDto> UpdateBookingAsync(BookingUpdateDto dto, string customerId,
        CancellationToken cancellationToken = default)
    {
        this.logger
            .LogDebug("Attempting to update a new booking");
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new ArgumentException(ExceptionMessages.InvalidProviderId, nameof(customerId));
        }

        this.logger
            .LogDebug(
                "Attempting to update a booking with ID {BookingId} by CustomerId {CustomerId}",
                dto.Id, customerId
            );

        var bookingToUpdate = await this.dbContext
            .Bookings
            .Include(b => b.Service)
            .Include(b => b.Service.Provider)
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Id == dto.Id, cancellationToken);

        if (bookingToUpdate == null)
        {
            this.logger
                .LogWarning("Update failed: Booking {BookingId} not found.",
                    dto.Id);
            throw new EntityNotFoundException(nameof(Booking), dto.Id);
        }

        if (bookingToUpdate.CustomerId != customerId)
        {
            this.logger
                .LogWarning("Update failed: User {UserId} tried to update booking {BookingId} owned by {OwnerId}.",
                    customerId, dto.Id, bookingToUpdate.CustomerId);
            throw new AuthorizationException(customerId, $"Update Booking {dto.Id}");
        }

        // Only allow updates if the booking is still active (Pending or Confirmed).
        if (bookingToUpdate.Status != BookingStatus.Pending && bookingToUpdate.Status != BookingStatus.Confirmed)
        {
            this.logger
                .LogWarning("Update failed: Booking {BookingId} is in state {Status} and cannot be updated.",
                    dto.Id, bookingToUpdate.Status);
            throw new InvalidBookingStateException(dto.Id, bookingToUpdate.Status.ToString(), "Update");
        }

        bool isReschedule = false;
        DateTime oldStart = bookingToUpdate.BookingStart;

        if (bookingToUpdate.BookingStart != dto.BookingStart)
        {
            this.logger
                .LogInformation("Rescheduling booking {BookingId} from {OldStart} to {NewStart}.",
                    dto.Id, bookingToUpdate.BookingStart, dto.BookingStart);

            var isAvailable = await this.availabilityService
                .IsSlotAvailableAsync(
                    bookingToUpdate.ServiceId,
                    dto.BookingStart,
                    bookingToUpdate.Service.DurationInMinutes,
                    cancellationToken);

            if (!isAvailable)
            {
                this.logger
                    .LogWarning("Update failed: New slot {NewStart} is unavailable.",
                        dto.BookingStart);
                throw new SlotUnavailableException(bookingToUpdate.ServiceId, dto.BookingStart);
            }

            bookingToUpdate.BookingStart = dto.BookingStart;
            isReschedule = true;

            if (bookingToUpdate.Status == BookingStatus.Confirmed)
            {
                this.logger
                    .LogInformation("Resetting booking {BookingId} status to Pending due to reschedule.",
                        dto.Id);
                bookingToUpdate.Status = BookingStatus.Pending;
            }
        }

        bookingToUpdate.Notes = dto.Notes;

        try
        {
            await this.dbContext.SaveChangesAsync(cancellationToken);
            this.logger
                .LogInformation("Booking {BookingId} updated successfully.",
                    dto.Id);
            
            if (isReschedule)
            {
                await this.notificationService.NotifyBookingRescheduledAsync(bookingToUpdate, oldStart);
            }
        }
        catch (Exception ex)
        {
            this.logger
                .LogError(ex, "Failed to update booking {BookingId}.",
                    dto.Id);
            throw;
        }

        var bookingDto = new BookingViewDto
        {
            Id = bookingToUpdate.Id,
            ServiceId = bookingToUpdate.ServiceId,
            ServiceName = bookingToUpdate.Service.Name,
            ServicePrice = bookingToUpdate.Service.Price,
            CustomerId = bookingToUpdate.CustomerId,
            CustomerName = $"{bookingToUpdate.Customer.FirstName} {bookingToUpdate.Customer.LastName}",
            CustomerEmail = bookingToUpdate.Customer.Email,
            CustomerPhone = bookingToUpdate.Customer.PhoneNumber,
            ProviderId = bookingToUpdate.Service.ProviderId,
            ProviderName = $"{bookingToUpdate.Service.Provider.FirstName} {bookingToUpdate.Service.Provider.LastName}",
            BookingStart = bookingToUpdate.BookingStart,
            Status = bookingToUpdate.Status.ToString(),
            Notes = bookingToUpdate.Notes,
            CreatedOn = bookingToUpdate.CreatedOn
        };

        return bookingDto;
    }

    /// <inheritdoc/>
    public async Task<BookingViewDto?> GetBookingByIdAsync(string bookingId, string userId,
        CancellationToken cancellationToken = default)
    {
        this.logger
            .LogDebug("Retrieving booking {BookingId} for user {UserId}",
                bookingId, userId);
        
        var booking = await this.dbContext
            .Bookings
            .AsNoTracking()
            .Include(b => b.Service)
            .ThenInclude(s => s.Provider)
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken: cancellationToken);

        if (booking is null)
        {
            return null;
        }

        if (userId != booking.CustomerId && userId != booking.Service.ProviderId)
        {
            return null;
        }

        var bookingDto = new BookingViewDto
        {
            Id = booking.Id,
            ServiceId = booking.ServiceId,
            ServiceName = booking.Service.Name,
            ServicePrice = booking.Service.Price,
            CustomerId = booking.CustomerId,
            CustomerName = $"{booking.Customer.FirstName} {booking.Customer.LastName}",
            CustomerEmail = booking.Customer.Email,
            CustomerPhone = booking.Customer.PhoneNumber,
            ProviderId = booking.Service.ProviderId,
            ProviderName = $"{booking.Service.Provider.FirstName} {booking.Service.Provider.LastName}",
            BookingStart = booking.BookingStart,
            Status = booking.Status.ToString(),
            Notes = booking.Notes,
            CreatedOn = booking.CreatedOn
        };

        return bookingDto;
    }

    /// <inheritdoc/>
    public async Task<PagedResult<BookingViewDto>> GetBookingsByCustomerAsync(string customerId, PagingAndSortingParameters parameters,
        CancellationToken cancellationToken = default)
    {
        this.logger
            .LogDebug("Retrieving bookings for customer {CustomerId}. Page: {Page}, Size: {Size}",
            customerId, parameters.PageNumber, parameters.PageSize);
        
        var query = this.dbContext.Bookings
            .AsNoTracking()
            .Include(b => b.Service)
            .ThenInclude(s => s.Provider)
            .Include(b => b.Customer)
            .Where(b => b.CustomerId == customerId);

        var isDescending = parameters.SortDirection?.ToLower() == "desc";

        query = parameters.SortBy?.ToLower() switch
        {
            "date" => isDescending 
                ? query.OrderByDescending(b => b.BookingStart) 
                : query.OrderBy(b => b.BookingStart),
            "status" => isDescending 
                ? query.OrderByDescending(b => b.Status) 
                : query.OrderBy(b => b.Status),
            "service" => isDescending 
                ? query.OrderByDescending(b => b.Service.Name) 
                : query.OrderBy(b => b.Service.Name),
            _ => query.OrderByDescending(b => b.BookingStart)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(b => new BookingViewDto
            {
                Id = b.Id,
                ServiceId = b.ServiceId,
                ServiceName = b.Service.Name,
                ServicePrice = b.Service.Price,
                CustomerId = b.CustomerId,
                CustomerName = $"{b.Customer.FirstName} {b.Customer.LastName}",
                CustomerEmail = b.Customer.Email,
                CustomerPhone = b.Customer.PhoneNumber,
                ProviderId = b.Service.ProviderId,
                ProviderName = $"{b.Service.Provider.FirstName} {b.Service.Provider.LastName}",
                BookingStart = b.BookingStart,
                Status = b.Status.ToString(),
                Notes = b.Notes,
                CreatedOn = b.CreatedOn
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<BookingViewDto>(items, totalCount, parameters.PageNumber, parameters.PageSize);
    }
    
    /// <inheritdoc/>
    public async Task<PagedResult<BookingViewDto>> GetBookingsByProviderAsync(string providerId, PagingAndSortingParameters parameters,
        CancellationToken cancellationToken = default)
    {
        this.logger
            .LogDebug("Retrieving bookings for provider {ProviderId}. Page: {Page}, Size: {Size}",
                providerId, parameters.PageNumber, parameters.PageSize);
        
        var query = this.dbContext.Bookings
            .AsNoTracking()
            .Include(b => b.Service)
            .ThenInclude(s => s.Provider)
            .Include(b => b.Customer)
            .Where(b => b.Service.ProviderId == providerId);

        var isDescending = parameters.SortDirection?.ToLower() == "desc";

        query = parameters.SortBy?.ToLower() switch
        {
            "date" => isDescending 
                ? query.OrderByDescending(b => b.BookingStart) 
                : query.OrderBy(b => b.BookingStart),
            "status" => isDescending 
                ? query.OrderByDescending(b => b.Status) 
                : query.OrderBy(b => b.Status),
            "service" => isDescending 
                ? query.OrderByDescending(b => b.Service.Name) 
                : query.OrderBy(b => b.Service.Name),
            "customer" => isDescending 
                ? query.OrderByDescending(b => b.Customer.LastName) 
                : query.OrderBy(b => b.Customer.LastName),
            _ => query.OrderByDescending(b => b.BookingStart)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(b => new BookingViewDto
            {
                Id = b.Id,
                ServiceId = b.ServiceId,
                ServiceName = b.Service.Name,
                ServicePrice = b.Service.Price,
                CustomerId = b.CustomerId,
                CustomerName = $"{b.Customer.FirstName} {b.Customer.LastName}",
                CustomerEmail = b.Customer.Email,
                CustomerPhone = b.Customer.PhoneNumber,
                ProviderId = b.Service.ProviderId,
                ProviderName = $"{b.Service.Provider.FirstName} {b.Service.Provider.LastName}",
                BookingStart = b.BookingStart,
                Status = b.Status.ToString(),
                Notes = b.Notes,
                CreatedOn = b.CreatedOn
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<BookingViewDto>(items, totalCount, parameters.PageNumber, parameters.PageSize);
    }

    /// <inheritdoc/>
    public async Task<List<BookingViewDto>> GetBookingsByProviderAndCustomerAsync(string providerId, string customerId,
        CancellationToken cancellationToken = default)
    {
        this.logger
            .LogDebug("Retrieving bookings between provider {ProviderId} and customer {CustomerId}",
                providerId, customerId);

        var bookings = await this.dbContext.Bookings
            .AsNoTracking()
            .Include(b => b.Service)
            .ThenInclude(s => s.Provider)
            .Include(b => b.Customer)
            .Where(b => b.Service.ProviderId == providerId && b.CustomerId == customerId)
            .OrderByDescending(b => b.BookingStart)
            .Select(b => new BookingViewDto
            {
                Id = b.Id,
                ServiceId = b.ServiceId,
                ServiceName = b.Service.Name,
                ServicePrice = b.Service.Price,
                CustomerId = b.CustomerId,
                CustomerName = $"{b.Customer.FirstName} {b.Customer.LastName}",
                CustomerEmail = b.Customer.Email,
                CustomerPhone = b.Customer.PhoneNumber,
                ProviderId = b.Service.ProviderId,
                ProviderName = $"{b.Service.Provider.FirstName} {b.Service.Provider.LastName}",
                BookingStart = b.BookingStart,
                Status = b.Status.ToString(),
                Notes = b.Notes,
                CreatedOn = b.CreatedOn
            })
            .ToListAsync(cancellationToken);

        return bookings;
    }

    /// <inheritdoc/>
    public async Task<BookingViewDto> ConfirmBookingAsync(string bookingId, string providerId,
        CancellationToken cancellationToken = default)
    {
        this.logger
            .LogDebug("Attempting to confirm booking {BookingId} by provider {ProviderId}",
                bookingId, providerId);

        var booking = await this.dbContext.Bookings
            .Include(b => b.Service)
            .Include(b => b.Service.Provider)
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        if (booking == null)
        {
            this.logger
                .LogWarning("Confirm failed: Booking {BookingId} not found.",
                    bookingId);
            throw new EntityNotFoundException(nameof(Booking), bookingId);
        }

        if (booking.Service.ProviderId != providerId)
        {
            this.logger
                .LogWarning("Confirm failed: User {UserId} is not the provider for booking {BookingId}.",
                    providerId, bookingId);
            throw new AuthorizationException(providerId, $"Confirm Booking {bookingId}");
        }

        if (booking.Status != BookingStatus.Pending)
        {
            this.logger
                .LogWarning("Confirm failed: Booking {BookingId} is in state {Status}.",
                    bookingId, booking.Status);
            throw new InvalidBookingStateException(bookingId, booking.Status.ToString(), "Confirm");
        }

        booking.Status = BookingStatus.Confirmed;
        await this.dbContext.SaveChangesAsync(cancellationToken);
        
        this.logger
            .LogInformation("Booking {BookingId} confirmed by provider {ProviderId}.",
                bookingId, providerId);
        
        await this.notificationService.NotifyBookingConfirmedAsync(booking);

        var dto = new BookingViewDto
        {
            Id = booking.Id,
            ServiceId = booking.ServiceId,
            ServiceName = booking.Service.Name,
            ServicePrice = booking.Service.Price,
            CustomerId = booking.CustomerId,
            CustomerName = $"{booking.Customer.FirstName} {booking.Customer.LastName}",
            CustomerEmail = booking.Customer.Email,
            CustomerPhone = booking.Customer.PhoneNumber,
            ProviderId = booking.Service.ProviderId,
            ProviderName = $"{booking.Service.Provider.FirstName} {booking.Service.Provider.LastName}",
            BookingStart = booking.BookingStart,
            Status = booking.Status.ToString(),
            Notes = booking.Notes,
            CreatedOn = booking.CreatedOn
        };
        return dto;
    }

    /// <inheritdoc/>
    public async Task<BookingViewDto> DeclineBookingAsync(string bookingId, string providerId,
        CancellationToken cancellationToken = default)
    {
        this.logger
            .LogDebug("Attempting to decline booking {BookingId} by provider {ProviderId}",
                bookingId, providerId);

        var booking = await this.dbContext.Bookings
            .Include(b => b.Service)
            .Include(b => b.Service.Provider)
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        if (booking == null)
        {
            this.logger
                .LogWarning("Decline failed: Booking {BookingId} not found.",
                bookingId);
            throw new EntityNotFoundException(nameof(Booking), bookingId);
        }

        if (booking.Service.ProviderId != providerId)
        {
            this.logger
                .LogWarning("Decline failed: User {UserId} is not the provider for booking {BookingId}.",
                providerId, bookingId);
            throw new AuthorizationException(providerId, $"Decline Booking {bookingId}");
        }

        if (booking.Status != BookingStatus.Pending)
        {
            this.logger
                .LogWarning("Decline failed: Booking {BookingId} is in state {Status}.",
                bookingId, booking.Status);
            throw new InvalidBookingStateException(bookingId, booking.Status.ToString(), "Decline");
        }

        booking.Status = BookingStatus.Declined;
        await this.dbContext.SaveChangesAsync(cancellationToken);
        
        this.logger
            .LogInformation("Booking {BookingId} declined by provider {ProviderId}.",
                bookingId, providerId);
        
        await this.notificationService.NotifyBookingDeclinedAsync(booking);

        var dto = new BookingViewDto
        {
            Id = booking.Id,
            ServiceId = booking.ServiceId,
            ServiceName = booking.Service.Name,
            ServicePrice = booking.Service.Price,
            CustomerId = booking.CustomerId,
            CustomerName = $"{booking.Customer.FirstName} {booking.Customer.LastName}",
            CustomerEmail = booking.Customer.Email,
            CustomerPhone = booking.Customer.PhoneNumber,
            ProviderId = booking.Service.ProviderId,
            ProviderName = $"{booking.Service.Provider.FirstName} {booking.Service.Provider.LastName}",
            BookingStart = booking.BookingStart,
            Status = booking.Status.ToString(),
            Notes = booking.Notes,
            CreatedOn = booking.CreatedOn
        };
        return dto;
    }

    /// <inheritdoc/>
    public async Task<BookingViewDto> CancelBookingAsync(string bookingId, string userId,
        CancellationToken cancellationToken = default)
    {
        this.logger
            .LogDebug("Attempting to cancel booking {BookingId} by user {UserId}",
                bookingId, userId);

        var booking = await this.dbContext.Bookings
            .Include(b => b.Service)
            .Include(b => b.Service.Provider)
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        if (booking == null)
        {
            this.logger.LogWarning("Cancel failed: Booking {BookingId} not found.", bookingId);
            throw new EntityNotFoundException(nameof(Booking), bookingId);
        }

        // Allow cancellation if user is the Customer OR the Provider
        if (booking.CustomerId != userId && booking.Service.ProviderId != userId)
        {
            this.logger
                .LogWarning("Cancel failed: User {UserId} is not authorized to cancel booking {BookingId}.",
                    userId, bookingId);
            throw new AuthorizationException(userId, $"Cancel Booking {bookingId}");
        }

        if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Confirmed)
        {
            this.logger
                .LogWarning("Cancel failed: Booking {BookingId} is in state {Status}.",
                    bookingId, booking.Status);
            throw new InvalidBookingStateException(bookingId, booking.Status.ToString(), "Cancel");
        }

        booking.Status = BookingStatus.Cancelled;
        await this.dbContext.SaveChangesAsync(cancellationToken);
        
        this.logger
            .LogInformation("Booking {BookingId} cancelled by user {UserId}.",
            bookingId, userId);
        
        // Determine who cancelled to notify the other party
        bool cancelledByProvider = (userId == booking.Service.ProviderId);
        await this.notificationService.NotifyBookingCancelledAsync(booking, cancelledByProvider);

        var dto = new BookingViewDto
        {
            Id = booking.Id,
            ServiceId = booking.ServiceId,
            ServiceName = booking.Service.Name,
            ServicePrice = booking.Service.Price,
            CustomerId = booking.CustomerId,
            CustomerName = $"{booking.Customer.FirstName} {booking.Customer.LastName}",
            CustomerEmail = booking.Customer.Email,
            CustomerPhone = booking.Customer.PhoneNumber,
            ProviderId = booking.Service.ProviderId,
            ProviderName = $"{booking.Service.Provider.FirstName} {booking.Service.Provider.LastName}",
            BookingStart = booking.BookingStart,
            Status = booking.Status.ToString(),
            Notes = booking.Notes,
            CreatedOn = booking.CreatedOn
        };
        return dto;
    }

    /// <inheritdoc/>
    public async Task<BookingViewDto> CompleteBookingAsync(string bookingId, string userId,
        CancellationToken cancellationToken = default)
    {
        this.logger
            .LogDebug("Attempting to complete booking {BookingId} by user {UserId}",
                bookingId, userId);

        var booking = await this.dbContext.Bookings
            .Include(b => b.Service)
            .Include(b => b.Service.Provider)
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        if (booking == null)
        {
            this.logger
                .LogWarning("Complete failed: Booking {BookingId} not found.",
                    bookingId);
            throw new EntityNotFoundException(nameof(Booking), bookingId);
        }

        // Only the Provider can mark a booking as completed.
        if (booking.Service.ProviderId != userId)
        {
            this.logger
                .LogWarning("Complete failed: User {UserId} is not the provider for booking {BookingId}.",
                    userId, bookingId);
            throw new AuthorizationException(userId, $"Complete Booking {bookingId}");
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            this.logger
                .LogWarning("Complete failed: Booking {BookingId} is in state {Status}.",
                    bookingId, booking.Status);
            throw new InvalidBookingStateException(bookingId, booking.Status.ToString(), "Complete");
        }

        if (booking.BookingStart > DateTime.UtcNow)
        {
            this.logger
                .LogWarning("Complete failed: Cannot complete a future booking {BookingId}.",
                    bookingId);
             
            throw new BookingTimeException(bookingId, booking.BookingStart, 
                $"Cannot mark booking '{bookingId}' as completed because it is in the future (Start: {booking.BookingStart})."
            );
        }

        booking.Status = BookingStatus.Completed;
        await this.dbContext.SaveChangesAsync(cancellationToken);
        
        this.logger
            .LogInformation("Booking {BookingId} completed by provider {ProviderId}.",
            bookingId, userId);

        var dto = new BookingViewDto
        {
            Id = booking.Id,
            ServiceId = booking.ServiceId,
            ServiceName = booking.Service.Name,
            ServicePrice = booking.Service.Price,
            CustomerId = booking.CustomerId,
            CustomerName = $"{booking.Customer.FirstName} {booking.Customer.LastName}",
            CustomerEmail = booking.Customer.Email,
            CustomerPhone = booking.Customer.PhoneNumber,
            ProviderId = booking.Service.ProviderId,
            ProviderName = $"{booking.Service.Provider.FirstName} {booking.Service.Provider.LastName}",
            BookingStart = booking.BookingStart,
            Status = booking.Status.ToString(),
            Notes = booking.Notes,
            CreatedOn = booking.CreatedOn
        };
        return dto;
    }

    /// <inheritdoc/>
    public async Task<bool> HasCompletedBookingAsync(string userId, int serviceId,
        CancellationToken cancellationToken = default)
    {
        this.logger
            .LogDebug("Checking if user {UserId} has completed booking for service {ServiceId}",
                userId, serviceId);
        
        var hasCompletedBooking = await this.dbContext.Bookings
            .AsNoTracking()
            .AnyAsync(b => 
                b.CustomerId == userId && 
                b.ServiceId == serviceId && 
                b.Status == BookingStatus.Completed, 
                cancellationToken);

        return hasCompletedBooking;
    }
}