using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Core.Contracts.Security;
using Core.Models.Global;
using Microsoft.Extensions.Configuration;

namespace Core.Service.Security
{
    public class TurnstileService : ITurnstileService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public TurnstileService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<Response<bool>> ValidateToken(string token, string? remoteIp, CancellationToken cancellationToken)
        {
            var secretKey = _configuration["Turnstile:SecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                return Response<bool>.ErrorResponse(["Turnstile is not configured."], HttpStatusCode.InternalServerError);
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                return Response<bool>.ErrorResponse(["Captcha validation is required."], HttpStatusCode.BadRequest);
            }

            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = secretKey,
                ["response"] = token,
                ["remoteip"] = remoteIp ?? string.Empty
            });

            var client = _httpClientFactory.CreateClient();
            using var response = await client.PostAsync(
                "https://challenges.cloudflare.com/turnstile/v0/siteverify",
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Response<bool>.ErrorResponse(["Captcha validation service is unavailable."], response.StatusCode);
            }

            var verification = await response.Content.ReadFromJsonAsync<TurnstileVerifyResponse>(cancellationToken);
            if (verification?.Success == true)
            {
                return Response<bool>.SuccessResponse(true, HttpStatusCode.OK);
            }

            return Response<bool>.ErrorResponse(["Captcha validation failed."], HttpStatusCode.BadRequest);
        }

        private sealed class TurnstileVerifyResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }
        }
    }
}
