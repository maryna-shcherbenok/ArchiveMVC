using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ArchiveDomain.Model;
using ArchiveInfrastructure;

namespace ArchiveInfrastructure.Controllers;

public class DocumentInstancesController : Controller
{
    private readonly DbarchiveContext _context;

    public DocumentInstancesController(DbarchiveContext context)
    {
        _context = context;
    }

    // GET: DocumentInstances + додала пошуком та фільтрацію по деяким атрибутам
    public async Task<IActionResult> Index(string inventoryNumber, string state, bool? available, int? documentId)
    {
        IQueryable<DocumentInstance> instances = _context.DocumentInstances
            .Include(d => d.Document)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(inventoryNumber)) { instances = instances.Where(di => di.InventoryNumber.ToString().Contains(inventoryNumber)); }

        if (!string.IsNullOrEmpty(state)) { instances = instances.Where(di => di.State.Contains(state)); }

        if (available.HasValue) { instances = instances.Where(di => di.Available == available.Value); }

        if (documentId.HasValue) { instances = instances.Where(di => di.DocumentId == documentId); }

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
    public IActionResult Create(int? documentId)
    {
        var documents = _context.Documents.ToList();
        //if (!documents.Any()){ModelState.AddModelError("", "Немає доступних документів для створення екземпляра.");}

        if (documentId.HasValue)
        {
            ViewData["DocumentId"] = new SelectList(documents.Where(d => d.Id == documentId), "Id", "Title", documentId);
        }
        else
        {
            ViewData["DocumentId"] = new SelectList(documents, "Id", "Title");
        }

        return View();
    }

    // POST: DocumentInstances/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("InventoryNumber,State,Available,DocumentId,Id")] DocumentInstance documentInstance)
    {
        if (_context.DocumentInstances.Any(di => di.InventoryNumber == documentInstance.InventoryNumber))
        {
            ModelState.AddModelError("InventoryNumber", "Екземпляр документа з таким інвентарним номером вже існує.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["DocumentId"] = new SelectList(_context.Documents, "Id", "Title", documentInstance.DocumentId);
            return View(documentInstance);
        }

        _context.DocumentInstances.Add(documentInstance);
        await _context.SaveChangesAsync();

        // Збільшую Quantity у документа
        var document = await _context.Documents.FindAsync(documentInstance.DocumentId);
        if (document != null)
        {
            document.Quantity += 1;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
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
    public async Task<IActionResult> Edit(int id, [Bind("Id,InventoryNumber,State,Available,DocumentId")] DocumentInstance documentInstance)
    {
        if (id != documentInstance.Id) return NotFound();

        // Перевірка на унікальність InventoryNumber
        bool isDuplicate = await _context.DocumentInstances
            .AnyAsync(di => di.InventoryNumber == documentInstance.InventoryNumber && di.Id != documentInstance.Id);

        if (isDuplicate)
        {
            ModelState.AddModelError("InventoryNumber", "Інвентарний номер вже використовується.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["DocumentId"] = new SelectList(_context.Documents, "Id", "Title", documentInstance.DocumentId);
            return View(documentInstance);
        }

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

        // 🔹 Видаляємо всі пов’язані бронювання
        var reservationsToDelete = documentInstance.ReservationDocuments
            .Select(rd => rd.Reservation)
            .Distinct()
            .ToList();

        foreach (var reservation in reservationsToDelete)
        {
            _context.ReservationDocuments.RemoveRange(reservation.ReservationDocuments);
            _context.Reservations.Remove(reservation);
        }

        // 🔹 Видаляємо сам екземпляр документа
        _context.DocumentInstances.Remove(documentInstance);
        await _context.SaveChangesAsync();

        // 🔹 Перевіряємо, чи залишилися інші екземпляри цього документа
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
            // 🔹 Якщо є ще екземпляри, зменшуємо Quantity
            var document = await _context.Documents.FindAsync(documentId);
            if (document != null && document.Quantity > 1)
            {
                document.Quantity -= 1;
                await _context.SaveChangesAsync();
            }
        }

        return RedirectToAction(nameof(Index));
    }

}
