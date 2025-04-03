using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ArchiveDomain.Model;
using ArchiveInfrastructure;
using Microsoft.AspNetCore.Authorization;

namespace ArchiveInfrastructure.Controllers;

[Authorize(Roles = "admin")]
public class DocumentTypesController : Controller
{
    private readonly DbarchiveContext _context;

    public DocumentTypesController(DbarchiveContext context)
    {
        _context = context;
    }

    // GET: DocumentTypes
    public async Task<IActionResult> Index()
    {
        return View(await _context.DocumentTypes.AsNoTracking().ToListAsync());
    }

    // GET: DocumentTypes/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var documentType = await _context.DocumentTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (documentType == null) return NotFound();

        return RedirectToAction("Index", "Documents", new { typeId = documentType.Id });
    }

    // GET: DocumentTypes/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: DocumentTypes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Id")] DocumentType documentType)
    {
        if (ModelState.IsValid)
        {
            // Теж перевірка на співпадіння назв
            var existingType = await _context.DocumentTypes
                .FirstOrDefaultAsync(dt => dt.Name == documentType.Name);

            if (existingType != null)
            {
                ModelState.AddModelError("", "Такий тип документа вже існує.");
                return View(documentType);
            }

            _context.Add(documentType);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(documentType);
    }

    // GET: DocumentTypes/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var documentType = await _context.DocumentTypes.FindAsync(id);
        if (documentType == null) return NotFound();

        return View(documentType);
    }

    // POST: DocumentTypes/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Name,Id")] DocumentType documentType)
    {
        if (id != documentType.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(documentType);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.DocumentTypes.Any(e => e.Id == documentType.Id))
                    return NotFound();
                else
                    throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(documentType);
    }

    // GET: DocumentTypes/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var documentType = await _context.DocumentTypes
            .Include(dt => dt.Documents) 
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (documentType == null) return NotFound();

        return View(documentType);
    }

    // POST: DocumentTypes/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var documentType = await _context.DocumentTypes
            .Include(dt => dt.Documents)
            .ThenInclude(d => d.AuthorDocuments)
            .Include(dt => dt.Documents)
             .ThenInclude(d => d.CategoryDocuments) 
            .FirstOrDefaultAsync(dt => dt.Id == id);

        if (documentType == null) return NotFound();

        foreach (var document in documentType.Documents.ToList())
        {
            _context.AuthorDocuments.RemoveRange(document.AuthorDocuments);
            _context.CategoryDocuments.RemoveRange(document.CategoryDocuments);
            _context.Documents.Remove(document);
        }

        _context.DocumentTypes.Remove(documentType);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}

