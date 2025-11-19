using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Serilog Configuration ---
// This configures Serilog to be the logging provider for the application.
// It reads configuration from appsettings.json, enriches logs with context,
// and writes to both the console and a rolling file.
builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration) // Allows configuring Serilog from appsettings.json
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/service-booking-.log", rollingInterval: RollingInterval.Day) // Writes to a daily log file
);
// --- End Serilog Configuration ---

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// --- Add Serilog Request Logging ---
// This middleware logs details about each incoming HTTP request.
// It's placed early in the pipeline to capture as much information as possible.
app.UseSerilogRequestLogging();
// --- End Serilog Request Logging ---

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


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
    // Ensure all logs are written before the application exits.
    Log.CloseAndFlush(); 
}
