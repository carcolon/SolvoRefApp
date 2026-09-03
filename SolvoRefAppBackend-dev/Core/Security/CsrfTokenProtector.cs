using System.Security.Cryptography;
using System.Text;

namespace Core.Security
{
    public static class CsrfTokenProtector
    {
        public static string Create(string authToken, string signingKey)
        {
            var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var signature = Sign(authToken, nonce, signingKey);
            return $"{Base64UrlEncode(nonce)}.{Base64UrlEncode(signature)}";
        }

        public static bool Validate(string csrfToken, string authToken, string signingKey)
        {
            if (string.IsNullOrWhiteSpace(csrfToken) ||
                string.IsNullOrWhiteSpace(authToken) ||
                string.IsNullOrWhiteSpace(signingKey))
            {
                return false;
            }

            var parts = csrfToken.Split('.', 2);
            if (parts.Length != 2)
            {
                return false;
            }

            try
            {
                var nonce = Base64UrlDecode(parts[0]);
                var providedSignature = Base64UrlDecode(parts[1]);
                var expectedSignature = Sign(authToken, nonce, signingKey);

                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(providedSignature),
                    Encoding.UTF8.GetBytes(expectedSignature));
            }
            catch
            {
                return false;
            }
        }

        private static string Sign(string authToken, string nonce, string signingKey)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
            var payload = Encoding.UTF8.GetBytes($"{authToken}|{nonce}");
            return Convert.ToBase64String(hmac.ComputeHash(payload));
        }

        private static string Base64UrlEncode(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string Base64UrlDecode(string value)
        {
            var padded = value.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2:
                    padded += "==";
                    break;
                case 3:
                    padded += "=";
                    break;
            }

            return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
        }
    }
}
