namespace IT15_INATO_POS.Services
{
    public interface IPdfService
    {
        Task<byte[]?> GenerateReceiptPdfAsync(int transactionId);
    }
}