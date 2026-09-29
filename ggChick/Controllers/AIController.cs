using Microsoft.AspNetCore.Mvc;
using ggChick.Services;

namespace ggChick.Controllers
{
    public class AIController : Controller
    {
        private readonly IGeminiService _geminiService;
        private readonly ILogger<AIController> _logger;

        public AIController(IGeminiService geminiService, ILogger<AIController> logger)
        {
            _geminiService = geminiService;
            _logger = logger;
        }

        public class ChatRequest
        {
            public string Message { get; set; } = string.Empty;
        }

        // POST: /AI/Chat
        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return Json(new { success = false, reply = "Please ask a question so I can assist you!" });
            }

            try
            {
                var reply = await _geminiService.GenerateResponseAsync(request.Message.Trim());
                return Json(new { success = true, reply });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chatbot error processing message.");
                return Json(new { 
                    success = false, 
                    reply = "I'm having trouble connecting right now. Please try again or browse our categories above!" 
                });
            }
        }
    }
}
