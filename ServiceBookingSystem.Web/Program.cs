using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Entities.Identity;
using ServiceBookingSystem.Data.Seeders;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Serilog Configuration ---
builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/service-booking-.log", rollingInterval: RollingInterval.Day)
);

// --- Adding services to the container ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Registering the individual seeders with the DI container.
builder.Services.AddTransient<RolesSeeder>();
builder.Services.AddTransient<AdministratorSeeder>();

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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

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