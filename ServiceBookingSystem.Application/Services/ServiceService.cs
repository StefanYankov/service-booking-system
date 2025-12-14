using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceBookingSystem.Application.DTOs.Service;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Core.Constants;
using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;
using ServiceBookingSystem.Data.Extensions;

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
            logger.LogWarning("User {UserId} attempted to create a service without being in the 'Provider' role.",
                providerId);
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
            logger.LogInformation("Service {ServiceId} created with name {ServiceName} for provider {ProviderId}",
                service.Id, service.Name, providerId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create service with name {ServiceName}", dto.Name);
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
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(providerId))
        {
            throw new ArgumentException(ExceptionMessages.InvalidProviderId, nameof(providerId));
        }

        logger.LogDebug(
            "Attempting to update a new Service with Name: {ServiceName}, ID: {ServiceId} to a provider with an ID {providerId}",
            dto.Name, dto.Id, providerId);

        var serviceToUpdate = await dbContext.Services.FindAsync(new object[] { dto.Id }, cancellationToken);
        if (serviceToUpdate == null)
        {
            logger.LogWarning("Attempted to update non-existent Service {ServiceId}", dto.Id);
            throw new EntityNotFoundException(nameof(Service), dto.Id);
        }

        if (serviceToUpdate.ProviderId != providerId)
        {
            logger.LogWarning("User {UserId} attempted to update Service {ServiceId} owned by {OwnerId}", providerId,
                serviceToUpdate.Id, serviceToUpdate.ProviderId);
            throw new AuthorizationException(providerId, $"Update ServiceId '{serviceToUpdate.Id}'");
        }

        var categoryDto = await categoryService.GetByIdAsync(dto.CategoryId, cancellationToken);
        if (categoryDto == null)
        {
            logger.LogWarning("Attempted to update Service {ServiceId} with non-existent CategoryId {CategoryId}",
                serviceToUpdate.Id, dto.CategoryId);
            throw new EntityNotFoundException(nameof(Category), dto.CategoryId);
        }

        serviceToUpdate.Name = dto.Name;
        serviceToUpdate.Description = dto.Description;
        serviceToUpdate.Price = dto.Price;
        serviceToUpdate.DurationInMinutes = dto.DurationInMinutes;
        serviceToUpdate.CategoryId = dto.CategoryId;
        serviceToUpdate.IsOnline = dto.IsOnline;
        serviceToUpdate.IsActive = dto.IsActive;
        serviceToUpdate.StreetAddress = dto.StreetAddress;
        serviceToUpdate.City = dto.City;
        serviceToUpdate.PostalCode = dto.PostalCode;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Service {ServiceId} was successfully updated by provider {ProviderId}",
                serviceToUpdate.Id, providerId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update Service {ServiceId}", serviceToUpdate.Id);
            throw;
        }

        var providerDto = await usersService.GetUserByIdAsync(providerId);

        if (providerDto == null)
        {
            logger.LogError(
                "Data integrity issue: Provider {ProviderId} for Service {ServiceId} not found after update.",
                providerId, serviceToUpdate.Id);
            throw new InvalidOperationException(
                $"A data integrity issue was detected. Provider '{providerId}' could not be found.");
        }

        var returnDto = new ServiceViewDto
        {
            Id = serviceToUpdate.Id,
            Name = serviceToUpdate.Name,
            Description = serviceToUpdate.Description,
            Price = serviceToUpdate.Price,
            DurationInMinutes = serviceToUpdate.DurationInMinutes,
            IsOnline = serviceToUpdate.IsOnline,
            StreetAddress = serviceToUpdate.StreetAddress,
            City = serviceToUpdate.City,
            PostalCode = serviceToUpdate.PostalCode,
            IsActive = serviceToUpdate.IsActive,
            ProviderId = serviceToUpdate.ProviderId,
            ProviderName = $"{providerDto.FirstName} {providerDto.LastName}",
            CategoryId = serviceToUpdate.CategoryId,
            CategoryName = categoryDto.Name
        };
        return returnDto;
    }

    /// <inheritdoc/>
    public async Task DeleteServiceAsync(int serviceId, string providerId,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Attempting to delete Service {ServiceId} owned by {ProviderId}", serviceId, providerId);
        var serviceToDelete = await dbContext
            .Services
            .FindAsync(new object[] { serviceId }, cancellationToken);

        if (serviceToDelete == null)
        {
            logger.LogWarning("Failed delete attempt for Service {ServiceId} by user {ProviderId}. Service not found.",
                serviceId, providerId);
            throw new EntityNotFoundException(nameof(Service), serviceId);
        }

        if (serviceToDelete.ProviderId != providerId)
        {
            logger
                .LogWarning("User {UserId} attempted to delete Service {ServiceId} owned by {OwnerId}",
                    providerId,
                    serviceToDelete.Id,
                    serviceToDelete.ProviderId);
            throw new AuthorizationException(providerId, $"Delete ServiceId '{serviceToDelete.Id}'");
        }

        var serviceName = serviceToDelete.Name;
        this.dbContext.SoftDelete(serviceToDelete);
        try
        {
            await this.dbContext.SaveChangesAsync(cancellationToken);
            logger
                .LogInformation(
                    "Service {ServiceId} with name {ServiceName} owned by {ProviderId} was successfully soft-deleted.",
                    serviceId, serviceName,
                    providerId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete Service {ServiceId}", serviceId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceViewDto?> GetServiceByIdAsync(int serviceId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Attempting to fetch service with ID: {ServiceId}", serviceId);

        var service = await dbContext.Services
            .AsNoTracking()
            .Include(s => s.Provider)
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken);

        if (service == null)
        {
            logger.LogInformation("Service with ID: {ServiceId} not found.", serviceId);
            return null;
        }

        var dto = new ServiceViewDto
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
            ProviderName = $"{service.Provider.FirstName} {service.Provider.LastName}",
            CategoryId = service.CategoryId,
            CategoryName = service.Category.Name
        };

        logger.LogInformation("Successfully fetched Service {ServiceId}", serviceId);
        return dto;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ServiceViewDto>> GetServicesByCategoryAsync(int categoryId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ServiceViewDto>> GetServicesByProviderAsync(string providerId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}