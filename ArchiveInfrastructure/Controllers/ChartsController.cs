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
        private record CountByLanguageResponseItem(string Language, int Count);
        private record CountByStateResponseItem(string State, int Count);

        private readonly DbarchiveContext _context;

        public ChartsController(DbarchiveContext context)
        {
            _context = context;
        }

        // 🔹 API: Кількість документів за мовами
        [HttpGet("countByLanguage")]
        public async Task<JsonResult> GetCountByLanguageAsync(CancellationToken cancellationToken)
        {
            var responseItems = await _context.Documents
                .GroupBy(d => d.Language)
                .Select(group => new CountByLanguageResponseItem(group.Key, group.Count()))
                .ToListAsync(cancellationToken);

            return new JsonResult(responseItems);
        }

        // 🔹 API: Кількість екземплярів документів за станом
        [HttpGet("countByState")]
        public async Task<JsonResult> GetCountByStateAsync(CancellationToken cancellationToken)
        {
            var responseItems = await _context.DocumentInstances
                .GroupBy(di => di.State)
                .Select(group => new CountByStateResponseItem(group.Key, group.Count()))
                .ToListAsync(cancellationToken);

            return new JsonResult(responseItems);
        }
    }
}
