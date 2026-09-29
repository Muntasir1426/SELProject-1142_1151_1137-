using System.Text;
using System.Text.Json;
using ggChick.Data;
using Microsoft.EntityFrameworkCore;

namespace ggChick.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(
            HttpClient httpClient,
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<string> GenerateResponseAsync(string userPrompt, string? conversationContext = null)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            var model = _configuration["Gemini:Model"] ?? "gemini-1.5-flash";

            // If API key is missing or is the default placeholder, provide intelligent contextual simulated response
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Equals("YOUR_GEMINI_API_KEY", StringComparison.OrdinalIgnoreCase))
            {
                return await GetSimulatedAssistantResponseAsync(userPrompt);
            }

            try
            {
                // Fetch catalog summary to ground the Gemini prompt
                var catalogSummary = await GetStoreCatalogContextAsync();

                var systemInstruction = "You are GGCLIKS AI Shopping Assistant, a helpful and friendly e-commerce assistant for the GGCLIKS store. " +
                                        "Assist customers with questions about products, categories, pricing, shipping, orders, and returns. " +
                                        "Keep answers polite, engaging, and concise (under 3 paragraphs). " +
                                        "Here is our current store catalog and policy information:\n" +
                                        catalogSummary +
                                        "\nPolicy: Free standard shipping on orders over $100. 30-day hassle-free returns. Payment methods: Credit/Debit Cards, Cash on Delivery.";

                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

                var requestBody = new
                {
                    system_instruction = new
                    {
                        parts = new[] { new { text = systemInstruction } }
                    },
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[] { new { text = userPrompt } }
                        }
                    }
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync(endpoint, jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Gemini API call failed with status {StatusCode}: {Error}", response.StatusCode, errorDetails);
                    return await GetSimulatedAssistantResponseAsync(userPrompt);
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);

                var root = doc.RootElement;
                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                    {
                        var text = parts[0].GetProperty("text").GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            return text.Trim();
                        }
                    }
                }

                return "I'm here to help you browse our GGCLIKS catalog! Feel free to ask about our T-shirts, cardigans, hoodies, or pants.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while contacting Google Gemini API.");
                return await GetSimulatedAssistantResponseAsync(userPrompt);
            }
        }

        private async Task<string> GetStoreCatalogContextAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var products = await db.Products
                    .Where(p => p.IsActive)
                    .Include(p => p.Category)
                    .Take(15)
                    .Select(p => $"- {p.Name} (${p.Price}) in {p.Category!.Name}: {p.Description.Substring(0, Math.Min(p.Description.Length, 90))}...")
                    .ToListAsync();

                return string.Join("\n", products);
            }
            catch
            {
                return "- AW22 Heavyweight Vintage Graphic Tee ($44.99)\n- Chunky Oversized Mohair Knit Cardigan ($98.00)\n- Classic Heavyweight French Terry Hoodie ($78.00)\n- Tactical Multi-Pocket Relaxed Cargo Pants ($85.00)";
            }
        }

        private async Task<string> GetSimulatedAssistantResponseAsync(string prompt)
        {
            var p = prompt.ToLowerInvariant();

            if (p.Contains("hoodie") || p.Contains("fleece") || p.Contains("pullover"))
            {
                return "Our hoodies are customer favorites! Check out our **Classic Heavyweight French Terry Hoodie** ($78.00) crafted with 450 GSM fleece, or the vintage **Streetwear Acid Wash Pullover Hoodie** ($88.00).";
            }

            if (p.Contains("cardigan") || p.Contains("knit") || p.Contains("sweater"))
            {
                return "Looking for knitwear? We recommend our **Chunky Oversized Mohair Knit Cardigan** ($98.00) or the **Nordic Geometric Brushed Wool Cardigan** ($115.00) for effortless layering.";
            }

            if (p.Contains("tee") || p.Contains("tshirt") || p.Contains("t-shirt") || p.Contains("shirt"))
            {
                return "Explore our streetwear tees! The **AW22 Heavyweight Vintage Graphic Tee** ($44.99) and the boxy **Dope Chef Streetwear T-Shirt** ($48.50) are both made with 100% heavyweight cotton.";
            }

            if (p.Contains("pant") || p.Contains("cargo") || p.Contains("trouser") || p.Contains("bottom"))
            {
                return "Need new bottoms? Check out the **Tactical Multi-Pocket Relaxed Cargo Pants** ($85.00) or our relaxed **Wide-Leg Pleated Baggy Street Trouser** ($78.00).";
            }

            if (p.Contains("ship") || p.Contains("delivery") || p.Contains("track"))
            {
                return "GGCLIKS provides **Free Standard Shipping** on all orders over $100! Standard orders typically arrive in 3-5 business days, and you can track your status anytime under **My Orders**.";
            }

            if (p.Contains("return") || p.Contains("refund"))
            {
                return "We offer a 30-day hassle-free return policy. If you're not completely satisfied with your order, return it in original condition for a full refund or replacement.";
            }

            if (p.Contains("hello") || p.Contains("hi") || p.Contains("hey"))
            {
                return "Hello and welcome to **GGCLIKS**! 👋 I'm your AI Shopping Assistant. How can I assist you with our T-shirts, cardigans, hoodies, or pants today?";
            }

            if (p.Contains("recommend") || p.Contains("best") || p.Contains("popular"))
            {
                return "Our top customer favorites right now are the **AW22 Heavyweight Graphic Tee**, the **Chunky Oversized Mohair Cardigan**, and the **Classic French Terry Hoodie**. Let me know if you'd like more details on any of these!";
            }

            return "Thanks for asking! At GGCLIKS we offer curated street & casual wear including T-shirts, cardigans, hoodies, and pants with free shipping over $100. Feel free to search products using our top search bar or ask me any question about our items!";
        }
    }
}
