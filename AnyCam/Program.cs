using AnyCam.Models;
using AnyCam.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddHostedService<AiProcessingService>();
builder.Services.AddHostedService<ScheduledRecordingService>();
builder.Services.AddHostedService<StreamCleanupService>();

builder.Services.AddDataProtection(); // For GDPR compliance and encryption

builder.Services.AddScoped<AnyCam.Services.CameraService>();
builder.Services.AddScoped<AnyCam.Services.AiService>();
builder.Services.AddSingleton<AnyCam.Services.StreamingService>();

var app = builder.Build();

// Register shutdown handler to stop all streams
app.Lifetime.ApplicationStopping.Register(() =>
{
    var streamingService = app.Services.GetRequiredService<StreamingService>();
    streamingService.StopAllStreams();
});

// Seed roles and admin user
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    // Seed roles
    if (!await roleManager.RoleExistsAsync("Admin")) await roleManager.CreateAsync(new IdentityRole("Admin"));
    if (!await roleManager.RoleExistsAsync("Viewer")) await roleManager.CreateAsync(new IdentityRole("Viewer"));
    if (!await roleManager.RoleExistsAsync("Guest")) await roleManager.CreateAsync(new IdentityRole("Guest"));

    // Seed admin user
    var adminUser = await userManager.FindByEmailAsync("admin@anycam.com");
    if (adminUser == null)
    {
        adminUser = new IdentityUser { UserName = "admin", Email = "admin@anycam.com" };
        await userManager.CreateAsync(adminUser, "Admin123!");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Middleware to track stream access
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/streams"))
    {
        var path = context.Request.Path.Value;
        var parts = path.Split('/');
        if (parts.Length >= 3 && int.TryParse(parts[2], out int cameraId))
        {
            var streamingService = context.RequestServices.GetRequiredService<StreamingService>();
            streamingService.UpdateLastAccessed(cameraId);
        }
    }
    await next();
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
