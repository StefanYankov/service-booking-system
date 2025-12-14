using Microsoft.Extensions.Logging;
using ServiceBookingSystem.Application.DTOs.Booking;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Data.Contexts;

namespace ServiceBookingSystem.Application.Services;

public class BookingService : IBookingService
{
    private readonly ApplicationDbContext dbContext;
    private readonly ILogger<ServiceService> logger;
    private readonly IServiceService serviceService;
    private readonly IAvailabilityService availabilityService;
    private readonly IUsersService usersService;

    public BookingService(
        ApplicationDbContext dbContext,
        ILogger<ServiceService> logger,
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
    public async Task<BookingViewDto> CreateBookingAsync(BookingCreateDto dto, string customerId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<BookingViewDto> UpdateBookingAsync(BookingUpdateDto dto, string customerId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<BookingViewDto?> GetBookingByIdAsync(string bookingId, string userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<PagedResult<BookingViewDto>> GetBookingsByCustomerAsync(string customerId, PagingAndSortingParameters parameters,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<PagedResult<BookingViewDto>> GetBookingsByProviderAsync(string providerId, PagingAndSortingParameters parameters,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<BookingViewDto> ConfirmBookingAsync(string bookingId, string providerId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<BookingViewDto> DeclineBookingAsync(string bookingId, string providerId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<BookingViewDto> CancelBookingAsync(string bookingId, string userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<BookingViewDto> CompleteBookingAsync(string bookingId, string userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<bool> HasCompletedBookingAsync(string userId, int serviceId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}