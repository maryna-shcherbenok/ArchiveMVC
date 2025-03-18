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

public class DocumentsController : Controller
{
    private readonly DbarchiveContext _context;

    public DocumentsController(DbarchiveContext context)
    {
        _context = context;
    }

    // GET: Documents + теж пошук і фільтрація
    public async Task<IActionResult> Index(
        string title, int? authorId, int? categoryId, int? typeId,
        string language, bool? onlyAvailable)
    {
        IQueryable<Document> documents = _context.Documents
            .Include(d => d.Type)
            .Include(d => d.DocumentInstances)
            .Include(d => d.AuthorDocuments).ThenInclude(ad => ad.Author)
            .Include(d => d.CategoryDocuments).ThenInclude(cd => cd.Category)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(title)) { documents = documents.Where(d => d.Title.Contains(title)); }

        if (authorId.HasValue) 
        {
            documents = documents.Where(d => d.AuthorDocuments.Any(ad => ad.AuthorId == authorId));
            ViewBag.AuthorId = authorId;
        }

        if (categoryId.HasValue) 
        {
            documents = documents.Where(d => d.CategoryDocuments.Any(cd => cd.CategoryId == categoryId));
            ViewBag.CategoryId = categoryId;
        }

        if (typeId.HasValue) 
        {
            documents = documents.Where(d => d.TypeId == typeId);
            ViewBag.TypeId = typeId;
        }

        if (!string.IsNullOrEmpty(language)) { documents = documents.Where(d => d.Language.Contains(language)); }

        if (onlyAvailable.HasValue && onlyAvailable.Value) { documents = documents.Where(d => d.DocumentInstances.Any(di => di.Available)); }

        ViewBag.DocumentTypes = await _context.DocumentTypes.ToListAsync();
        ViewBag.Authors = await _context.Authors.ToListAsync();
        ViewBag.Categories = await _context.Categories.ToListAsync();

        return View(await documents.ToListAsync());
    }

    // GET: Documents/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var document = await _context.Documents
            .Include(d => d.Type)
            .Include(d => d.DocumentInstances)
            .Include(d => d.AuthorDocuments).ThenInclude(ad => ad.Author)
            .Include(d => d.CategoryDocuments).ThenInclude(cd => cd.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (document == null) return NotFound();

        return View(document);
    }

    // GET: Documents/Create
    public IActionResult Create(int? authorId, int? categoryId, int? typeId)
    {
        ViewBag.TypeList = new SelectList(_context.DocumentTypes, "Id", "Name", typeId);
        ViewBag.AuthorList = new MultiSelectList(_context.Authors, "Id", "Name");
        ViewBag.CategoryList = new MultiSelectList(_context.Categories, "Id", "Name");

        ViewBag.AuthorId = authorId;
        ViewBag.CategoryId = categoryId;
        ViewBag.TypeId = typeId;

        return View();
    }

    // POST: Documents/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Title,PublicationDate,Language,Info,TypeId,Id")] Document document, int[] authorIds, int[] categoryIds)
    {
        if (ModelState.IsValid)
        {
            document.Quantity = 1;

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            if (authorIds != null)
            {
                foreach (var authorId in authorIds)
                {
                    _context.AuthorDocuments.Add(new AuthorDocument { AuthorId = authorId, DocumentId = document.Id });
                }
            }

            if (categoryIds != null)
            {
                foreach (var categoryId in categoryIds)
                {
                    _context.CategoryDocuments.Add(new CategoryDocument { CategoryId = categoryId, DocumentId = document.Id });
                }
            }

            // Як тільки додасться до системи новий документ, йому автоматично буде присвоєно інверт. номер, стан і т.д.
            var firstInstance = new DocumentInstance
            {
                DocumentId = document.Id,
                InventoryNumber = 1,
                State = "Новий",
                Available = true
            };
            _context.DocumentInstances.Add(firstInstance);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        ViewBag.TypeList = new SelectList(_context.DocumentTypes, "Id", "Name", document.TypeId);
        ViewBag.AuthorList = new MultiSelectList(_context.Authors, "Id", "Name", authorIds);
        ViewBag.CategoryList = new MultiSelectList(_context.Categories, "Id", "Name", categoryIds);

        return View(document);
    }

    // GET: Documents/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var document = await _context.Documents
            .Include(d => d.AuthorDocuments)
            .Include(d => d.CategoryDocuments)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null) return NotFound();

        ViewBag.TypeList = new SelectList(_context.DocumentTypes, "Id", "Name", document.TypeId);
        ViewBag.AuthorList = new MultiSelectList(_context.Authors, "Id", "Name", document.AuthorDocuments.Select(a => a.AuthorId));
        ViewBag.CategoryList = new MultiSelectList(_context.Categories, "Id", "Name", document.CategoryDocuments.Select(c => c.CategoryId));

        return View(document);
    }

    // POST: Documents/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,PublicationDate,Language,Info,Quantity,TypeId")] Document document, int[] authorIds, int[] categoryIds)
    {
        if (id != document.Id) return NotFound();

        if (ModelState.IsValid)
        {
            _context.Update(document);
            await _context.SaveChangesAsync();

            _context.AuthorDocuments.RemoveRange(_context.AuthorDocuments.Where(ad => ad.DocumentId == document.Id));
            foreach (var authorId in authorIds)
            {
                _context.AuthorDocuments.Add(new AuthorDocument { AuthorId = authorId, DocumentId = document.Id });
            }

            _context.CategoryDocuments.RemoveRange(_context.CategoryDocuments.Where(cd => cd.DocumentId == document.Id));
            foreach (var categoryId in categoryIds)
            {
                _context.CategoryDocuments.Add(new CategoryDocument { CategoryId = categoryId, DocumentId = document.Id });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = document.Id });
        }

        return View(document);
    }

    // GET: Documents/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var document = await _context.Documents
            .Include(d => d.Type)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (document == null) return NotFound();

        return View(document);
    }

    // POST: Documents/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var document = await _context.Documents
            .Include(d => d.DocumentInstances)
                .ThenInclude(di => di.ReservationDocuments) // Завантажуємо бронювання екземплярів
            .Include(d => d.AuthorDocuments)
            .Include(d => d.CategoryDocuments)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null) return NotFound();

        // 🔹 1. Знаходимо всі бронювання, пов'язані з екземплярами документа
        var reservationIds = document.DocumentInstances
            .SelectMany(di => di.ReservationDocuments)
            .Select(rd => rd.ReservationId)
            .Distinct()
            .ToList();

        if (reservationIds.Any())
        {
            var reservationsToRemove = await _context.Reservations
                .Where(r => reservationIds.Contains(r.Id))
                .Include(r => r.ReservationDocuments)
                .ToListAsync();

            // 🔹 Видаляємо всі бронювання та їх зв’язки
            _context.ReservationDocuments.RemoveRange(reservationsToRemove.SelectMany(r => r.ReservationDocuments));
            _context.Reservations.RemoveRange(reservationsToRemove);
        }

        // 🔹 2. Видаляємо всі екземпляри документа
        if (document.DocumentInstances.Any())
        {
            _context.DocumentInstances.RemoveRange(document.DocumentInstances);
        }

        // 🔹 3. Видаляємо зв'язки з авторами та категоріями
        _context.AuthorDocuments.RemoveRange(document.AuthorDocuments);
        _context.CategoryDocuments.RemoveRange(document.CategoryDocuments);

        // 🔹 4. Видаляємо сам документ
        _context.Documents.Remove(document);

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
