using Microsoft.Extensions.Logging;
using ServiceBookingSystem.Application.DTOs.Service;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Core.Constants;
using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.Application.Services;

public class ServiceService : IServiceService
{
    private readonly ApplicationDbContext dbContext;
    private readonly ILogger<ServiceService> logger;
    private readonly ICategoryService categoryService;
    private readonly IUsersService usersService;

    public ServiceService(
        ApplicationDbContext dbContext,
        ILogger<ServiceService> logger,
        IUsersService usersService,
        ICategoryService categoryService)
    {
        this.dbContext = dbContext;
        this.logger = logger;
        this.usersService = usersService;
        this.categoryService = categoryService;
    }

    /// <inheritdoc/>
    public async Task<ServiceViewDto> CreateServiceAsync(ServiceCreateDto dto, string providerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (string.IsNullOrWhiteSpace(providerId))
        {
            throw new ArgumentException(ExceptionMessages.InvalidProviderId, nameof(providerId));
        }
        
        logger.LogDebug(
            "Attempting to create a new Service with Name: {ServiceName} to a provider with an ID {providerId}",
            dto.Name, providerId);
        
        var providerDto = await usersService.GetUserByIdAsync(providerId);
        if (providerDto == null)
        {
            logger.LogWarning("User {UserId} attempted to create a service but the user does not exist.", providerId);
            throw new EntityNotFoundException(nameof(ApplicationUser), providerId);
        }
        
        if (!providerDto.Roles.Contains("Provider"))
        {
            logger.LogWarning("User {UserId} attempted to create a service without being in the 'Provider' role.", providerId);
            throw new AuthorizationException(providerId, "CreateService");
        }
        
        var categoryDto = await categoryService.GetByIdAsync(dto.CategoryId, cancellationToken);
        
        if (categoryDto == null)
        {
            logger.LogWarning("Attempted to create service with non-existent CategoryId {CategoryId}", dto.CategoryId);
            throw new EntityNotFoundException(nameof(Category), dto.CategoryId);
        }
        
        var service = new Service
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            DurationInMinutes = dto.DurationInMinutes,
            IsOnline = dto.IsOnline,
            StreetAddress = dto.StreetAddress,
            City = dto.City,
            PostalCode = dto.PostalCode,
            ProviderId = providerId,
            CategoryId = dto.CategoryId
        };
        
        await dbContext.Services.AddAsync(service, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Service {ServiceId} created with name {ServiceName} for provider {ProviderId}", service.Id, service.Name, providerId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,"Failed to create service with name {ServiceName}", dto.Name);
            throw;
        }
        
        var returnDto = new ServiceViewDto
        {
            Id = service.Id,
            Name = service.Name,
            Description = service.Description,
            Price = service.Price,
            DurationInMinutes = service.DurationInMinutes,
            IsOnline = service.IsOnline,
            StreetAddress = service.StreetAddress,
            City = service.City,
            PostalCode = service.PostalCode,
            IsActive = service.IsActive,
            ProviderId = service.ProviderId,
            ProviderName = $"{providerDto.FirstName} {providerDto.LastName}",
            CategoryId = service.CategoryId,
            CategoryName = categoryDto.Name
        };
        return returnDto;
    }

    /// <inheritdoc/>
    public async Task<ServiceViewDto> UpdateServiceAsync(ServiceUpdateDto dto, string providerId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteServiceAsync(int serviceId, string providerId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceViewDto?> GetServiceByIdAsync(int serviceId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<ServiceViewDto>> GetServicesByCategoryAsync(int categoryId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<ServiceViewDto>> GetServicesByProviderAsync(string providerId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}