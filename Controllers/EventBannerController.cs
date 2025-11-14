using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCity_BE.Data;
using SmartCity_BE.DTOs;

namespace Smartcity_BE.Controllers
{
    [ApiController]
    [Route("api/EventBanners")]
    public class EventBannerController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EventBannerController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<EventBannerDto>>> GetEventBanners()
        {
            var banner = await _context.EventBanners
                .Select(b => new EventBannerDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Description = b.Description,
                    ImageUrl = b.ImageUrl
                })
                .ToListAsync();

            return Ok(banner);
        }
    }
}