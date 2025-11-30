using ServiceBookingSystem.Application.DTOs.Service;

namespace ServiceBookingSystem.Application.Interfaces;

public interface IServiceService
{
 Task<ServiceViewDto> CreateServiceAsync(ServiceCreateDto dto, string providerId, CancellationToken cancellationToken = default);

 Task<ServiceViewDto> UpdateServiceAsync(ServiceUpdateDto dto, string providerId, CancellationToken cancellationToken = default);

 Task DeleteServiceAsync(int serviceId, string providerId, CancellationToken cancellationToken = default);

 Task<ServiceViewDto?> GetServiceByIdAsync(int serviceId, CancellationToken cancellationToken = default);

 Task<IEnumerable<ServiceViewDto>> GetServicesByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);


 Task<IEnumerable<ServiceViewDto>> GetServicesByProviderAsync(string providerId, CancellationToken cancellationToken = default);
}