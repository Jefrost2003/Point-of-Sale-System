using DotNetEnv;
using IT15_INATO_POS.Data;
using IT15_INATO_POS.Middleware;
using IT15_INATO_POS.Models;
using IT15_INATO_POS.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==================== FORCE FIND .env FILE ====================
string envPath = null;
#pragma warning disable S1075
var possiblePaths = new[]
{
    Path.Combine(Directory.GetCurrentDirectory(), ".env"),
    Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())?.FullName ?? "", ".env"),
    @"D:\Sites\site64907\.env",
    @"D:\Sites\site64907\wwwroot\.env",
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env")
};
#pragma warning restore S1075

#pragma warning disable S3267
foreach (var path in possiblePaths)
{
    if (File.Exists(path))
    {
        envPath = path;
        break;
    }
}
#pragma warning restore S3267

if (envPath != null)
{
    try
    {
        Env.Load(envPath);
        Console.WriteLine($"✅ .env loaded from: {envPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Error loading .env from {envPath}: {ex.Message}");
    }
}
else
{
    Console.WriteLine("❌ CRITICAL: .env file not found in any location!");
}

static string GetRequiredConfig(string key)
{
    var value = Environment.GetEnvironmentVariable(key);
    if (string.IsNullOrEmpty(value))
    {
#pragma warning disable S108
#pragma warning disable S2486
        try
        {
            value = Env.GetString(key);
        }
        catch { }
#pragma warning restore S2486
#pragma warning restore S108

        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Required configuration '{key}' is missing. Please check your .env file.");
        }
    }
    return value;
}

// ==================== DATABASE ====================
var dbServer = GetRequiredConfig("DB_SERVER");
var dbName = GetRequiredConfig("DB_NAME");
var dbUser = GetRequiredConfig("DB_USER");
var dbPassword = GetRequiredConfig("DB_PASSWORD");

var dbConnectionString = $"Server={dbServer};Database={dbName};User Id={dbUser};Password={dbPassword};Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(dbConnectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ==================== IDENTITY ====================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12;
    options.Password.RequiredUniqueChars = 4;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddPasswordValidator<CustomPasswordValidator>();

// ==================== EMAIL SENDER ====================
builder.Services.AddTransient<IEmailSender, EmailSender>();

// ==================== COOKIE SETTINGS ====================
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.IsEssential = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Home/AccessDenied";
});

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(5);
});

// ==================== SECURITY HEADERS ====================
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

// ==================== CORS ====================
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://ootd.runasp.net")
              .WithMethods("GET", "POST", "PUT", "DELETE")
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ==================== RATE LIMITING ====================
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<Microsoft.AspNetCore.Http.HttpContext, string>(httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 500,
                QueueLimit = 50,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.RejectionStatusCode = 429;
});

// ==================== SERVICES ====================
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<ISecurityLogService, SecurityLogService>();
builder.Services.AddScoped<ISystemLogService, SystemLogService>();
builder.Services.AddScoped<IReCaptchaService, ReCaptchaService>();
builder.Services.AddScoped<CustomPasswordValidator>();
builder.Services.AddHttpClient();

// ==================== PAYMONGO ====================
var paymongoSecret = GetRequiredConfig("PAYMONGO_SECRET_KEY");
var paymongoPublic = GetRequiredConfig("PAYMONGO_PUBLIC_KEY");

builder.Services.AddScoped<IPayMongoService>(provider =>
    new PayMongoService(
        paymongoSecret,
        paymongoPublic,
        provider.GetRequiredService<ILogger<PayMongoService>>()));

// ==================== PDF SERVICE ====================
var pdfKey = GetRequiredConfig("CRAFTMYPDF_API_KEY");
builder.Services.AddScoped<IPdfService>(provider =>
    new PdfService(
        provider.GetRequiredService<ApplicationDbContext>(),
        pdfKey));

// ==================== MVC & API ====================
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSession();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddResponseCompression();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ==================== BUILD ====================
var app = builder.Build();

// ==================== MIDDLEWARE ====================
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseExceptionHandler("/Home/Error");
}
else
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("Production");
app.UseRateLimiter();
app.UseResponseCompression();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.UseMiddleware<SecurityHeadersMiddleware>();

app.MapControllers();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// ==================== SEED DATABASE ====================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await SeedDatabaseAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error during database seeding");
    }
}

#pragma warning disable S6966
app.Run();
#pragma warning restore S6966

// ==================== SEED METHOD ====================
static async Task SeedDatabaseAsync(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

    await dbContext.Database.MigrateAsync();

    string[] roleNames = { "Super Admin", "Admin", "Store Manager", "Cashier", "Inventory Clerk" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    var superAdminEmail = Environment.GetEnvironmentVariable("SUPER_ADMIN_EMAIL");
    var superAdminPassword = Environment.GetEnvironmentVariable("SUPER_ADMIN_PASSWORD");

    if (!string.IsNullOrEmpty(superAdminEmail) && !string.IsNullOrEmpty(superAdminPassword))
    {
        var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);
        if (superAdmin == null)
        {
#pragma warning disable S2068
            var newSuperAdmin = new ApplicationUser
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                FirstName = Environment.GetEnvironmentVariable("SUPER_ADMIN_FIRSTNAME") ?? "System",
                LastName = Environment.GetEnvironmentVariable("SUPER_ADMIN_LASTNAME") ?? "Administrator",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                PasswordLastChanged = DateTime.UtcNow,
                PasswordHistory = "[]"
            };
#pragma warning restore S2068

            var result = await userManager.CreateAsync(newSuperAdmin, superAdminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(newSuperAdmin, "Super Admin");
            }
        }
    }
}