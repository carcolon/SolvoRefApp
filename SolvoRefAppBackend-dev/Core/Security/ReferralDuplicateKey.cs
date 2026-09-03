using System.Security.Cryptography;
using System.Text;

namespace Core.Security
{
    public static class ReferralDuplicateKey
    {
        public static string Create(string? referrerId, string? referralId, string? email)
        {
            var normalized = string.Join("|",
                Normalize(referrerId),
                Normalize(referralId),
                Normalize(email));

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string Normalize(string? value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }
    }
}
