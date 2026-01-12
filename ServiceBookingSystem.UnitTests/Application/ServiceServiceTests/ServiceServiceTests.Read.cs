using ServiceBookingSystem.Application.DTOs.Service;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.UnitTests.Application.ServiceServiceTests;

public partial class ServiceServiceTests
{
    [Fact]
    public async Task GetServiceByIdAsync_WithExistingId_ShouldReturnCorrectlyMappedDto()
    {
        // Arrange:
        const string providerId = "provider-id";
        const int serviceId = 1;

        var provider = new ApplicationUser
        {
            Id = providerId,
            FirstName = "John",
            LastName = "Doe"
        };
        var category = new Category
        {
            Id = 1,
            Name = "Test Category"
        };
        var service = new Service
        {
            Id = serviceId,
            Name = "Test Service",
            Description = "Description",
            Price = 100,
            DurationInMinutes = 60,
            IsOnline = true,
            IsActive = true,
            StreetAddress = "123 Main St",
            City = "Test City",
            PostalCode = "12345",
            ProviderId = providerId,
            CategoryId = 1
        };

        await this.dbContext.Users.AddAsync(provider);
        await this.dbContext.Categories.AddAsync(category);
        await this.dbContext.Services.AddAsync(service);
        await this.dbContext.SaveChangesAsync();

        // Act:
        var result = await this.serviceService.GetServiceByIdAsync(serviceId);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(serviceId, result.Id);
        Assert.Equal("Test Service", result.Name);
        Assert.Equal("Description", result.Description);
        Assert.Equal(100, result.Price);
        Assert.Equal(60, result.DurationInMinutes);
        Assert.True(result.IsOnline);
        Assert.True(result.IsActive);
        Assert.Equal("123 Main St", result.StreetAddress);
        Assert.Equal("Test City", result.City);
        Assert.Equal("12345", result.PostalCode);
        Assert.Equal(providerId, result.ProviderId);
        Assert.Equal("John Doe", result.ProviderName);
        Assert.Equal(1, result.CategoryId);
        Assert.Equal("Test Category", result.CategoryName);
    }

    [Fact]
    public async Task GetServiceByIdAsync_WithSoftDeletedService_ShouldReturnNull()
    {
        // Arrange:
        const string providerId = "provider-id";
        const int serviceId = 2;

        var service = new Service
        {
            Id = serviceId,
            Name = "Deleted Service",
            Description = "This service is deleted.",
            ProviderId = providerId,
            CategoryId = 1,
            IsDeleted = true, // Soft deleted
            DeletedOn = DateTime.UtcNow
        };


        var provider = new ApplicationUser
        {
            Id = providerId,
            FirstName = "John",
            LastName = "Doe"
        };
        var category = new Category
        {
            Id = 1,
            Name = "Test Category"
        };

        await this.dbContext.Users.AddAsync(provider);
        await this.dbContext.Categories.AddAsync(category);
        await this.dbContext.Services.AddAsync(service);
        await this.dbContext.SaveChangesAsync();

        this.dbContext.ChangeTracker.Clear();

        // Act:
        var result = await this.serviceService.GetServiceByIdAsync(serviceId);

        // Assert:
        Assert.Null(result);
    }

    [Fact]
    public async Task GetServiceByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange:
        const int nonExistentId = 999;

        // Act:
        var result = await this.serviceService.GetServiceByIdAsync(nonExistentId);

        // Assert:
        Assert.Null(result);
    }

    [Fact]
    public async Task GetServiceByIdAsync_WithNegativeId_ShouldReturnNull()
    {
        // Arrange:
        const int negativeId = -1;

        // Act:
        var result = await this.serviceService.GetServiceByIdAsync(negativeId);

        // Assert:
        Assert.Null(result);
    }

    [Fact]
    public async Task GetServiceByIdAsync_ShouldNotTrackEntity()
    {
        // Arrange:
        const string providerId = "provider-id";
        const int serviceId = 3;

        var provider = new ApplicationUser
        {
            Id = providerId,
            FirstName = "John",
            LastName = "Doe"
        };
        var category = new Category
        {
            Id = 1,
            Name = "Test Category"
        };
        var service = new Service
        {
            Id = serviceId,
            Name = "Tracking Test Service",
            Description = "Test Description",
            ProviderId = providerId,
            CategoryId = 1,
        };

        await this.dbContext.Users.AddAsync(provider);
        await this.dbContext.Categories.AddAsync(category);
        await this.dbContext.Services.AddAsync(service);
        await this.dbContext.SaveChangesAsync();

        this.dbContext.ChangeTracker.Clear();

        // Act:
        var result = await this.serviceService.GetServiceByIdAsync(serviceId);

        // Assert:
        Assert.NotNull(result);


        var isTracked = this.dbContext.ChangeTracker.Entries<Service>()
            .Any(e => e.Entity.Id == serviceId); // Verify that the ChangeTracker is NOT tracking the service entity

        Assert.False(isTracked, "Entity should not be tracked when using AsNoTracking()");
    }

    [Fact]
    public async Task GetServicesByCategoryAsync_WithValidCategoryId_ShouldReturnPagedAndSortedServices()
    {
        // Arrange:
        var provider = new ApplicationUser
        {
            Id = "provider-1",
            FirstName = "John",
            LastName = "Doe"
        };
        var category1 = new Category
        {
            Id = 1,
            Name = "Category 1"
        };
        var category2 = new Category
        {
            Id = 2,
            Name = "Category 2"
        };

        var service1 = new Service
        {
            Name = "B Service",
            Description = "Desc 1",
            ProviderId = "provider-1",
            CategoryId = 1,
            Price = 200
        };

        var service2 = new Service
        {
            Name = "C Service",
            Description = "Desc 2",
            ProviderId = "provider-1",
            CategoryId = 2
        }; // Wrong category
        var service3 = new Service
        {
            Name = "A Service",
            Description = "Desc 3",
            ProviderId = "provider-1",
            CategoryId = 1,
            Price = 100
        };
        var service4 = new Service
        {
            Name = "D Service",
            Description = "Desc 4",
            ProviderId = "provider-1",
            CategoryId = 1,
            IsDeleted = true
        }; // Soft-deleted

        await this.dbContext.Users.AddAsync(provider);
        await this.dbContext.Categories.AddRangeAsync(category1, category2);
        await this.dbContext.Services.AddRangeAsync(service1, service2, service3, service4);
        await this.dbContext.SaveChangesAsync();

        var parameters = new PagingAndSortingParameters
        {
            PageNumber = 1,
            PageSize = 5,
            SortBy = "Name",
            SortDirection = "asc"
        };

        // Act:
        var result = await this.serviceService.GetServicesByCategoryAsync(1, parameters);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount); // Total non-deleted services in category 1
        Assert.Equal(2, result.Items.Count()); // Items on the current page
        Assert.Equal("A Service", result.Items.First().Name); // Check sorting
        Assert.Equal("B Service", result.Items.Last().Name);
        Assert.True(result.Items.All(s => s.CategoryId == 1));
    }

    [Fact]
    public async Task GetServicesByCategoryAsync_WithCategoryHavingNoServices_ShouldReturnEmptyPagedResult()
    {
        // Arrange:
        var category = new Category
        {
            Id = 1,
            Name = "Empty Category"
        };
        await this.dbContext.Categories.AddAsync(category);
        await this.dbContext.SaveChangesAsync();
        var parameters = new PagingAndSortingParameters();

        // Act:
        var result = await this.serviceService.GetServicesByCategoryAsync(1, parameters);

        // Assert:
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetServicesByCategoryAsync_WithNonExistentCategoryId_ShouldReturnEmptyPagedResult()
    {
        // Arrange:
        const int nonExistentCategoryId = 999;
        var parameters = new PagingAndSortingParameters();

        // Act:
        var result = await this.serviceService.GetServicesByCategoryAsync(nonExistentCategoryId, parameters);

        // Assert:
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetServicesByProviderAsync_WithValidProviderId_ShouldReturnPagedAndSortedServices()
    {
        // Arrange:
        var provider1 = new ApplicationUser
        {
            Id = "provider-1",
            FirstName = "John",
            LastName = "Doe"
        };

        var provider2 = new ApplicationUser
        {
            Id = "provider-2",
            FirstName = "Jane",
            LastName = "Smith"
        };

        var category = new Category
        {
            Id = 1,
            Name = "Category 1"
        };

        var service1 = new Service
        {
            Name = "B Service",
            Description = "Desc 1",
            ProviderId = "provider-1",
            CategoryId = 1,
            Price = 200
        };

        var service2 = new Service
        {
            Name = "C Service",
            Description = "Desc 2",
            ProviderId = "provider-2", // Wrong provider
            CategoryId = 1
        };

        var service3 = new Service
        {
            Name = "A Service",
            Description = "Desc 3",
            ProviderId = "provider-1",
            CategoryId = 1,
            Price = 100
        };

        var service4 = new Service
        {
            Name = "D Service",
            Description = "Desc 4",
            ProviderId = "provider-1",
            CategoryId = 1,
            IsDeleted = true // Soft-deleted
        };

        await this.dbContext.Users.AddRangeAsync(provider1, provider2);
        await this.dbContext.Categories.AddAsync(category);
        await this.dbContext.Services.AddRangeAsync(service1, service2, service3, service4);
        await this.dbContext.SaveChangesAsync();

        var parameters = new PagingAndSortingParameters
        {
            PageNumber = 1,
            PageSize = 5,
            SortBy = "Price",
            SortDirection = "asc"
        };

        // Act:
        var result = await this.serviceService.GetServicesByProviderAsync("provider-1", parameters);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount); // Total non-deleted services for provider 1
        Assert.Equal(2, result.Items.Count());
        Assert.Equal("A Service", result.Items.First().Name); // Check sorting by price
        Assert.Equal("B Service", result.Items.Last().Name);
        Assert.True(result.Items.All(s => s.ProviderId == "provider-1"));
    }

    [Fact]
    public async Task GetServicesByProviderAsync_WithProviderHavingNoServices_ShouldReturnEmptyPagedResult()
    {
        // Arrange:
        var provider = new ApplicationUser
        {
            Id = "provider-1",
            FirstName = "John",
            LastName = "Doe"
        };
        await this.dbContext.Users.AddAsync(provider);
        await this.dbContext.SaveChangesAsync();
        var parameters = new PagingAndSortingParameters();

        // Act:
        var result = await this.serviceService.GetServicesByProviderAsync("provider-1", parameters);

        // Assert:
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetServicesByCategoryAsync_WithSecondPage_ShouldReturnCorrectItems()
    {
        // Arrange:
        var provider = new ApplicationUser
        {
            Id = "provider-1",
            FirstName = "John",
            LastName = "Doe"
        };

        var category = new Category
        {
            Id = 1,
            Name = "Category 1"
        };

        var service1 = new Service
        {
            Name = "Service 1",
            Description = "Desc",
            ProviderId = "provider-1",
            CategoryId = 1
        };

        var service2 = new Service
        {
            Name = "Service 2",
            Description = "Desc",
            ProviderId = "provider-1",
            CategoryId = 1
        };

        var service3 = new Service
        {
            Name = "Service 3",
            Description = "Desc",
            ProviderId = "provider-1",
            CategoryId = 1
        };

        await this.dbContext.Users.AddAsync(provider);
        await this.dbContext.Categories.AddAsync(category);
        await this.dbContext.Services.AddRangeAsync(service1, service2, service3);
        await this.dbContext.SaveChangesAsync();

        var parameters = new PagingAndSortingParameters
        {
            PageNumber = 2,
            PageSize = 2,
            SortBy = "Name",
            SortDirection = "asc"
        };

        // Act:
        var result = await this.serviceService.GetServicesByCategoryAsync(1, parameters);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount); // Total items is 3
        Assert.Single(result.Items); // Page 2 with size 2 should have 1 item (the 3rd one)
        Assert.Equal("Service 3", result.Items.First().Name);
    }

    [Fact]
    public async Task GetServicesByCategoryAsync_WithSortByPriceDesc_ShouldReturnCorrectOrder()
    {
        // Arrange:
        var provider = new ApplicationUser
        {
            Id = "provider-1",
            FirstName = "John",
            LastName = "Doe"
        };

        var category = new Category
        {
            Id = 1,
            Name = "Category 1"
        };

        var service1 = new Service
        {
            Name = "Cheap",
            Description = "Desc",
            ProviderId = "provider-1",
            CategoryId = 1,
            Price = 10
        };

        var service2 = new Service
        {
            Name = "Expensive",
            Description = "Desc",
            ProviderId = "provider-1",
            CategoryId = 1,
            Price = 100
        };

        var service3 = new Service
        {
            Name = "Medium",
            Description = "Desc",
            ProviderId = "provider-1",
            CategoryId = 1,
            Price = 50
        };

        await this.dbContext.Users.AddAsync(provider);
        await this.dbContext.Categories.AddAsync(category);
        await this.dbContext.Services.AddRangeAsync(service1, service2, service3);
        await this.dbContext.SaveChangesAsync();

        var parameters = new PagingAndSortingParameters
        {
            SortBy = "Price",
            SortDirection = "desc"
        };

        // Act:
        var result = await this.serviceService.GetServicesByCategoryAsync(1, parameters);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(3, result.Items.Count());
        Assert.Equal("Expensive", result.Items.First().Name);
        Assert.Equal("Medium", result.Items.ElementAt(1).Name);
        Assert.Equal("Cheap", result.Items.Last().Name);
    }

    [Fact]
    public async Task GetServicesByCategoryAsync_WithInvalidSortColumn_ShouldDefaultToCreatedOnDesc()
    {
        // Arrange:
        var provider = new ApplicationUser
        {
            Id = "provider-1",
            FirstName = "John",
            LastName = "Doe"
        };
        var category = new Category
        {
            Id = 1,
            Name = "Category 1"
        };

        var service1 = new Service
        {
            Name = "Service 1",
            Description = "Desc",
            ProviderId = "provider-1",
            CategoryId = 1,
            CreatedOn = DateTime.UtcNow.AddHours(-3)
        };

        var service2 = new Service
        {
            Name = "Service 2",
            Description = "Desc",
            ProviderId = "provider-1",
            CategoryId = 1,
            CreatedOn = DateTime.UtcNow.AddHours(-2)
        };

        var service3 = new Service
        {
            Name = "Service 3",
            Description = "Desc",
            ProviderId = "provider-1",
            CategoryId = 1,
            CreatedOn = DateTime.UtcNow.AddHours(-1)
        };

        await this.dbContext.Users.AddAsync(provider);
        await this.dbContext.Categories.AddAsync(category);
        await this.dbContext.Services.AddRangeAsync(service1, service2, service3);
        await this.dbContext.SaveChangesAsync();

        var parameters = new PagingAndSortingParameters
        {
            SortBy = "InvalidColumnName",
            SortDirection = "asc"
        };

        // Act:
        var result = await this.serviceService.GetServicesByCategoryAsync(1, parameters);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(3, result.Items.Count());

        // Should be sorted by CreatedOn Descending (Newest first)
        Assert.Equal("Service 3", result.Items.First().Name);
        Assert.Equal("Service 2", result.Items.ElementAt(1).Name);
        Assert.Equal("Service 1", result.Items.Last().Name);
    }

    [Fact]
    public async Task SearchServicesAsync_WithSearchTerm_ShouldFilterByNameOrDescription()
    {
        // Arrange:
        var provider = new ApplicationUser
        {
            Id = "p1",
            FirstName = "P",
            LastName = "1"
        };

        var category = new Category
        {
            Id = 1,
            Name = "Cat"
        };

        var s1 = new Service
        {
            Name = "Plumber",
            Description = "Fix pipes",
            ProviderId = "p1",
            CategoryId = 1
        };

        var s2 = new Service
        {
            Name = "Electrician",
            Description = "Fix wires",
            ProviderId = "p1",
            CategoryId = 1
        };

        var s3 = new Service
        {
            Name = "Carpenter",
            Description = "Wood plumber work",
            ProviderId = "p1",
            CategoryId = 1
        }; // Matches "plumber" in desc

        await dbContext.Users.AddAsync(provider);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Services.AddRangeAsync(s1, s2, s3);
        await dbContext.SaveChangesAsync();

        var parameters = new ServiceSearchParameters { SearchTerm = "plumber" };

        // Act:
        var result = await serviceService.SearchServicesAsync(parameters);

        // Assert:
        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, s => s.Name == "Plumber");
        Assert.Contains(result.Items, s => s.Name == "Carpenter");
    }

    [Fact]
    public async Task SearchServicesAsync_WithPriceRange_ShouldFilterByPrice()
    {
        // Arrange:
        var provider = new ApplicationUser
        {
            Id = "p1",
            FirstName = "P",
            LastName = "1"
        };

        var category = new Category
        {
            Id = 1,
            Name = "Cat"
        };

        var service1 = new Service
        {
            Name = "Cheap",
            Description = "Desc",
            Price = 10,
            ProviderId = "p1",
            CategoryId = 1
        };

        var service2 = new Service
        {
            Name = "Medium",
            Description = "Desc",
            Price = 50,
            ProviderId = "p1",
            CategoryId = 1
        };

        var service3 = new Service
        {
            Name = "Expensive",
            Description = "Desc",
            Price = 100,
            ProviderId = "p1",
            CategoryId = 1
        };

        await dbContext.Users.AddAsync(provider);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Services.AddRangeAsync(service1, service2, service3);
        await dbContext.SaveChangesAsync();

        var parameters = new ServiceSearchParameters
        {
            MinPrice = 20,
            MaxPrice = 80
        };

        // Act:
        var result = await serviceService.SearchServicesAsync(parameters);

        // Assert:
        Assert.Single(result.Items);
        Assert.Equal("Medium", result.Items.First().Name);
    }

    [Fact]
    public async Task SearchServicesAsync_WithOnlineFilter_ShouldFilterByIsOnline()
    {
        // Arrange:
        var provider = new ApplicationUser
        {
            Id = "p1",
            FirstName = "P",
            LastName = "1"
        };

        var category = new Category
        {
            Id = 1,
            Name = "Cat"
        };
        var service1 = new Service
        {
            Name = "Online",
            Description = "Desc",
            IsOnline = true,
            ProviderId = "p1",
            CategoryId = 1
        };

        var service2 = new Service
        {
            Name = "Offline",
            Description = "Desc",
            IsOnline = false,
            ProviderId = "p1",
            CategoryId = 1
        };

        await dbContext.Users.AddAsync(provider);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Services.AddRangeAsync(service1, service2);
        await dbContext.SaveChangesAsync();

        var parameters = new ServiceSearchParameters { IsOnline = true };

        // Act:
        var result = await serviceService.SearchServicesAsync(parameters);

        // Assert:
        Assert.Single(result.Items);
        Assert.Equal("Online", result.Items.First().Name);
    }

    [Fact]
    public async Task SearchServicesAsync_WithCombinedFilters_ShouldReturnIntersection()
    {
        // Arrange:
        var provider = new ApplicationUser
        {
            Id = "p1",
            FirstName = "P",
            LastName = "1"
        };

        var category = new Category
        {
            Id = 1,
            Name = "Cat"
        };

        var service1 = new Service
        {
            Name = "Target",
            Description = "Desc",
            Price = 50,
            IsOnline = true,
            ProviderId = "p1",
            CategoryId = 1
        };

        var service2 = new Service
        {
            Name = "Target",
            Description = "Desc",
            Price = 150,
            IsOnline = true,
            ProviderId = "p1",
            CategoryId = 1
        }; // Price too high

        var service3 = new Service
        {
            Name = "Target",
            Description = "Desc",
            Price = 50,
            IsOnline = false,
            ProviderId = "p1",
            CategoryId = 1
        }; // Offline

        var service4 = new Service
        {
            Name = "Other",
            Description = "Desc",
            Price = 50,
            IsOnline = true,
            ProviderId = "p1",
            CategoryId = 1
        }; // Name mismatch

        await dbContext.Users.AddAsync(provider);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Services.AddRangeAsync(service1, service2, service3, service4);
        await dbContext.SaveChangesAsync();

        var parameters = new ServiceSearchParameters
        {
            SearchTerm = "Target",
            MaxPrice = 100,
            IsOnline = true
        };

        // Act:
        var result = await serviceService.SearchServicesAsync(parameters);

        // Assert:
        Assert.Single(result.Items);
        Assert.Equal("Target", result.Items.First().Name);
        Assert.Equal(50, result.Items.First().Price);
    }

    [Fact]
    public async Task GetDistinctCitiesAsync_ShouldReturnDistinctCitiesOrderedAlphabetically()
    {
        // Arrange:
        var provider = new ApplicationUser
        {
            Id = "p1",
            FirstName = "P",
            LastName = "1"
        };

        var category = new Category
        {
            Id = 1,
            Name = "Cat"
        };

        var service1 = new Service
        {
            Name = "S1",
            Description = "D",
            ProviderId = "p1",
            CategoryId = 1,
            City = "Sofia"
        };

        var service2 = new Service
        {
            Name = "S2",
            Description = "D",
            ProviderId = "p1",
            CategoryId = 1,
            City = "Varna"
        };
        
        var service3 = new Service
        {
            Name = "S3",
            Description = "D",
            ProviderId = "p1",
            CategoryId = 1,
            City = "Sofia"
        }; // Duplicate
        
        var service4 = new Service
        {
            Name = "S4",
            Description = "D",
            ProviderId = "p1",
            CategoryId = 1,
            City = "Plovdiv"
        };

        await dbContext.Users.AddAsync(provider);
        await dbContext.Categories.AddAsync(category);
        await this.dbContext.Services.AddRangeAsync(service1, service2, service3, service4);
        await dbContext.SaveChangesAsync();

        // Act:
        var result = await serviceService.GetDistinctCitiesAsync();

        // Assert:
        Assert.Equal(3, result.Count);
        Assert.Equal("Plovdiv", result[0]);
        Assert.Equal("Sofia", result[1]);
        Assert.Equal("Varna", result[2]);
    }

    [Fact]
    public async Task GetDistinctCitiesAsync_ShouldIgnoreNullOrEmptyCities()
    {
        // Arrange
        var provider = new ApplicationUser
        {
            Id = "p1",
            FirstName = "P",
            LastName = "1"
        };
        
        var category = new Category
        {
            Id = 1,
            Name = "Cat"
        };

        var service1 = new Service
            { 
                Name = "S1",
                Description = "D",
                ProviderId = "p1",
                CategoryId = 1,
                City = "Sofia" 
            };
        var service2 = new Service
        {
            Name = "S2",
                Description = "D",
                ProviderId = "p1",
                CategoryId = 1, 
                City = null
        }; // Null
        var service3 = new Service
        {
            Name = "S3",
                Description = "D",
                ProviderId = "p1",
                CategoryId = 1,
                City = ""
        }; // Empty
        var service4 = new Service
        {
            Name = "S4",
            Description = "D",
            ProviderId = "p1",
            CategoryId = 1,
            City = "   "
        };

        await dbContext.Users.AddAsync(provider);
        await dbContext.Categories.AddAsync(category);
        await this.dbContext.Services.AddRangeAsync(service1, service2, service3, service4);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await serviceService.GetDistinctCitiesAsync();

        // Assert:
        Assert.Contains("Sofia", result);
        Assert.DoesNotContain(null, result);
        Assert.DoesNotContain("", result);
    }

    [Fact]
    public async Task GetDistinctCitiesAsync_WhenNoServicesExist_ShouldReturnEmptyList()
    {
        // Arrange:
        // DB is empty

        // Act:
        var result = await serviceService.GetDistinctCitiesAsync();

        // Assert:
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchServicesAsync_WithCityFilter_ShouldFilterByCity()
    {
        // Arrange:
        var provider = new ApplicationUser
        {
            Id = "p1",
            FirstName = "P",
            LastName = "1"
        };
        
        var category = new Category
        {
            Id = 1,
            Name = "Cat"
        };
        
        var service1 = new Service 
        {
            Name = "S1",
                Description = "D",
                ProviderId = "p1",
                CategoryId = 1,
                City = "Sofia"
        };
        
        var service2 = new Service 
        {
            Name = "S2",
                Description = "D",
                ProviderId = "p1",
                CategoryId = 1,
                City = "Varna"
        };
        
        var service3 = new Service 
        {
            Name = "S3",
                Description = "D",
                ProviderId = "p1",
                CategoryId = 1,
                City = "Sofia"
        };
        

        await dbContext.Users.AddAsync(provider);
        await dbContext.Categories.AddAsync(category);
        await this.dbContext.Services.AddRangeAsync(service1, service2, service3);
        await dbContext.SaveChangesAsync();

        var parameters = new ServiceSearchParameters { City = "Sofia" };

        // Act:
        var result = await serviceService.SearchServicesAsync(parameters);

        // Assert:
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, s => Assert.Equal("Sofia", s.City));
    }
}