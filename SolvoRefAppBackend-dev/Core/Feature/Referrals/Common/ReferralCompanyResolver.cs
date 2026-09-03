using System.Globalization;
using System.Text;

namespace Core.Feature.Referrals.Common
{
    public static class ReferralCompanyResolver
    {
        public const string Transparent = "Transparent";
        public const string Solvo = "Solvo";

        public static string ResolveDataSourcingCompany(string account, string area, string country, string? city)
        {
            return IsTransparentReferral(account, area, country, city)
                ? Transparent
                : Solvo;
        }

        public static bool IsTransparentReferral(string account, string area, string country, string? city)
        {
            if (HasSpecificAccount(account))
            {
                return IsTbpoAccount(account);
            }

            return IsTransparentAreaLocation(area, country, city);
        }

        private static bool IsTbpoAccount(string account)
        {
            var normalizedAccount = NormalizeForComparison(account);
            if (normalizedAccount.StartsWith("tbpo cr and sales roles"))
            {
                return true;
            }

            var tbpoAccounts = new HashSet<string>
            {
                "tpg travel pass",
                "propio",
                "uly",
                "nolan",
                "netsol",
                "jlr",
                "truly",
                "urgently ehi uda",
                "the ticket clinic",
                "ttc",
                "cyracom",
                "spirit",
                "honk"
            };

            return tbpoAccounts.Any(tbpoAccount => ContainsNormalizedPhrase(normalizedAccount, tbpoAccount));
        }

        private static bool HasSpecificAccount(string account)
        {
            var normalizedAccount = NormalizeForComparison(account);
            if (string.IsNullOrWhiteSpace(normalizedAccount))
            {
                return false;
            }

            var noSpecificAccountValues = new HashSet<string>
            {
                "other",
                "i m not referring to any particular account",
                "im not referring to any particular account"
            };

            return !noSpecificAccountValues.Contains(normalizedAccount);
        }

        private static bool IsTransparentAreaLocation(string area, string country, string? city)
        {
            var normalizedArea = NormalizeForComparison(area);
            if (normalizedArea != "customer service" && normalizedArea != "sales")
            {
                return false;
            }

            var normalizedCountry = NormalizeForComparison(country);
            var normalizedCity = NormalizeForComparison(city ?? string.Empty);
            if (normalizedCountry == "argentina")
            {
                return normalizedCity == "cordoba";
            }

            if (normalizedCountry == "mexico")
            {
                return normalizedCity == "chihuahua";
            }

            if (normalizedCountry != "colombia")
            {
                return false;
            }

            var transparentColombiaCities = new HashSet<string>
            {
                "barranquilla"
            };

            return transparentColombiaCities.Contains(normalizedCity);
        }

        private static bool ContainsNormalizedPhrase(string normalizedValue, string normalizedPhrase)
        {
            return normalizedValue.Equals(normalizedPhrase, StringComparison.Ordinal) ||
                normalizedValue.StartsWith($"{normalizedPhrase} ", StringComparison.Ordinal) ||
                normalizedValue.EndsWith($" {normalizedPhrase}", StringComparison.Ordinal) ||
                normalizedValue.Contains($" {normalizedPhrase} ", StringComparison.Ordinal);
        }

        private static string NormalizeForComparison(string value)
        {
            var normalized = (value ?? string.Empty).Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                builder.Append(char.IsLetterOrDigit(character) ? character : ' ');
            }

            return string.Join(
                ' ',
                builder
                    .ToString()
                    .Normalize(NormalizationForm.FormC)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
