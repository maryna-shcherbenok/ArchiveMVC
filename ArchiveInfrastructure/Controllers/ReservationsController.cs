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

        // GET: Reservations
        public async Task<IActionResult> Index(int? userId)
        {
            IQueryable<Reservation> reservations = _context.Reservations
                .Include(r => r.User)
                .AsNoTracking();

            if (userId.HasValue)
            {
                reservations = reservations.Where(r => r.UserId == userId);
                ViewBag.UserId = userId;
                ViewBag.UserName = _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => u.Email)
                    .FirstOrDefault();
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
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reservation == null) return NotFound();

            return View(reservation);
        }

        // GET: Reservations/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email");
            return View();
        }

        // POST: Reservations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,ReservationStartDate,ReservationEndDate,Id")] Reservation reservation)
        {
            if (ModelState.IsValid)
            {
                // Перевірка на конфлікт дат бронювання
                var overlappingReservation = await _context.Reservations
                    .FirstOrDefaultAsync(r => r.UserId == reservation.UserId &&
                        ((reservation.ReservationStartDate >= r.ReservationStartDate && reservation.ReservationStartDate <= r.ReservationEndDate) ||
                         (reservation.ReservationEndDate >= r.ReservationStartDate && reservation.ReservationEndDate <= r.ReservationEndDate)));

                if (overlappingReservation != null)
                {
                    ModelState.AddModelError("", "Користувач вже має бронювання в цьому часовому діапазоні.");
                    return View(reservation);
                }

                _context.Add(reservation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", reservation.UserId);
            return View(reservation);
        }

        // GET: Reservations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null) return NotFound();

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", reservation.UserId);
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
                // Перевірка на конфлікт дат бронювання
                var overlappingReservation = await _context.Reservations
                    .FirstOrDefaultAsync(r => r.UserId == reservation.UserId &&
                        r.Id != reservation.Id &&
                        ((reservation.ReservationStartDate >= r.ReservationStartDate && reservation.ReservationStartDate <= r.ReservationEndDate) ||
                         (reservation.ReservationEndDate >= r.ReservationStartDate && reservation.ReservationEndDate <= r.ReservationEndDate)));

                if (overlappingReservation != null)
                {
                    ModelState.AddModelError("", "Ці дати перетинаються з існуючим бронюванням.");
                    return View(reservation);
                }

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

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", reservation.UserId);
            return View(reservation);
        }

        // GET: Reservations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.ReservationDocuments)
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
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null) return NotFound();

            // Перевірка, чи бронювання має пов’язані екземпляри документів
            if (reservation.ReservationDocuments.Any())
            {
                ModelState.AddModelError("", "Неможливо видалити бронювання, оскільки воно містить зарезервовані документи.");
                return View(reservation);
            }

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}

