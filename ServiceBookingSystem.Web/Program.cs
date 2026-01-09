using ServiceBookingSystem.Data.Seeders;
using ServiceBookingSystem.Application;
using Serilog;
using ServiceBookingSystem.Data;
using ServiceBookingSystem.Infrastructure;
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

// This single call now registers DbContext, Identity, and all seeders from the Data layer.
builder.Services.AddDataServices(builder.Configuration);

// This call registers all services from the Application layer.
builder.Services.AddApplicationServices();

// This call registers all services from the Infrastructure layer (e.g., Email Service).
builder.Services.AddInfrastructureServices(builder.Configuration);

// Register Global Exception Handler
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
    
    logger.LogInformation("Starting database seeding...");
    try
    {
        var rolesSeeder = serviceProvider.GetRequiredService<RolesSeeder>();
        await rolesSeeder.SeedAsync(serviceProvider);

        var adminSeeder = serviceProvider.GetRequiredService<AdministratorSeeder>();
        await adminSeeder.SeedAsync(serviceProvider);
        
        logger.LogInformation("Database seeding completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "An error occurred during database seeding.");
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
