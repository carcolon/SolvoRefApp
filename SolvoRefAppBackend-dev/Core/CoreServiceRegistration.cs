using System.Reflection;
using System.Text;
using Core.Contracts.Azure;
using Core.Contracts.Configurations;
using Core.Contracts.Datalake;
using Core.Contracts.DataSourcing;
using Core.Contracts.Fabric;
using Core.Contracts.Identity;
using Core.Contracts.Referrals;
using Core.Contracts.Security;
using Core.DBContext;
using Core.Models.Global;
using Core.Models.Identity;
using Core.Repositories;
using Core.Repositories.Configurations;
using Core.Repositories.Referral;
using Core.Service.Azure;
using Core.Service.Datalake;
using Core.Service.DataSourcing;
using Core.Service.Fabric;
using Core.Service.Identity;
using Core.Service.Security;
using Core.Services.Identity;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
namespace Core
{
    public static class CoreServiceRegistration
    {
        public static IServiceCollection AddCoreServices(this IServiceCollection services, IConfiguration configuration)
        {
            var hangfireEnabled = configuration.GetValue<bool?>("BackgroundTask:Enabled") ?? true;
            var hangfireConnectionString = configuration.GetConnectionString("HRHangfireDatabaseConnectionString");
            var hangfireConfigured = IsValidSqlConnectionString(hangfireConnectionString);

            services.Configure<JwtSetting>(configuration.GetSection("JwtSetting"));
            services.AddDbContext<SolvoRefAppContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("HRDatabaseConnectionString"));
            });
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 12;
            }).AddEntityFrameworkStores<SolvoRefAppContext>().AddDefaultTokenProviders();


            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = configuration["JwtSetting:Issuer"],
                    ValidAudience = configuration["JwtSetting:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSetting:Key"])),
                };
                o.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrWhiteSpace(context.Token) &&
                            context.Request.Cookies.TryGetValue("auth_token", out var cookieToken))
                        {
                            context.Token = cookieToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                        var userId = context.Principal?.FindFirst("uid")?.Value;
                        var tokenSecurityStamp = context.Principal?.FindFirst("sstamp")?.Value;

                        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(tokenSecurityStamp))
                        {
                            context.Fail("Invalid token claims.");
                            return;
                        }

                        var user = await userManager.FindByIdAsync(userId);
                        if (user is null || !string.Equals(user.SecurityStamp, tokenSecurityStamp, StringComparison.Ordinal))
                        {
                            context.Fail("Token revoked.");
                        }
                    }
                };
            });
            if (hangfireEnabled && hangfireConfigured)
            {
                services.AddHangfire((sp, config) =>
                {
                    config.UseSqlServerStorage(hangfireConnectionString);
                });
            }

            services.AddTransient<IAuthService, AuthService>();
            services.AddTransient<IUserService, UserService>();
            services.AddHttpClient();
            services.AddTransient<IAzureBlobStorageService, AzureBlobStorageService>();
            services.AddTransient<IDatalakeService, DatalakeService>();
            services.AddTransient<IFabricService, FabricService>();
            services.AddScoped<IDataSourcingService, DataSourcingService>();
            services.AddScoped<IReferralRepository, ReferralRepository>();
            services.AddScoped<IReferralAccountRepository, ReferralAccountRepository>();
            services.AddScoped<IReferralApplyAreaRepository, ReferralApplyAreaRepository>();
            services.AddScoped<IReferralCityRepository, ReferralCityRepository>();
            services.AddScoped<IReferralCountryRepository, ReferralCountryRepository>();
            services.AddScoped<IReferralEnglishLevelRepository, ReferralEnglishLevelRepository>();
            services.AddScoped<IReferralExperienceRepository, ReferralExperienceRepository>();
            services.AddScoped<IReferralFoundRepository, ReferralFoundRepository>();
            services.AddScoped<IHolyDatesCountryCodeRepository, HolyDatesCountryCodeRepository>();
            services.AddScoped<ICountryHuntyInformationRepository, CountryHuntyInformationRepository>();
            services.AddScoped<IReferralVacancyRepository, ReferralVacancyRepository>();
            services.AddScoped<IPaymentScheduleRepository, PaymentScheduleRepository>();
            services.AddScoped<ITurnstileService, TurnstileService>();

            if (hangfireEnabled && hangfireConfigured)
            {
                services.AddHangfireServer();
            }
            services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()));
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

            return services;
        }

        private static bool IsValidSqlConnectionString(string? connectionString)
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
    }
}
