using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ArchiveDomain.Model;
using ArchiveInfrastructure;

namespace ArchiveInfrastructure.Controllers
{
    public class ReservationsController : Controller
    {
        private readonly DbarchiveContext _context;

        public ReservationsController(DbarchiveContext context)
        {
            _context = context;
        }

        // GET: Reservations (Список бронювань з пошуком)
        public async Task<IActionResult> Index(string readerCardNumber, string inventoryNumber)
        {
            IQueryable<Reservation> reservations = _context.Reservations
                .Include(r => r.User)
                .Include(r => r.ReservationDocuments)
                    .ThenInclude(rd => rd.DocumentInstance)
                .AsNoTracking();

            // 🔹 Фільтр за ReaderCardNumber (Читацький квиток)
            if (!string.IsNullOrEmpty(readerCardNumber))
            {
                if (int.TryParse(readerCardNumber, out int parsedReaderCardNumber))
                {
                    reservations = reservations.Where(r => r.User.ReaderCardNumber == parsedReaderCardNumber);
                    ViewBag.ReaderCardNumber = parsedReaderCardNumber;
                }
                else
                {
                    ModelState.AddModelError("", "Неправильний формат номера читацького квитка.");
                }
            }

            // 🔹 Фільтр за InventoryNumber (Інвентарний номер)
            if (!string.IsNullOrEmpty(inventoryNumber))
            {
                if (int.TryParse(inventoryNumber, out int parsedInventoryNumber))
                {
                    reservations = reservations.Where(r => r.ReservationDocuments.Any(rd => rd.DocumentInstance.InventoryNumber == parsedInventoryNumber));
                    ViewBag.InventoryNumber = parsedInventoryNumber;
                }
                else
                {
                    ModelState.AddModelError("", "Неправильний формат інвентарного номера.");
                }
            }

            return View(await reservations.ToListAsync());
        }

        // GET: Reservations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.ReservationDocuments)
                 .ThenInclude(rd => rd.DocumentInstance)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reservation == null) return NotFound();

            return View(reservation);
        }

        // GET: Reservations/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "ReaderCardNumber");

            // Вибираємо тільки доступні екземпляри документів
            ViewData["DocumentInstances"] = new MultiSelectList(
                _context.DocumentInstances.Where(di => di.Available),
                "Id",
                "InventoryNumber"
            );

            return View();
        }

        // POST: Reservations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,ReservationStartDate,ReservationEndDate,Id")] Reservation reservation, int[] documentInstanceIds)
        {
            if (documentInstanceIds == null || !documentInstanceIds.Any())
            {
                ModelState.AddModelError("", "Не можна створити бронювання без вибраного екземпляра документа.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["UserId"] = new SelectList(_context.Users, "Id", "ReaderCardNumber", reservation.UserId);
                ViewData["DocumentInstances"] = new MultiSelectList(
                    _context.DocumentInstances.Where(di => di.Available),
                    "Id",
                    "InventoryNumber"
                );
                return View(reservation);  // Повертаємо користувача на ту ж сторінку з помилкою
            }

            _context.Add(reservation);
            await _context.SaveChangesAsync();

            foreach (var instanceId in documentInstanceIds)
            {
                var documentInstance = await _context.DocumentInstances.FindAsync(instanceId);
                if (documentInstance != null && documentInstance.Available)
                {
                    documentInstance.Available = false;
                    _context.ReservationDocuments.Add(new ReservationDocument
                    {
                        ReservationId = reservation.Id,
                        DocumentInstanceId = instanceId
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Reservations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.ReservationDocuments)
                .ThenInclude(rd => rd.DocumentInstance)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reservation == null) return NotFound();

            var reservedInstanceIds = reservation.ReservationDocuments
                .Select(rd => rd.DocumentInstanceId)
                .ToList();

            var availableInstances = await _context.DocumentInstances
                .Where(di => di.Available || reservedInstanceIds.Contains(di.Id))
                .AsNoTracking()
                .ToListAsync();

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "ReaderCardNumber", reservation.UserId);
            ViewData["DocumentInstances"] = new MultiSelectList(
                availableInstances,
                "Id",
                "InventoryNumber",
                reservedInstanceIds
            );

            return View(reservation);
        }

        // POST: Reservations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,ReservationStartDate,ReservationEndDate,Id")] Reservation reservation)
        {
            if (id != reservation.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reservation);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Reservations.Any(e => e.Id == reservation.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "ReaderCardNumber", reservation.UserId);
            return View(reservation);
        }

        // GET: Reservations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.ReservationDocuments)
                 .ThenInclude(rd => rd.DocumentInstance)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reservation == null) return NotFound();

            return View(reservation);
        }

        // POST: Reservations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.ReservationDocuments)
                    .ThenInclude(rd => rd.DocumentInstance)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null) return NotFound();

            foreach (var rd in reservation.ReservationDocuments)
            {
                if (rd.DocumentInstance != null)
                {
                    rd.DocumentInstance.Available = true;
                }
            }

            _context.ReservationDocuments.RemoveRange(reservation.ReservationDocuments);
            _context.Reservations.Remove(reservation);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
