using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ArchiveInfrastructure;

namespace ArchiveInfrastructure.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChartsController : ControllerBase
    {
        private record CountByStateResponseItem(string State, int Count);

        private readonly DbarchiveContext _context;

        public ChartsController(DbarchiveContext context)
        {
            _context = context;
        }

        // API: Кількість екземплярів документів за станом
        [HttpGet("countByState")]
        public async Task<JsonResult> GetCountByStateAsync(CancellationToken cancellationToken)
        {
            var responseItems = await _context.DocumentInstances
                .GroupBy(di => di.State)
                .Select(group => new CountByStateResponseItem(group.Key, group.Count()))
                .ToListAsync(cancellationToken);

            return new JsonResult(responseItems);
        }

        // API: Кількість бронювань по місяцях
        [HttpGet("countReservationsByMonth")]
        public async Task<JsonResult> GetCountReservationsByMonthAsync(CancellationToken cancellationToken)
        {
            var items = await _context.Reservations
                .GroupBy(r => new { r.ReservationStartDateTime.Year, r.ReservationStartDateTime.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    MonthNumber = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.MonthNumber)
                .ToListAsync(cancellationToken);

            // Форматуємо дату вже після виконання SQL (in-memory)
            var formatted = items.Select(x => new
            {
                Month = $"{x.Year}-{x.MonthNumber:D2}", // тепер без помилок
                x.Count
            });

            return new JsonResult(formatted);
        }
    }
}
