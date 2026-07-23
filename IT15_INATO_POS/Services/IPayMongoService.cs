namespace IT15_INATO_POS.Services
{
    public interface IPayMongoService
    {
        Task<(bool success, string? referenceId, string? errorMessage)> CreatePaymentIntentAsync(
            decimal amount,
            string description,
            string paymentMethod);

        Task<(bool success, string? status, string? errorMessage)> VerifyPaymentAsync(string paymentIntentId);
    }
}