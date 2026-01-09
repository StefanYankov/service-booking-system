using Microsoft.EntityFrameworkCore;
using ServiceBookingSystem.Data.Seeders;
using ServiceBookingSystem.Application;
using Serilog;
using ServiceBookingSystem.Data;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Infrastructure;
using ServiceBookingSystem.Web.Extensions;
using ServiceBookingSystem.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// --- Serilog Configuration ---
builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/service-booking-.log", rollingInterval: RollingInterval.Day)
);

// --- Register services from other layers ---

builder.Services.AddDataServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// --- JWT Authentication Configuration ---
builder.Services.AddApiAuthentication(builder.Configuration);

// --- Register Global Exception Handler ---
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// --- Seed the database ---
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation("Starting database migration and seeding...");
    try
    {
        // Apply Migrations automatically
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.MigrateAsync();
        }

        var rolesSeeder = serviceProvider.GetRequiredService<RolesSeeder>();
        await rolesSeeder.SeedAsync(serviceProvider);

        var adminSeeder = serviceProvider.GetRequiredService<AdministratorSeeder>();
        await adminSeeder.SeedAsync(serviceProvider);
        
        logger.LogInformation("Database migration and seeding completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "An error occurred during database migration or seeding.");
        throw;
    }
}

// --- Configure the HTTP request pipeline ---
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
}

app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), appBuilder =>
{
    appBuilder.UseExceptionHandler(options => { });
});

app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api"), appBuilder =>
{
    appBuilder.UseExceptionHandler("/Home/Error");
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

try
{
    Log.Information("Starting web host");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
// By adding this partial class definition, we are making the auto-generated Program class public.
// This is required for the WebApplicationFactory in the integration test project to access it.
public partial class Program { }
