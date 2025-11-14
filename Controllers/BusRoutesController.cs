using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCity_BE.Data;
using SmartCity_BE.DTOs;
using SmartCity_BE.Models;
using System.Text.Json;

namespace SmartCity_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BusRoutesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BusRoutesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ⭐ Helper method để parse stops
        private List<string> ParseStops(string? stopsJson)
        {
            if (string.IsNullOrEmpty(stopsJson))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(stopsJson) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        // GET: /api/BusRoutes
        // Lấy tất cả các tuyến xe buýt
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BusRouteDto>>> GetBusRoutes()
        {
            // ⭐ Lấy data trước, sau đó parse JSON
            var routes = await _context.BusRoutes
                .Where(r => r.IsActive)
                .ToListAsync();

            var routeDtos = routes.Select(route => new BusRouteDto
            {
                Id = route.Id,
                RouteNumber = route.RouteNumber,
                RouteName = route.RouteName,
                Schedule = route.Schedule,
                Description = route.Description,
                StartPoint = route.StartPoint,
                EndPoint = route.EndPoint,
                Price = route.Price,
                FirstBusTime = route.FirstBusTime,
                LastBusTime = route.LastBusTime,
                TripDuration = route.TripDuration,
                Stops = ParseStops(route.StopsJson), // ⭐ Parse sau khi query
                ImageUrl = route.ImageUrl,
                IsActive = route.IsActive
            }).ToList();

            return Ok(routeDtos);
        }

        // GET: /api/BusRoutes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BusRouteDto>> GetBusRoute(int id)
        {
            var route = await _context.BusRoutes.FindAsync(id);

            if (route == null)
            {
                return NotFound(new { message = "Không tìm thấy tuyến xe" });
            }

            var dto = new BusRouteDto
            {
                Id = route.Id,
                RouteNumber = route.RouteNumber,
                RouteName = route.RouteName,
                Schedule = route.Schedule,
                Description = route.Description,
                StartPoint = route.StartPoint,
                EndPoint = route.EndPoint,
                Price = route.Price,
                FirstBusTime = route.FirstBusTime,
                LastBusTime = route.LastBusTime,
                TripDuration = route.TripDuration,
                Stops = ParseStops(route.StopsJson), // ⭐ Parse
                ImageUrl = route.ImageUrl,
                IsActive = route.IsActive
            };

            return Ok(dto);
        }

        // GET: /api/BusRoutes/search?q=01
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<BusRouteDto>>> SearchBusRoutes([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(new { message = "Từ khóa tìm kiếm không được để trống" });
            }

            // ⭐ Lấy data trước
            var routes = await _context.BusRoutes
                .Where(r => r.IsActive &&
                       (r.RouteNumber.Contains(q) ||
                        r.RouteName.Contains(q) ||
                        (r.StartPoint != null && r.StartPoint.Contains(q)) ||
                        (r.EndPoint != null && r.EndPoint.Contains(q))))
                .ToListAsync();

            // ⭐ Parse sau
            var routeDtos = routes.Select(route => new BusRouteDto
            {
                Id = route.Id,
                RouteNumber = route.RouteNumber,
                RouteName = route.RouteName,
                Schedule = route.Schedule,
                Description = route.Description,
                StartPoint = route.StartPoint,
                EndPoint = route.EndPoint,
                Price = route.Price,
                FirstBusTime = route.FirstBusTime,
                LastBusTime = route.LastBusTime,
                TripDuration = route.TripDuration,
                Stops = ParseStops(route.StopsJson),
                ImageUrl = route.ImageUrl,
                IsActive = route.IsActive
            }).ToList();

            return Ok(routeDtos);
        }

        // POST: /api/BusRoutes
        [HttpPost]
        public async Task<ActionResult<BusRouteDto>> CreateBusRoute(CreateUpdateBusRouteDto dto)
        {
            var busRoute = new BusRoute
            {
                RouteNumber = dto.RouteNumber,
                RouteName = dto.RouteName,
                Schedule = dto.Schedule,
                Description = dto.Description,
                StartPoint = dto.StartPoint,
                EndPoint = dto.EndPoint,
                Price = dto.Price,
                FirstBusTime = dto.FirstBusTime,
                LastBusTime = dto.LastBusTime,
                TripDuration = dto.TripDuration,
                StopsJson = dto.Stops != null && dto.Stops.Any()
                    ? JsonSerializer.Serialize(dto.Stops)
                    : null,
                ImageUrl = dto.ImageUrl,
                IsActive = dto.IsActive
            };

            _context.BusRoutes.Add(busRoute);
            await _context.SaveChangesAsync();

            var resultDto = new BusRouteDto
            {
                Id = busRoute.Id,
                RouteNumber = busRoute.RouteNumber,
                RouteName = busRoute.RouteName,
                Schedule = busRoute.Schedule,
                Description = busRoute.Description,
                StartPoint = busRoute.StartPoint,
                EndPoint = busRoute.EndPoint,
                Price = busRoute.Price,
                FirstBusTime = busRoute.FirstBusTime,
                LastBusTime = busRoute.LastBusTime,
                TripDuration = busRoute.TripDuration,
                Stops = dto.Stops,
                ImageUrl = busRoute.ImageUrl,
                IsActive = busRoute.IsActive
            };

            return CreatedAtAction(nameof(GetBusRoute), new { id = busRoute.Id }, resultDto);
        }

        // PUT: /api/BusRoutes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBusRoute(int id, CreateUpdateBusRouteDto dto)
        {
            var busRoute = await _context.BusRoutes.FindAsync(id);
            if (busRoute == null)
            {
                return NotFound(new { message = "Không tìm thấy tuyến xe" });
            }

            busRoute.RouteNumber = dto.RouteNumber;
            busRoute.RouteName = dto.RouteName;
            busRoute.Schedule = dto.Schedule;
            busRoute.Description = dto.Description;
            busRoute.StartPoint = dto.StartPoint;
            busRoute.EndPoint = dto.EndPoint;
            busRoute.Price = dto.Price;
            busRoute.FirstBusTime = dto.FirstBusTime;
            busRoute.LastBusTime = dto.LastBusTime;
            busRoute.TripDuration = dto.TripDuration;
            busRoute.StopsJson = dto.Stops != null && dto.Stops.Any()
                ? JsonSerializer.Serialize(dto.Stops)
                : null;
            busRoute.ImageUrl = dto.ImageUrl;
            busRoute.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật tuyến xe thành công" });
        }

        // DELETE: /api/BusRoutes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBusRoute(int id)
        {
            var busRoute = await _context.BusRoutes.FindAsync(id);
            if (busRoute == null)
            {
                return NotFound(new { message = "Không tìm thấy tuyến xe" });
            }

            // Soft delete
            busRoute.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa tuyến xe thành công" });
        }
    }
}
