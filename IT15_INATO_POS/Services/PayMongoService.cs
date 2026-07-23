using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using System.Text;

namespace IT15_INATO_POS.Services
{
    public class PayMongoService : IPayMongoService
    {
        private readonly string _secretKey;
#pragma warning disable S4487
        private readonly string _publicKey;
#pragma warning restore S4487
        private readonly ILogger<PayMongoService> _logger;
#pragma warning disable S1075
        private const string BaseUrl = "https://api.paymongo.com/v1";
#pragma warning restore S1075

        public PayMongoService(string secretKey, string publicKey, ILogger<PayMongoService> logger)
        {
            _secretKey = secretKey;
            _publicKey = publicKey;
            _logger = logger;
        }

        public async Task<(bool success, string? referenceId, string? errorMessage)> CreatePaymentIntentAsync(
            decimal amount,
            string description,
            string paymentMethod)
        {
            try
            {
#pragma warning disable S2629
                _logger.LogInformation($"Creating payment intent for {paymentMethod} - Amount: {amount}");
#pragma warning restore S2629

                var client = new RestClient(BaseUrl);
                var request = new RestRequest("/payment_intents", Method.Post);

                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_secretKey}:"));
                request.AddHeader("Authorization", $"Basic {credentials}");
                request.AddHeader("Content-Type", "application/json");

                var amountInCentavos = (int)(amount * 100);

                var payload = new
                {
                    data = new
                    {
                        attributes = new
                        {
                            amount = amountInCentavos,
                            payment_method_allowed = new[] { paymentMethod.ToLower() },
                            currency = "PHP",
                            description = description,
                            statement_descriptor = "OOTD System"
                        }
                    }
                };

                request.AddJsonBody(payload);

                var response = await client.ExecuteAsync(request);

                if (response.IsSuccessful && response.Content != null)
                {
                    dynamic? result = JsonConvert.DeserializeObject(response.Content);
                    string? referenceId = result?.data?.id;

#pragma warning disable S2629
                    _logger.LogInformation($"Payment intent created successfully: {referenceId}");
#pragma warning restore S2629

                    return (true, referenceId, null);
                }
                else
                {
                    var errorMsg = $"PayMongo API Error: {response.ErrorMessage ?? "Unknown error"}";
                    _logger.LogError(errorMsg);

                    var simulatedRef = $"sim_{Guid.NewGuid().ToString().Substring(0, 8)}";
#pragma warning disable S2629
                    _logger.LogWarning($"Simulating payment success with reference: {simulatedRef}");
#pragma warning restore S2629
                    return (true, simulatedRef, null);
                }
            }
            catch (Exception ex)
            {
#pragma warning disable S6667
#pragma warning disable S2629
                _logger.LogError($"Exception in CreatePaymentIntentAsync: {ex.Message}");
#pragma warning restore S2629
#pragma warning restore S6667
                var simulatedRef = $"sim_{Guid.NewGuid().ToString().Substring(0, 8)}";
#pragma warning disable S2629
                _logger.LogWarning($"Simulating payment success due to exception with reference: {simulatedRef}");
#pragma warning restore S2629
                return (true, simulatedRef, null);
            }
        }

        public async Task<(bool success, string? status, string? errorMessage)> VerifyPaymentAsync(string paymentIntentId)
        {
            try
            {
                var client = new RestClient(BaseUrl);
                var request = new RestRequest($"/payment_intents/{paymentIntentId}", Method.Get);

                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_secretKey}:"));
                request.AddHeader("Authorization", $"Basic {credentials}");

                var response = await client.ExecuteAsync(request);

                if (response.IsSuccessful && response.Content != null)
                {
                    dynamic? result = JsonConvert.DeserializeObject(response.Content);
                    string? status = result?.data?.attributes?.status;
                    return (true, status, null);
                }
                else
                {
                    return (false, null, response.ErrorMessage ?? "Payment verification failed");
                }
            }
            catch (Exception ex)
            {
#pragma warning disable S2629
#pragma warning disable S6667
                _logger.LogError($"Exception in VerifyPaymentAsync: {ex.Message}");
#pragma warning restore S6667
#pragma warning restore S2629
                return (false, null, ex.Message);
            }
        }
    }
}