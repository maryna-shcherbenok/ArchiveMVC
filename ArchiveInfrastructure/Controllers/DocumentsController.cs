using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ArchiveDomain.Model;
using ArchiveInfrastructure;
using ArchiveInfrastructure.Services;
using ArchiveInfrastructure.Factories;
using Microsoft.AspNetCore.Authorization;

namespace ArchiveInfrastructure.Controllers;

[Authorize]
public class DocumentsController : Controller
{
    private readonly DbarchiveContext _context;
    private readonly IDataPortServiceFactory<Document> _documentDataPortServiceFactory;

    public DocumentsController(DbarchiveContext context, IDataPortServiceFactory<Document> documentDataPortServiceFactory)
    {
        _context = context;
        _documentDataPortServiceFactory = documentDataPortServiceFactory;
    }

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

        if (!string.IsNullOrEmpty(title))
            documents = documents.Where(d => d.Title.Contains(title));

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

        if (!string.IsNullOrEmpty(language))
            documents = documents.Where(d => d.Language.Contains(language));

        if (onlyAvailable.HasValue && onlyAvailable.Value)
            documents = documents.Where(d => d.DocumentInstances.Any(di => di.Available));

        ViewBag.DocumentTypes = await _context.DocumentTypes.ToListAsync();
        ViewBag.Authors = await _context.Authors.ToListAsync();
        ViewBag.Categories = await _context.Categories.ToListAsync();

        return View(await documents.ToListAsync());
    }

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

            var lastInstance = await _context.DocumentInstances
                .Where(di => di.InventoryNumber >= 11120001)
                .OrderByDescending(di => di.InventoryNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 11120001;

            if (lastInstance != null)
            {
                int lastDigits = int.Parse(lastInstance.InventoryNumber.ToString().Substring(5));
                nextNumber = 11120000 + (lastDigits + 1);
            }

            var firstInstance = new DocumentInstance
            {
                DocumentId = document.Id,
                InventoryNumber = nextNumber,
                State = "Новий",
                Available = true
            };

            _context.DocumentInstances.Add(firstInstance);
            await _context.SaveChangesAsync();

            // Оновлюємо кількість копій
            await UpdateDocumentQuantityAsync(document.Id);

            return RedirectToAction(nameof(Index));
        }

        ViewBag.TypeList = new SelectList(_context.DocumentTypes, "Id", "Name", document.TypeId);
        ViewBag.AuthorList = new MultiSelectList(_context.Authors, "Id", "Name", authorIds);
        ViewBag.CategoryList = new MultiSelectList(_context.Categories, "Id", "Name", categoryIds);

        return View(document);
    }

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

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var document = await _context.Documents
            .Include(d => d.DocumentInstances)
                .ThenInclude(di => di.ReservationDocuments)
            .Include(d => d.AuthorDocuments)
            .Include(d => d.CategoryDocuments)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null) return NotFound();

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

            _context.ReservationDocuments.RemoveRange(reservationsToRemove.SelectMany(r => r.ReservationDocuments));
            _context.Reservations.RemoveRange(reservationsToRemove);
        }

        if (document.DocumentInstances.Any())
        {
            _context.DocumentInstances.RemoveRange(document.DocumentInstances);
        }

        _context.AuthorDocuments.RemoveRange(document.AuthorDocuments);
        _context.CategoryDocuments.RemoveRange(document.CategoryDocuments);
        _context.Documents.Remove(document);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Import() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile fileExcel, CancellationToken cancellationToken = default)
    {
        if (fileExcel == null || fileExcel.Length == 0)
        {
            ModelState.AddModelError("", "Будь ласка, оберіть файл для імпорту.");
            return View();
        }

        var importService = _documentDataPortServiceFactory.GetImportService(fileExcel.ContentType);

        try
        {
            using var stream = fileExcel.OpenReadStream();
            await importService.ImportFromStreamAsync(stream, cancellationToken);
            TempData["SuccessMessage"] = "Імпорт виконано успішно.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Import));
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Export(
        [FromQuery] string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        CancellationToken cancellationToken = default)
    {
        var exportService = _documentDataPortServiceFactory.GetExportService(contentType);

        var memoryStream = new MemoryStream();
        await exportService.WriteToAsync(memoryStream, cancellationToken);
        await memoryStream.FlushAsync(cancellationToken);
        memoryStream.Position = 0;

        return new FileStreamResult(memoryStream, contentType)
        {
            FileDownloadName = $"export_{DateTime.UtcNow:yyyy-MM-dd}.xlsx"
        };
    }

    // Допоміжний метод для оновлення кількості копій
    private async Task UpdateDocumentQuantityAsync(int documentId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document != null)
        {
            int count = await _context.DocumentInstances.CountAsync(di => di.DocumentId == documentId);
            document.Quantity = count;
            await _context.SaveChangesAsync();
        }
    }
}
