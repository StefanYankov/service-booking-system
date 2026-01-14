using ServiceBookingSystem.Application.DTOs.Booking;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Core.Exceptions;

namespace ServiceBookingSystem.Application.Interfaces;

public interface IBookingService
{
    /// <summary>
    /// Creates a new booking request for a specific service.
    /// </summary>
    /// <param name="dto">The booking details (ServiceId, Date, Notes).</param>
    /// <param name="customerId">The ID of the customer making the booking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created booking details.</returns>
    /// <exception cref="EntityNotFoundException">If the service does not exist.</exception>
    /// <exception cref="InvalidOperationException">If the service is not active or the slot is unavailable.</exception>
    Task<BookingViewDto> CreateBookingAsync(BookingCreateDto dto, string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the time or notes of a pending booking.
    /// </summary>
    /// <param name="dto">The booking update details.</param>
    /// <param name="customerId">The ID of the customer updating the booking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated booking details.</returns>
    /// <exception cref="AuthorizationException">If the user is not the owner of the booking.</exception>
    /// <exception cref="InvalidOperationException">If the booking is not in a valid state to be updated.</exception>
    Task<BookingViewDto> UpdateBookingAsync(BookingUpdateDto dto, string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific booking by its ID.
    /// </summary>
    /// <param name="bookingId">The unique booking ID.</param>
    /// <param name="userId">The ID of the user requesting the booking (for authorization).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The booking details, or null if not found.</returns>
    Task<BookingViewDto?> GetBookingByIdAsync(string bookingId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of bookings for a specific customer.
    /// </summary>
    /// <param name="customerId">The customer's ID.</param>
    /// <param name="parameters">Paging and sorting options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged result of bookings.</returns>
    Task<PagedResult<BookingViewDto>> GetBookingsByCustomerAsync(string customerId, PagingAndSortingParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of bookings for a specific provider (services they own).
    /// </summary>
    /// <param name="providerId">The provider's ID.</param>
    /// <param name="parameters">Paging and sorting options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged result of bookings.</returns>
    Task<PagedResult<BookingViewDto>> GetBookingsByProviderAsync(string providerId, PagingAndSortingParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of bookings between a specific provider and customer.
    /// Used for the Provider to see history with a specific client.
    /// </summary>
    /// <param name="providerId">The provider's ID.</param>
    /// <param name="customerId">The customer's ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of bookings.</returns>
    Task<List<BookingViewDto>> GetBookingsByProviderAndCustomerAsync(string providerId, string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a pending booking (Provider only).
    /// </summary>
    /// <param name="bookingId">The booking ID.</param>
    /// <param name="providerId">The provider's ID (for authorization).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated booking details.</returns>
    /// <exception cref="AuthorizationException">If the user is not the provider of the service.</exception>
    /// <exception cref="InvalidOperationException">If the booking is not in a valid state to be confirmed.</exception>
    Task<BookingViewDto> ConfirmBookingAsync(string bookingId, string providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Declines a pending booking (Provider only).
    /// </summary>
    /// <param name="bookingId">The booking ID.</param>
    /// <param name="providerId">The provider's ID (for authorization).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated booking details.</returns>
    Task<BookingViewDto> DeclineBookingAsync(string bookingId, string providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a booking (Customer or Provider).
    /// </summary>
    /// <param name="bookingId">The booking ID.</param>
    /// <param name="userId">The ID of the user initiating the cancellation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated booking details.</returns>
    Task<BookingViewDto> CancelBookingAsync(string bookingId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a booking as completed (System or Provider).
    /// Usually called by a background job or manual provider action after the time has passed.
    /// </summary>
    /// <param name="bookingId">The booking ID.</param>
    /// <param name="userId">The ID of the user (if manual) or system identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated booking details.</returns>
    Task<BookingViewDto> CompleteBookingAsync(string bookingId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific user has at least one completed booking for a specific service.
    /// Used to validate if a user is allowed to leave a review.
    /// </summary>
    /// <param name="userId">The customer's ID.</param>
    /// <param name="serviceId">The service ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a completed booking exists, otherwise false.</returns>
    Task<bool> HasCompletedBookingAsync(string userId, int serviceId, CancellationToken cancellationToken = default);
}