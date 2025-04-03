using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ArchiveDomain.Model;
using ArchiveInfrastructure;
using Microsoft.AspNetCore.Authorization;

namespace ArchiveInfrastructure.Controllers;

[Authorize(Roles = "admin")]
public class DocumentInstancesController : Controller
{
    private readonly DbarchiveContext _context;

    public DocumentInstancesController(DbarchiveContext context)
    {
        _context = context;
    }

    // GET: DocumentInstances
    public async Task<IActionResult> Index(string inventoryNumber, string state, bool? available, int? documentId)
    {
        IQueryable<DocumentInstance> instances = _context.DocumentInstances
            .Include(d => d.Document)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(inventoryNumber))
            instances = instances.Where(di => di.InventoryNumber.ToString().Contains(inventoryNumber));

        if (!string.IsNullOrEmpty(state))
            instances = instances.Where(di => di.State.Contains(state));

        if (available.HasValue)
            instances = instances.Where(di => di.Available == available.Value);

        if (documentId.HasValue)
            instances = instances.Where(di => di.DocumentId == documentId);

        ViewBag.DocumentList = new SelectList(await _context.Documents
            .Select(d => new { d.Id, d.Title })
            .ToListAsync(), "Id", "Title");

        return View(await instances.ToListAsync());
    }

    // GET: DocumentInstances/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var documentInstance = await _context.DocumentInstances
            .Include(d => d.Document)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (documentInstance == null) return NotFound();

        return View(documentInstance);
    }

    // GET: DocumentInstances/Create
    public IActionResult Create()
    {
        ViewData["DocumentId"] = new SelectList(_context.Documents, "Id", "Title");
        return View();
    }

    // POST: DocumentInstances/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("State,Available,DocumentId")] DocumentInstance documentInstance)
    {
        if (ModelState.IsValid)
        {
            const int inventoryPrefix = 11120000;
            int nextNumber;

            var usedNumbers = await _context.DocumentInstances
                .Select(di => di.InventoryNumber)
                .Where(n => n >= 11120001)
                .ToListAsync();

            if (usedNumbers.Count == 0)
            {
                nextNumber = inventoryPrefix + 1;
            }
            else
            {
                var allPossibleNumbers = Enumerable.Range(1, usedNumbers.Max() - inventoryPrefix)
                    .Select(n => inventoryPrefix + n)
                    .Except(usedNumbers)
                    .OrderBy(n => n)
                    .ToList();

                nextNumber = allPossibleNumbers.Any() ? allPossibleNumbers.First() : usedNumbers.Max() + 1;
            }

            documentInstance.InventoryNumber = nextNumber;

            _context.DocumentInstances.Add(documentInstance);
            await _context.SaveChangesAsync();

            // 🔄 Оновлюємо кількість
            await UpdateDocumentQuantityAsync(documentInstance.DocumentId);

            return RedirectToAction(nameof(Index));
        }

        ViewData["DocumentId"] = new SelectList(_context.Documents, "Id", "Title", documentInstance.DocumentId);
        return View(documentInstance);
    }

    // GET: DocumentInstances/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var documentInstance = await _context.DocumentInstances
            .Include(di => di.Document)
            .FirstOrDefaultAsync(di => di.Id == id);

        if (documentInstance == null) return NotFound();

        return View(documentInstance);
    }

    // POST: DocumentInstances/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,State,Available,DocumentId")] DocumentInstance documentInstance)
    {
        if (id != documentInstance.Id) return NotFound();

        var existingInstance = await _context.DocumentInstances
            .AsNoTracking()
            .FirstOrDefaultAsync(di => di.Id == id);

        if (existingInstance == null) return NotFound();

        documentInstance.InventoryNumber = existingInstance.InventoryNumber;

        try
        {
            _context.Update(documentInstance);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.DocumentInstances.Any(e => e.Id == documentInstance.Id))
                return NotFound();
            else
                throw;
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: DocumentInstances/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var documentInstance = await _context.DocumentInstances
            .Include(d => d.Document)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (documentInstance == null) return NotFound();

        return View(documentInstance);
    }

    // POST: DocumentInstances/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var documentInstance = await _context.DocumentInstances
            .Include(di => di.ReservationDocuments)
                .ThenInclude(rd => rd.Reservation)
            .FirstOrDefaultAsync(di => di.Id == id);

        if (documentInstance == null) return NotFound();

        int documentId = documentInstance.DocumentId;

        var reservationsToDelete = documentInstance.ReservationDocuments
            .Select(rd => rd.Reservation)
            .Distinct()
            .ToList();

        foreach (var reservation in reservationsToDelete)
        {
            _context.ReservationDocuments.RemoveRange(reservation.ReservationDocuments);
            _context.Reservations.Remove(reservation);
        }

        _context.DocumentInstances.Remove(documentInstance);
        await _context.SaveChangesAsync();

        var remainingInstances = await _context.DocumentInstances
            .Where(di => di.DocumentId == documentId)
            .CountAsync();

        if (remainingInstances == 0)
        {
            var document = await _context.Documents
                .Include(d => d.AuthorDocuments)
                .Include(d => d.CategoryDocuments)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document != null)
            {
                _context.AuthorDocuments.RemoveRange(document.AuthorDocuments);
                _context.CategoryDocuments.RemoveRange(document.CategoryDocuments);
                _context.Documents.Remove(document);
                await _context.SaveChangesAsync();
            }
        }
        else
        {
            // 🔄 Оновлюємо кількість, якщо документ залишився
            await UpdateDocumentQuantityAsync(documentId);
        }

        return RedirectToAction(nameof(Index));
    }

    // 🔧 Метод для оновлення кількості екземплярів
    private async Task UpdateDocumentQuantityAsync(int documentId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document != null)
        {
            int count = await _context.DocumentInstances
                .CountAsync(di => di.DocumentId == documentId);
            document.Quantity = count;
            await _context.SaveChangesAsync();
        }
    }
}
