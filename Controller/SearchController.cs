using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;

namespace SmartCity_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Đường dẫn: /api/search
    public class SearchController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly string _locationIQKey;

        public SearchController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            // Đọc API Key bảo mật từ appsettings.json
            _locationIQKey = _config["LocationIQ:ApiKey"] ?? string.Empty;
        }

        // GET: /api/search?q=atm
        // GET: /api/search?q=benh vien
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrEmpty(q) || string.IsNullOrEmpty(_locationIQKey))
            {
                return BadRequest("Thiếu truy vấn tìm kiếm hoặc API key.");
            }

            // Mặc định tìm ở Vũng Tàu (dùng viewbox)
            string viewbox = "107.040,10.320,107.230,10.450"; // Lon,Lat,Lon,Lat

            var client = _httpClientFactory.CreateClient();
            var url = $"https://us1.locationiq.com/v1/search?key={_locationIQKey}&q={q}&format=json&viewbox={viewbox}&bounded=1";

            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    // Trả về JSON y hệt từ LocationIQ
                    return Content(jsonString, "application/json");
                }
                else
                {
                    // Trả về lỗi nếu LocationIQ báo lỗi
                    return StatusCode((int)response.StatusCode, "Lỗi khi gọi LocationIQ API");
                }
            }
            catch (HttpRequestException e)
            {
                return StatusCode(500, $"Lỗi kết nối: {e.Message}");
            }
        }
    }
}