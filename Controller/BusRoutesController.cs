using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCity_BE.Data; // Đảm bảo using đúng namespace
using SmartCity_BE.DTOs; // Đảm bảo using đúng namespace

[ApiController]
[Route("api/[controller]")] // Đường dẫn sẽ là /api/BusRoutes
public class BusRoutesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BusRoutesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /api/BusRoutes
    // Lấy tất cả các tuyến xe buýt
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BusRouteDto>>> GetBusRoutes()
    {
        var routes = await _context.BusRoutes
            .Select(route => new BusRouteDto
            {
                Id = route.Id,
                RouteNumber = route.RouteNumber,
                RouteName = route.RouteName,
                Schedule = route.Schedule
            })
            .ToListAsync();

        return Ok(routes);
    }
}
