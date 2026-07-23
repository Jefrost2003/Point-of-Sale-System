using IT15_INATO_POS.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RestSharp;
using System.Text;

namespace IT15_INATO_POS.Services
{
    public class PdfService : IPdfService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _apiKey;

        public PdfService(ApplicationDbContext context, string apiKey)
        {
            _context = context;
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        }

        public async Task<byte[]?> GenerateReceiptPdfAsync(int transactionId)
        {
            try
            {
                var transaction = await _context.Transactions
                    .Include(t => t.Cashier)
                    .Include(t => t.SalesItems!)
                        .ThenInclude(si => si.ProductVariation!)
                            .ThenInclude(pv => pv!.Product)
                    .FirstOrDefaultAsync(t => t.TransID == transactionId);

                if (transaction == null)
                    return null;

                // Build receipt HTML (now static)
                var htmlContent = BuildReceiptHtml(transaction);

#pragma warning disable S1075
                var client = new RestClient("https://api.craftmypdf.com/v1");
#pragma warning restore S1075
                var request = new RestRequest("/create", Method.Post);

                request.AddHeader("X-API-KEY", _apiKey);
                request.AddHeader("Content-Type", "application/json");

                var payload = new
                {
                    template_id = "custom",
                    data = new
                    {
                        html = htmlContent
                    },
                    export_type = "json"
                };

                request.AddJsonBody(payload);

                var response = await client.ExecuteAsync(request);

                if (response.IsSuccessful && response.Content != null)
                {
                    dynamic? result = JsonConvert.DeserializeObject(response.Content);
                    string? pdfUrl = result?.file;

                    if (!string.IsNullOrEmpty(pdfUrl))
                    {
                        using var httpClient = new HttpClient();
                        return await httpClient.GetByteArrayAsync(pdfUrl);
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // ==================== FIXED: STATIC METHOD ====================
        private static string BuildReceiptHtml(IT15_INATO_POS.Models.Transaction transaction)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<html><head><style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            sb.AppendLine("table { width: 100%; border-collapse: collapse; }");
            sb.AppendLine("th, td { padding: 8px; text-align: left; border-bottom: 1px solid #ddd; }");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine("<h2>OOTD - Outfit of the Day</h2>");
            sb.AppendLine("<p>Receipt #: " + transaction.TransID + "</p>");
            sb.AppendLine("<p>Date: " + transaction.TransactionDate.ToString("MM/dd/yyyy hh:mm tt") + "</p>");
            sb.AppendLine("<p>Cashier: " + transaction.Cashier?.FullName + "</p>");

            sb.AppendLine("<hr/><table>");
            sb.AppendLine("<tr><th>Item</th><th>Qty</th><th>Price</th><th>Total</th></tr>");

            foreach (var item in transaction.SalesItems!)
            {
                sb.AppendLine(
                    $"<tr><td>{item.ProductVariation?.Product?.ProductName} " +
                    $"({item.ProductVariation?.Size} - {item.ProductVariation?.Color})</td>" +
                    $"<td>{item.Quantity}</td>" +
                    $"<td>₱{item.UnitPrice:N2}</td>" +
                    $"<td>₱{item.Subtotal:N2}</td></tr>"
                );
            }

            sb.AppendLine("</table><hr/>");

            sb.AppendLine($"<p><strong>Total: ₱{transaction.TotalAmount:N2}</strong></p>");
            sb.AppendLine($"<p>Payment Method: {transaction.PaymentMethod}</p>");

            sb.AppendLine("<p style='text-align: center; margin-top: 30px;'>Thank you for shopping with us!</p>");
            sb.AppendLine("</body></html>");

            return sb.ToString();
        }
    }
}