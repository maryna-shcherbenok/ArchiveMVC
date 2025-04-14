using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArchiveDomain.Model;
using ArchiveInfrastructure.Services;
using ArchiveInfrastructure.ViewModels;
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
        private readonly IEmailService _emailService;

        public ReservationsController(DbarchiveContext context, UserManager<User> userManager, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Index()
        {
            var reservations = await _context.Reservations
                .Include(r => r.ReservationDocuments)
                    .ThenInclude(rd => rd.DocumentInstance)
                        .ThenInclude(di => di.Document)
                .ToListAsync();

            var userIds = reservations
                .Select(r => r.UserId)
                .Where(id => id != null)
                .Distinct()
                .ToList();

            var usersDict = new Dictionary<string, string>();
            foreach (var userId in userIds)
            {
                var user = await _userManager.FindByIdAsync(userId!);
                if (user != null)
                    usersDict[userId!] = user.FullName!;
            }

            ViewBag.UsersDict = usersDict;
            return View(reservations);
        }

        [Authorize(Roles = "user")]
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

        [Authorize(Roles = "user")]
        public async Task<IActionResult> Create(int documentInstanceId)
        {
            var now = DateTime.Now;

            var documentInstance = await _context.DocumentInstances
                .Include(d => d.Document)
                .FirstOrDefaultAsync(d => d.Id == documentInstanceId);

            if (documentInstance == null)
                return NotFound("Екземпляр не знайдено.");

            var isCurrentlyReserved = await _context.Reservations
                .Where(r => r.ReservationDocuments.Any(rd => rd.DocumentInstanceId == documentInstanceId))
                .AnyAsync(r => r.ReservationStartDateTime <= now && r.ReservationEndDateTime >= now);

            if (isCurrentlyReserved)
                return NotFound("Документ зараз заброньований. Виберіть інший час.");

            if (documentInstance == null || !documentInstance.Available || documentInstance.State == "Пошкоджений")
            {
                return NotFound("Документ недоступний для бронювання.");
            }

            var model = new ReservationCreateViewModel
            {
                DocumentInstanceId = documentInstanceId,
                StartDateTime = now,
                EndDateTime = now.AddHours(2)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> Create(ReservationCreateViewModel model)
        {
            var start = model.StartDateTime;
            var end = model.EndDateTime;

            if (end <= start)
                ModelState.AddModelError(nameof(model.EndDateTime), "Дата завершення має бути пізніше за дату початку.");

            if ((end - start).TotalHours > 2)
                ModelState.AddModelError("", "Тривалість бронювання не може перевищувати 2 години.");

            if (start.Date != end.Date)
                ModelState.AddModelError("", "Бронювання повинно здійснюватись в межах одного календарного дня.");

            if (start.DayOfWeek == DayOfWeek.Saturday || start.DayOfWeek == DayOfWeek.Sunday)
                ModelState.AddModelError("", "Бронювання можливе лише в робочі дні (понеділок — пʼятниця).");

            var workingStart = TimeSpan.FromHours(9);
            var workingEnd = TimeSpan.FromHours(17);

            if (start.TimeOfDay < workingStart || end.TimeOfDay > workingEnd)
                ModelState.AddModelError("", "Бронювання можливе лише з 09:00 до 17:00.");

            if (!ModelState.IsValid)
                return View(model);

            var overlapping = await _context.Reservations
            .Where(r => r.ReservationDocuments.Any(rd => rd.DocumentInstanceId == model.DocumentInstanceId))
            .AnyAsync(r =>
                    model.StartDateTime < r.ReservationEndDateTime &&
                    model.EndDateTime > r.ReservationStartDateTime
            );

            if (overlapping)
            {
                ModelState.AddModelError("", "Цей екземпляр уже заброньований на вибраний проміжок часу.");
                return View(model);
            }

            var documentInstance = await _context.DocumentInstances
                .Include(d => d.Document)
                .FirstOrDefaultAsync(d => d.Id == model.DocumentInstanceId);

            if (documentInstance == null)
                return NotFound("Документ недоступний.");

            var reservation = new Reservation
            {
                UserId = _userManager.GetUserId(User),
                ReservationStartDateTime = start,
                ReservationEndDateTime = end,
                ReservationDocuments = new List<ReservationDocument>
                {
                    new ReservationDocument
                    {
                        DocumentInstanceId = model.DocumentInstanceId
                    }
                }
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Бронювання успішно створено.";
            return RedirectToAction("MyReservations");
        }

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

            _context.ReservationDocuments.RemoveRange(reservation.ReservationDocuments);
            _context.Reservations.Remove(reservation);

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Бронювання успішно скасовано.";
            return RedirectToAction("MyReservations");
        }

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
                if (!string.IsNullOrEmpty(reservation.UserId))
                {
                    var user = await _userManager.FindByIdAsync(reservation.UserId);
                    if (user != null)
                    {
                        await _emailService.SendEmailAsync(
                            user.Email!,
                            "Ваше бронювання було скасовано",
                            $"Шановний {user.Email}, ваше бронювання від {reservation.ReservationStartDateTime:g} було скасовано адміністратором.");
                    }
                }

                _context.ReservationDocuments.RemoveRange(reservation.ReservationDocuments);
                _context.Reservations.Remove(reservation);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "user,admin")]
        public async Task<IActionResult> DownloadTicket(int id, [FromServices] IPdfTicketService pdfService)
        {
            var reservation = await _context.Reservations
                .Include(r => r.ReservationDocuments)
                    .ThenInclude(rd => rd.DocumentInstance)
                        .ThenInclude(di => di.Document)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null || (User.IsInRole("user") && reservation.UserId != _userManager.GetUserId(User)))
                return NotFound();

            var pdf = pdfService.GenerateTicketPdf(reservation);
            return File(pdf, "application/pdf", $"Reservation_{id}.pdf");
        }
    }
}