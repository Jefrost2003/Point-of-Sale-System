using System.Text.Json;
using System.Text;

namespace IT15_INATO_POS.Services
{
    public interface IReCaptchaService
    {
        Task<bool> VerifyTokenAsync(string token);
    }

    public class ReCaptchaService : IReCaptchaService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ReCaptchaService> _logger;
        private readonly string _secretKey;

        public ReCaptchaService(IHttpClientFactory httpClientFactory, ILogger<ReCaptchaService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            // ONLY get from environment - NO HARDCODING!
            _secretKey = Environment.GetEnvironmentVariable("RECAPTCHA_SECRET_KEY") ?? string.Empty;

            if (string.IsNullOrEmpty(_secretKey))
            {
                _logger.LogError("❌ CRITICAL: RECAPTCHA_SECRET_KEY not configured in environment");
            }
            else
            {
#pragma warning disable S2629
                _logger.LogInformation($"✅ ReCaptchaService initialized (secret key loaded, length: {_secretKey.Length})");
#pragma warning restore S2629
            }
        }

        public async Task<bool> VerifyTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("reCAPTCHA token is null or empty");
                return false;
            }

            if (string.IsNullOrEmpty(_secretKey))
            {
                _logger.LogError("reCAPTCHA secret key not configured. Cannot verify token.");
                return false;
            }

            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                var content = new StringContent(
                    $"secret={Uri.EscapeDataString(_secretKey)}&response={Uri.EscapeDataString(token)}",
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded");

                _logger.LogInformation("Calling Google reCAPTCHA verification API");

#pragma warning disable S1075
                var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
#pragma warning restore S1075
                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var success = root.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
                var hostname = root.TryGetProperty("hostname", out var hostProp) ? hostProp.GetString() : "";

#pragma warning disable S2629
                _logger.LogInformation($"reCAPTCHA result - Success: {success}, Hostname: {hostname}");
#pragma warning restore S2629

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying reCAPTCHA token");
                return false;
            }
        }
    }
}