using Hangfire;
using Hangfire.Dashboard;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Api.Middleware;
using Core;
using Core.DBContext;
using Core.DBContext.Configuration.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Identity;
using System.Threading.RateLimiting;
using Core.Feature.Referrals.SyncActiveVacancies;
using MediatR;
using Microsoft.Extensions.Logging.AzureAppServices;
using Api.Swagger;
using Microsoft.EntityFrameworkCore.Metadata;

var builder = WebApplication.CreateBuilder(args);
var hangfireEnabled = builder.Configuration.GetValue<bool?>("BackgroundTask:Enabled") ?? true;
var hangfireConnectionString = builder.Configuration.GetConnectionString("HRHangfireDatabaseConnectionString");
var hangfireConfigured = IsValidSqlConnectionString(hangfireConnectionString);
hangfireEnabled = hangfireEnabled && hangfireConfigured;

builder.Logging.AddAzureWebAppDiagnostics();
builder.Logging.AddFilter("Api", LogLevel.Information);
builder.Logging.AddFilter("Core", LogLevel.Information);
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);
builder.Services.Configure<AzureFileLoggerOptions>(options =>
{
    options.FileName = "diagnostics-";
    options.FileSizeLimit = 50 * 1024;
    options.RetainedFileCountLimit = 5;
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Configuration
.SetBasePath(Directory.GetCurrentDirectory())
.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
.AddEnvironmentVariables();
builder.Services.AddControllers(options =>
options.Filters.Add(typeof(ValidationFilterAttribute)))
.ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true)
.AddNewtonsoftJson(options => { options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore; });
builder.Services.AddCoreServices(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = 20;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.AutoReplenishment = true;
    });

    options.AddPolicy("referral-create", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("fabric-validate", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 15,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("admin-content-write", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Solvo Ref API", Version = "v1" });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.OperationFilter<AuthorizeCheckOperationFilter>();
});

var app = builder.Build();

ApplyDatabaseMigrations(app);
await EnsureIdentityRoles(app);
EnsureDefaultHomeContentCards(app);
await SyncActiveVacanciesOnStartup(app);

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionMiddleware>();
app.UseStaticFiles();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), geolocation=(), microphone=()");
    context.Response.Headers.TryAdd("Cross-Origin-Opener-Policy", "same-origin");
    // This API is intentionally consumed cross-origin by the Static Web App.
    // "same-site" breaks browser fetches from the frontend custom domain to the API host.
    context.Response.Headers.TryAdd("Cross-Origin-Resource-Policy", "cross-origin");
    context.Response.Headers.TryAdd("Origin-Agent-Cluster", "?1");
    context.Response.Headers.TryAdd("X-Permitted-Cross-Domain-Policies", "none");
    context.Response.Headers.TryAdd("Content-Security-Policy", "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com data:; img-src 'self' data: blob: https:; connect-src 'self' https:; object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'");
    await next();
});

app.UseRouting();

var allowedOrigins = BuildAllowedOrigins(builder.Configuration);
app.UseCors(x =>
{
    x.AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials()
    .WithOrigins(allowedOrigins);
});
app.UseMiddleware<CsrfProtectionMiddleware>();

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
if (hangfireEnabled)
{
    var hangfireDashboardReadOnly = builder.Configuration.GetValue<bool?>("BackgroundTask:HangfireDashboardReadOnly") ?? false;
    var hangfireOptions = new DashboardOptions
    {
        Authorization = [new HangfireAuthorizationFilter()],
        IsReadOnlyFunc = _ => hangfireDashboardReadOnly
    };
    app.UseHangfireDashboard("/hangfire", hangfireOptions);
}
app.MapControllers();
if (hangfireEnabled)
{
    app.StartRecurringJobs(builder.Configuration);
}
app.Run();

static bool IsValidSqlConnectionString(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("#{"))
    {
        return false;
    }

    try
    {
        _ = new SqlConnectionStringBuilder(connectionString);
        return true;
    }
    catch
    {
        return false;
    }
}

static string[] BuildAllowedOrigins(IConfiguration configuration)
{
    var origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var knownFrontendOrigins = new[]
    {
        "https://red-river-03149200f.2.azurestaticapps.net",
        "https://yellow-pond-0c44ce80f.2.azurestaticapps.net",
        "https://pruebasolvoreferralapp.solvoglobal.com",
        "https://solvoreferralapp.solvoglobal.com"
    };

    foreach (var origin in knownFrontendOrigins)
    {
        if (TryGetOrigin(origin, out var normalized))
        {
            origins.Add(normalized);
        }
    }

    foreach (var origin in configuration.GetSection("OriginAllow").Get<string[]>() ?? [])
    {
        if (TryGetOrigin(origin, out var normalized))
        {
            origins.Add(normalized);
        }
    }

    AddOriginFromKey(configuration, origins, "AzureAd:RedirectUris:Frontend");
    AddOriginFromKey(configuration, origins, "AzureAd:RedirectUris:frontend");
    AddOriginFromKey(configuration, origins, "FrontendUrl");
    AddOriginFromKey(configuration, origins, "frontRedirect");
    AddOriginFromKey(configuration, origins, "FrontendRedirectUrl");
    AddOriginFromKey(configuration, origins, "REACT_APP_REDIRECT_URI");

    return origins.ToArray();
}

static void AddOriginFromKey(IConfiguration configuration, ISet<string> origins, string key)
{
    if (TryGetOrigin(configuration[key], out var normalized))
    {
        origins.Add(normalized);
    }
}

static bool TryGetOrigin(string? url, out string origin)
{
    origin = string.Empty;
    if (string.IsNullOrWhiteSpace(url))
    {
        return false;
    }

    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
    {
        return false;
    }

    origin = uri.GetLeftPart(UriPartial.Authority);
    return true;
}

static void ApplyDatabaseMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("StartupMigrations");
    var db = scope.ServiceProvider.GetRequiredService<SolvoRefAppContext>();

    try
    {
        var pendingMigrations = db.Database.GetPendingMigrations().ToArray();
        if (pendingMigrations.Length == 0)
        {
            logger.LogInformation("No pending database migrations.");
        }
        else
        {
            logger.LogInformation(
                "Applying {Count} pending database migration(s): {Migrations}",
                pendingMigrations.Length,
                string.Join(", ", pendingMigrations));

            db.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully.");
        }

        ValidateDatabaseSchema(db);
        logger.LogInformation("Database schema matches the Entity Framework model.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Failed to apply database migrations during startup.");
        throw;
    }
}

static void ValidateDatabaseSchema(SolvoRefAppContext db)
{
    const string sql = """
        SELECT s.name AS SchemaName, t.name AS TableName, c.name AS ColumnName
        FROM sys.tables AS t
        INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
        INNER JOIN sys.columns AS c ON c.object_id = t.object_id;
        """;

    var actualColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var connection = db.Database.GetDbConnection();
    var shouldClose = connection.State != System.Data.ConnectionState.Open;

    if (shouldClose)
    {
        connection.Open();
    }

    try
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            actualColumns.Add($"{reader.GetString(0)}.{reader.GetString(1)}.{reader.GetString(2)}");
        }
    }
    finally
    {
        if (shouldClose)
        {
            connection.Close();
        }
    }

    var missingColumns = db.Model.GetEntityTypes()
        .SelectMany(entityType =>
        {
            var tableName = entityType.GetTableName();
            if (tableName is null)
            {
                return [];
            }

            var schema = entityType.GetSchema() ?? "dbo";
            var table = StoreObjectIdentifier.Table(tableName, schema);
            return entityType.GetProperties()
                .Select(property => property.GetColumnName(table))
                .Where(columnName => columnName is not null)
                .Select(columnName => $"{schema}.{tableName}.{columnName}");
        })
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Where(expected => !actualColumns.Contains(expected))
        .OrderBy(expected => expected, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    if (missingColumns.Length > 0)
    {
        throw new InvalidOperationException(
            $"Database schema is missing {missingColumns.Length} model column(s): {string.Join(", ", missingColumns)}");
    }
}

static void EnsureDefaultHomeContentCards(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("HomeContentSeed");
    var db = scope.ServiceProvider.GetRequiredService<SolvoRefAppContext>();

    try
    {
        var existingSections = db.HomeContentCards
            .AsNoTracking()
            .Select(x => x.Section)
            .Distinct()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var defaultCards = HomeContentCardConfiguration.GetDefaultCards();
        var missingCards = defaultCards
            .Where(card => !existingSections.Contains(card.Section))
            .ToList();

        if (missingCards.Count == 0)
        {
            logger.LogInformation("Default Home content cards already present.");
            return;
        }

        db.HomeContentCards.AddRange(missingCards);
        db.SaveChanges();
        logger.LogInformation(
            "Seeded {Count} default Home content card(s) for missing sections: {Sections}",
            missingCards.Count,
            string.Join(", ", missingCards.Select(x => x.Section).Distinct(StringComparer.OrdinalIgnoreCase)));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to seed default Home content cards.");
        throw;
    }
}

static async Task SyncActiveVacanciesOnStartup(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ActiveVacancySync");

    try
    {
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(new SyncActiveVacanciesRequest());

        if (!response.Success)
        {
            logger.LogError(
                "Active vacancy sync finished with errors: {Errors}",
                response.Errors is { Count: > 0 } ? string.Join(" | ", response.Errors) : "Unknown error");
            return;
        }

        logger.LogInformation("Active vacancies synced successfully on startup.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to sync active vacancies on startup.");
    }
}

static async Task EnsureIdentityRoles(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentityBootstrap");
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    try
    {
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            var createRoleResult = await roleManager.CreateAsync(new IdentityRole("Admin"));
            if (!createRoleResult.Succeeded)
            {
                throw new InvalidOperationException($"Could not create Admin role: {string.Join(", ", createRoleResult.Errors.Select(e => e.Description))}");
            }
        }

        if (!await roleManager.RoleExistsAsync("User"))
        {
            var createRoleResult = await roleManager.CreateAsync(new IdentityRole("User"));
            if (!createRoleResult.Succeeded)
            {
                throw new InvalidOperationException($"Could not create User role: {string.Join(", ", createRoleResult.Errors.Select(e => e.Description))}");
            }
        }

        logger.LogInformation("Ensured identity roles.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Failed to ensure identity roles.");
        throw;
    }
}
