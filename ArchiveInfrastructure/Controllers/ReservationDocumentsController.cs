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
    public class ReservationDocumentsController : Controller
    {
        private readonly DbarchiveContext _context;

        public ReservationDocumentsController(DbarchiveContext context)
        {
            _context = context;
        }

        // GET: ReservationDocuments
        public async Task<IActionResult> Index()
        {
            var dbarchiveContext = _context.ReservationDocuments.Include(r => r.DocumentInstance).Include(r => r.Reservation);
            return View(await dbarchiveContext.ToListAsync());
        }

        // GET: ReservationDocuments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservationDocument = await _context.ReservationDocuments
                .Include(r => r.DocumentInstance)
                .Include(r => r.Reservation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (reservationDocument == null)
            {
                return NotFound();
            }

            return View(reservationDocument);
        }

        // GET: ReservationDocuments/Create
        public IActionResult Create()
        {
            ViewData["DocumentInstanceId"] = new SelectList(_context.DocumentInstances, "Id", "State");
            ViewData["ReservationId"] = new SelectList(_context.Reservations, "Id", "Id");
            return View();
        }

        // POST: ReservationDocuments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ReservationId,DocumentInstanceId,Id")] ReservationDocument reservationDocument)
        {
            if (ModelState.IsValid)
            {
                _context.Add(reservationDocument);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DocumentInstanceId"] = new SelectList(_context.DocumentInstances, "Id", "State", reservationDocument.DocumentInstanceId);
            ViewData["ReservationId"] = new SelectList(_context.Reservations, "Id", "Id", reservationDocument.ReservationId);
            return View(reservationDocument);
        }

        // GET: ReservationDocuments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservationDocument = await _context.ReservationDocuments.FindAsync(id);
            if (reservationDocument == null)
            {
                return NotFound();
            }
            ViewData["DocumentInstanceId"] = new SelectList(_context.DocumentInstances, "Id", "State", reservationDocument.DocumentInstanceId);
            ViewData["ReservationId"] = new SelectList(_context.Reservations, "Id", "Id", reservationDocument.ReservationId);
            return View(reservationDocument);
        }

        // POST: ReservationDocuments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ReservationId,DocumentInstanceId,Id")] ReservationDocument reservationDocument)
        {
            if (id != reservationDocument.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reservationDocument);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReservationDocumentExists(reservationDocument.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["DocumentInstanceId"] = new SelectList(_context.DocumentInstances, "Id", "State", reservationDocument.DocumentInstanceId);
            ViewData["ReservationId"] = new SelectList(_context.Reservations, "Id", "Id", reservationDocument.ReservationId);
            return View(reservationDocument);
        }

        // GET: ReservationDocuments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservationDocument = await _context.ReservationDocuments
                .Include(r => r.DocumentInstance)
                .Include(r => r.Reservation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (reservationDocument == null)
            {
                return NotFound();
            }

            return View(reservationDocument);
        }

        // POST: ReservationDocuments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reservationDocument = await _context.ReservationDocuments.FindAsync(id);
            if (reservationDocument != null)
            {
                _context.ReservationDocuments.Remove(reservationDocument);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ReservationDocumentExists(int id)
        {
            return _context.ReservationDocuments.Any(e => e.Id == id);
        }
    }
}
