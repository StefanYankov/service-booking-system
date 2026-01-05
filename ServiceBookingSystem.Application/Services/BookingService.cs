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

    public BookingService(
        ApplicationDbContext dbContext,
        ILogger<BookingService> logger,
        IServiceService serviceService,
        IAvailabilityService availabilityService,
        IUsersService usersService)
    {
        this.dbContext = dbContext;
        this.logger = logger;
        this.serviceService = serviceService;
        this.availabilityService = availabilityService;
        this.usersService = usersService;
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
            CustomerId = booking.CustomerId,
            CustomerName = $"{customerDto.FirstName} {customerDto.LastName}",
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
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<BookingViewDto?> GetBookingByIdAsync(string bookingId, string userId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<PagedResult<BookingViewDto>> GetBookingsByCustomerAsync(string customerId,
        PagingAndSortingParameters parameters,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<PagedResult<BookingViewDto>> GetBookingsByProviderAsync(string providerId,
        PagingAndSortingParameters parameters,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<BookingViewDto> ConfirmBookingAsync(string bookingId, string providerId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<BookingViewDto> DeclineBookingAsync(string bookingId, string providerId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<BookingViewDto> CancelBookingAsync(string bookingId, string userId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<BookingViewDto> CompleteBookingAsync(string bookingId, string userId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<bool> HasCompletedBookingAsync(string userId, int serviceId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}