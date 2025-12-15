using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCity_BE.Data;
using SmartCity_BE.Models;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SmartCity_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIFloodAnalysisController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AIFloodAnalysisController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        // ✅ Constants
        private const int MAX_ANALYSIS_LENGTH = 800;
        private const int MAX_RECOMMENDATIONS_LENGTH = 1000;

        public AIFloodAnalysisController(
            ApplicationDbContext context,
            ILogger<AIFloodAnalysisController> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("analyze/{floodReportId}")]
        public async Task<IActionResult> AnalyzeFloodImage(int floodReportId)
        {
            try
            {
                var report = await _context.FloodReports
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == floodReportId);

                if (report == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Không tìm thấy báo cáo"
                    });
                }

                if (string.IsNullOrEmpty(report.ImageUrl))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Báo cáo không có hình ảnh để phân tích"
                    });
                }

                var deepSeekApiKey = _configuration["DeepSeek:ApiKey"];
                if (string.IsNullOrEmpty(deepSeekApiKey))
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Chưa cấu hình DeepSeek API Key"
                    });
                }

                _logger.LogInformation("🔄 Calling DeepSeek AI for Report ID: {Id}", floodReportId);
                var analysis = await CallDeepSeekAI(report, deepSeekApiKey);

                // ✅ Truncate nếu quá dài
                analysis.Analysis = TruncateText(analysis.Analysis, MAX_ANALYSIS_LENGTH);
                analysis.Recommendations = TruncateText(analysis.Recommendations, MAX_RECOMMENDATIONS_LENGTH);

                _logger.LogInformation(
                    "✅ AI Analysis completed - Report ID: {Id}, Level: {Level}, Depth: {Depth}",
                    floodReportId,
                    analysis.WaterLevel,
                    analysis.EstimatedDepth
                );

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        reportId = floodReportId,
                        aiAnalysis = analysis,
                        imageUrl = report.ImageUrl
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Analysis error for report ID: {Id}", floodReportId);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Lỗi phân tích AI: {ex.Message}",
                    details = ex.InnerException?.Message
                });
            }
        }

        private async Task<AIAnalysisResult> CallDeepSeekAI(FloodReport report, string apiKey)
        {
            var baseUrl = _configuration["DeepSeek:BaseUrl"] ?? "https://openrouter.ai/api/v1";
            var model = _configuration["DeepSeek:Model"] ?? "deepseek/deepseek-chat";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            client.DefaultRequestHeaders.Add("HTTP-Referer", "http://localhost:5000");
            client.DefaultRequestHeaders.Add("X-Title", "SmartCity Admin Dashboard");

            // ✅ SYSTEM PROMPT - RÚT GỌN, NGẮN GỌN
            var systemPrompt = @"Bạn là chuyên gia phân tích ngập lụt. Phân tích ngắn gọn, súc tích.

MỨC ĐỘ NGẬP:
- Low: 10-20cm (mắt cá)
- Medium: 20-40cm (đầu gối)
- High: 40-60cm (eo)
- Dangerous: >60cm (nguy hiểm)

OUTPUT (JSON, NGẮN GỌN):
{
  ""waterLevel"": ""Low/Medium/High/Dangerous"",
  ""estimatedDepth"": ""XX-YYcm"",
  ""confidence"": ""low/medium/high"",
  ""analysis"": ""MÔ TẢ NGẮN 2-3 CÂU về mức độ, tác động, nguyên nhân"",
  ""recommendations"": ""5 KHUYẾN NGHỊ NGẮN (mỗi dòng 50-60 ký tự):
1. Hành động khẩn cấp
2. Giải pháp ngắn hạn
3. Biện pháp trung hạn
4. Phương án dài hạn
5. Lưu ý bảo trì""
}

LƯU Ý:
- Analysis: TỐI ĐA 150 từ
- Recommendations: TỐI ĐA 5 dòng, mỗi dòng 60 ký tự
- Trả về JSON hợp lệ, không có markdown
- Không thêm chú thích";

            var userPrompt = $@"Phân tích NGẮN GỌN:

Tiêu đề: {report.Title}
Mô tả: {TruncateText(report.Description, 300)}
Địa chỉ: {report.Address}
Thời gian: {report.CreatedAt:dd/MM/yyyy HH:mm}

Trả về JSON ngay, không giải thích thêm.";

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.3,
                max_tokens = 800, // ✅ GIẢM từ 1500 → 800
                top_p = 0.9,
                frequency_penalty = 0.5, // ✅ TĂNG để tránh lặp
                presence_penalty = 0.5
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("📤 Sending request to AI...");

            var response = await client.PostAsync($"{baseUrl}/chat/completions", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("📥 AI Status: {Status}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ AI API error: {Body}", responseBody);
                throw new Exception($"AI API error: HTTP {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<JsonElement>(responseBody);
            var aiResponse = result.GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrEmpty(aiResponse))
            {
                throw new Exception("AI response is empty");
            }

            _logger.LogInformation("📝 AI Response ({Len} chars):\n{Response}",
                aiResponse.Length,
                aiResponse.Length > 500 ? aiResponse.Substring(0, 500) + "..." : aiResponse
            );

            try
            {
                // ✅ CLEAN JSON - Aggressive
                var cleanedJson = CleanJsonResponse(aiResponse);
                _logger.LogInformation("🧹 Cleaned JSON:\n{Json}", cleanedJson);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                var aiData = JsonSerializer.Deserialize<AIAnalysisResult>(cleanedJson, options);

                if (aiData == null)
                {
                    throw new JsonException("Deserialized object is null");
                }

                // ✅ VALIDATE
                ValidateAIResult(aiData, report);

                _logger.LogInformation("✅ Parsed successfully - Level: {Level}", aiData.WaterLevel);

                return aiData;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "❌ JSON Parse Error");
                _logger.LogError("Raw response:\n{Response}", aiResponse);

                // ✅ FALLBACK: Extract từ text
                return CreateFallbackResult(aiResponse, report);
            }
        }

        // ✅ CLEAN JSON - Aggressive cleaning
        private string CleanJsonResponse(string response)
        {
            var cleaned = response.Trim();

            // Remove markdown code blocks
            cleaned = Regex.Replace(cleaned, @"^```(?:json)?\s*", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"\s*```$", "");

            // Remove text before first {
            var firstBrace = cleaned.IndexOf('{');
            if (firstBrace > 0)
            {
                cleaned = cleaned.Substring(firstBrace);
            }

            // Remove text after last }
            var lastBrace = cleaned.LastIndexOf('}');
            if (lastBrace >= 0 && lastBrace < cleaned.Length - 1)
            {
                cleaned = cleaned.Substring(0, lastBrace + 1);
            }

            // Fix common JSON issues
            cleaned = cleaned.Replace("\r\n", "\n");
            cleaned = cleaned.Replace("\r", "\n");

            // Remove zero-width characters
            cleaned = Regex.Replace(cleaned, @"[\u200B-\u200D\uFEFF]", "");

            // Ensure proper quotes (replace smart quotes)
            cleaned = cleaned.Replace('"', '"').Replace('"', '"');
            cleaned = cleaned.Replace('"', '\'').Replace('"', '\'');

            return cleaned.Trim();
        }

        // ✅ VALIDATE AI Result
        private void ValidateAIResult(AIAnalysisResult aiData, FloodReport report)
        {
            // Validate waterLevel
            var validLevels = new[] { "Low", "Medium", "High", "Dangerous" };
            if (!validLevels.Contains(aiData.WaterLevel))
            {
                _logger.LogWarning("⚠️  Invalid waterLevel: {Level}, defaulting to Medium", aiData.WaterLevel);
                aiData.WaterLevel = "Medium";
            }

            // Ensure analysis
            if (string.IsNullOrWhiteSpace(aiData.Analysis))
            {
                aiData.Analysis = $"Khu vực {report.Address} bị ngập mức {aiData.WaterLevel}. " +
                                $"Độ sâu: {aiData.EstimatedDepth}. Cần theo dõi và xử lý kịp thời.";
            }

            // Ensure recommendations
            if (string.IsNullOrWhiteSpace(aiData.Recommendations))
            {
                aiData.Recommendations = GenerateDefaultRecommendations(aiData.WaterLevel);
            }

            // Validate depth format
            if (!Regex.IsMatch(aiData.EstimatedDepth, @"\d+-\d+cm"))
            {
                aiData.EstimatedDepth = "20-40cm";
            }

            // Validate confidence
            var validConfidence = new[] { "low", "medium", "high" };
            if (!validConfidence.Contains(aiData.Confidence.ToLower()))
            {
                aiData.Confidence = "medium";
            }
        }

        // ✅ CREATE FALLBACK RESULT
        private AIAnalysisResult CreateFallbackResult(string text, FloodReport report)
        {
            _logger.LogWarning("⚠️  Using fallback parsing");

            var waterLevel = ExtractWaterLevel(text);
            var depth = ExtractDepth(text);

            return new AIAnalysisResult
            {
                WaterLevel = waterLevel,
                EstimatedDepth = depth,
                Confidence = "medium",
                Analysis = $"Phân tích tự động: Khu vực {report.Address} đang bị ngập mức {waterLevel.ToLower()}. " +
                          $"Độ sâu ước tính {depth}. " +
                          ExtractAnalysisFromText(text),
                Recommendations = GenerateDefaultRecommendations(waterLevel)
            };
        }

        // ✅ TRUNCATE TEXT
        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text ?? "";

            var truncated = text.Substring(0, maxLength - 20);
            var lastPeriod = truncated.LastIndexOf('.');
            var lastNewline = truncated.LastIndexOf('\n');
            var cutPoint = Math.Max(lastPeriod, lastNewline);

            if (cutPoint > maxLength / 2)
            {
                return truncated.Substring(0, cutPoint + 1) + "\n\n...(rút gọn)";
            }

            return truncated + "...";
        }

        // ✅ EXTRACT ANALYSIS FROM TEXT
        private string ExtractAnalysisFromText(string text)
        {
            // Try to find analysis section
            var analysisMatch = Regex.Match(text, @"analysis[""']?\s*:\s*[""'](.+?)[""']",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (analysisMatch.Success)
            {
                return TruncateText(analysisMatch.Groups[1].Value.Trim(), 300);
            }

            // Fallback: first 200 chars
            return TruncateText(text, 200);
        }

        // ✅ EXTRACT WATER LEVEL
        private string ExtractWaterLevel(string text)
        {
            text = text.ToLower();

            if (Regex.IsMatch(text, @"dangerous|nguy hiểm|>[\s]*60|trên[\s]*60"))
                return "Dangerous";

            if (Regex.IsMatch(text, @"high|cao|40[\s]*-[\s]*60|50[\s]*cm"))
                return "High";

            if (Regex.IsMatch(text, @"low|thấp|<[\s]*20|10[\s]*-[\s]*20"))
                return "Low";

            return "Medium";
        }

        // ✅ EXTRACT DEPTH
        private string ExtractDepth(string text)
        {
            var match = Regex.Match(text, @"(\d+)\s*-\s*(\d+)\s*cm");
            if (match.Success)
            {
                return $"{match.Groups[1].Value}-{match.Groups[2].Value}cm";
            }

            match = Regex.Match(text, @"(\d+)\s*cm");
            if (match.Success)
            {
                var depth = int.Parse(match.Groups[1].Value);
                var min = Math.Max(10, depth - 5);
                var max = depth + 5;
                return $"{min}-{max}cm";
            }

            return "20-40cm";
        }

        // ✅ DEFAULT RECOMMENDATIONS
        private string GenerateDefaultRecommendations(string waterLevel)
        {
            return waterLevel switch
            {
                "Dangerous" =>
                    "1. SƠ TÁN dân khẩn cấp đến nơi an toàn cao\n" +
                    "2. CHỐT CHẶN cấm tuyệt đối người/xe qua\n" +
                    "3. HUY ĐỘNG cảnh sát, quân đội cứu hộ\n" +
                    "4. NGẮT ĐIỆN khu vực tránh chập điện\n" +
                    "5. CHUẨN BỊ y tế, xe cứu thương khẩn cấp",

                "High" =>
                    "1. DỰNG biển cảnh báo, đèn đỏ nháy\n" +
                    "2. CHỐT người hướng dẫn đi đường khác\n" +
                    "3. TRIỂN KHAI máy bơm công suất lớn\n" +
                    "4. THEO DÕI 24/7, sẵn sàng sơ tán\n" +
                    "5. HỖ TRỢ dân di chuyển tài sản cao",

                "Medium" =>
                    "1. DỰNG biển cảnh báo, đèn vàng nháy\n" +
                    "2. HƯỚNG DẪN xe chạy chậm, tránh ngập\n" +
                    "3. BƠM NƯỚC máy 30-50HP tại điểm trũng\n" +
                    "4. CỬ người theo dõi diễn biến\n" +
                    "5. CẬP NHẬT qua app SmartCity",

                _ =>
                    "1. THEO DÕI dự báo thời tiết liên tục\n" +
                    "2. NHẮC NHỞ chú ý trơn trượt khi đi\n" +
                    "3. CHUẨN BỊ máy bơm, bao cát dự phòng\n" +
                    "4. VỆ SINH nạo vét cống rãnh\n" +
                    "5. GHI NHẬN vị trí để nâng cấp sau"
            };
        }

        public class AIAnalysisResult
        {
            public string WaterLevel { get; set; } = "Medium";
            public string EstimatedDepth { get; set; } = "20-40cm";
            public string Confidence { get; set; } = "medium";
            public string Analysis { get; set; } = "";
            public string Recommendations { get; set; } = "";
        }
    }
}
