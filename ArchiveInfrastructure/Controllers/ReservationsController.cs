using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArchiveDomain.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchiveInfrastructure.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly DbarchiveContext _context;
        private readonly UserManager<User> _userManager;

        public ReservationsController(DbarchiveContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 🔒 Адміністратор: Перегляд усіх бронювань
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Index()
        {
            var reservations = await _context.Reservations
                .Include(r => r.ReservationDocuments)
                    .ThenInclude(rd => rd.DocumentInstance)
                        .ThenInclude(di => di.Document)
                .ToListAsync();

            return View(reservations);
        }

        // 👤 Користувач: Перегляд своїх бронювань
        public async Task<IActionResult> MyReservations()
        {
            var userId = _userManager.GetUserId(User);

            var reservations = await _context.Reservations
                .Include(r => r.ReservationDocuments)
                    .ThenInclude(rd => rd.DocumentInstance)
                        .ThenInclude(di => di.Document)
                .Where(r => r.UserId == userId)
                .ToListAsync();

            return View(reservations);
        }

        // ✅ Бронювання документа з екземпляра
        public async Task<IActionResult> CreateFromInstance(int documentInstanceId)
        {
            var documentInstance = await _context.DocumentInstances
                .Include(d => d.Document)
                .FirstOrDefaultAsync(d => d.Id == documentInstanceId);

            if (documentInstance == null || !documentInstance.Available)
                return NotFound("Документ недоступний для бронювання.");

            var reservation = new Reservation
            {
                UserId = _userManager.GetUserId(User),
                ReservationStartDate = DateOnly.FromDateTime(DateTime.Today),
                ReservationDocuments = new List<ReservationDocument>
                {
                    new ReservationDocument
                    {
                        DocumentInstanceId = documentInstanceId
                    }
                }
            };

            _context.Reservations.Add(reservation);
            documentInstance.Available = false;

            await _context.SaveChangesAsync();

            return RedirectToAction("MyReservations");
        }

        // ❌ Скасування бронювання користувачем
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.ReservationDocuments)
                    .ThenInclude(rd => rd.DocumentInstance)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null || reservation.UserId != _userManager.GetUserId(User))
                return NotFound();

            // Звільнити екземпляри
            foreach (var rd in reservation.ReservationDocuments)
            {
                if (rd.DocumentInstance != null)
                    rd.DocumentInstance.Available = true;
            }

            // Спочатку видалити зв’язки
            _context.ReservationDocuments.RemoveRange(reservation.ReservationDocuments);
            _context.Reservations.Remove(reservation);

            await _context.SaveChangesAsync();
            return RedirectToAction("MyReservations");
        }

        // 🔍 Адмін: Деталі бронювання
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.ReservationDocuments)
                    .ThenInclude(rd => rd.DocumentInstance)
                        .ThenInclude(di => di.Document)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null) return NotFound();

            return View(reservation);
        }

        // 🗑️ Адмін: Видалення бронювання
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.ReservationDocuments)
                    .ThenInclude(rd => rd.DocumentInstance)
                        .ThenInclude(di => di.Document)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reservation == null) return NotFound();

            return View(reservation);
        }

        // POST: Reservations/Delete/5 (адмін)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.ReservationDocuments)
                    .ThenInclude(rd => rd.DocumentInstance)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation != null)
            {
                foreach (var rd in reservation.ReservationDocuments)
                {
                    if (rd.DocumentInstance != null)
                        rd.DocumentInstance.Available = true;
                }

                _context.ReservationDocuments.RemoveRange(reservation.ReservationDocuments);
                _context.Reservations.Remove(reservation);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
