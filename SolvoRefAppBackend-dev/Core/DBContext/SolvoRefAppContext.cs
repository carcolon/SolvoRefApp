using Core.Models.Configurations;
using Core.Models.Content;
using Core.Models.Global;
using Core.Models.Identity;
using Core.Models.Referrals;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Core.DBContext
{
    public class SolvoRefAppContext(DbContextOptions<SolvoRefAppContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Referral> Referral { get; set; }
        public DbSet<ReferralAccount> ReferralAccounts { get; set; }
        public DbSet<ReferralApplyArea> ReferralApplyAreas { get; set; }
        public DbSet<ReferralCity> ReferralCities { get; set; }
        public DbSet<ReferralCountry> ReferralCountries { get; set; }
        public DbSet<ReferralEnglishLevel> ReferralEnglishLevels { get; set; }
        public DbSet<ReferralExperience> ReferralExperiences { get; set; }
        public DbSet<ReferralFound> ReferralFounds { get; set; }
        public DbSet<ReferralLink> ReferralLinks { get; set; }
        public DbSet<ApplicationUser> AspNetUsers { get; set; }
        public DbSet<HolyDatesCountryCode> HolyDatesCountryCodes { get; set; }
        public DbSet<CountryHuntyInformation> CountriesHuntyInformation { get; set; }
        public DbSet<ReferralVacancy> Vacancies {get; set;}
        public DbSet<HomeContentCard> HomeContentCards { get; set; }
        public DbSet<PaymentSchedule> PaymentSchedules { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Referral>()
                .Property(x => x.ReferralSubmissionKey)
                .HasMaxLength(64);
            builder.Entity<Referral>()
                .HasIndex(x => x.ReferralSubmissionKey)
                .IsUnique();
            builder.ApplyConfigurationsFromAssembly(typeof(SolvoRefAppContext).Assembly);
        }
    }
}
