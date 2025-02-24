using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ArchiveDomain.Model;
using ArchiveInfrastructure;

namespace ArchiveInfrastructure.Controllers;

public class CategoriesController : Controller
{
    private readonly DbarchiveContext _context;

    public CategoriesController(DbarchiveContext context)
    {
        _context = context;
    }

    // GET: Categories
    public async Task<IActionResult> Index()
    {
        return View(await _context.Categories.AsNoTracking().ToListAsync());
    }

    // GET: Categories/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var category = await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (category == null) return NotFound();

        return RedirectToAction("Index", "Documents", new { categoryId = category.Id });
    }

    // GET: Categories/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Categories/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Id")] Category category)
    {
        if (ModelState.IsValid)
        {
            // Так само перевірка на співпадіння назв
            bool exists = await _context.Categories.AnyAsync(c => c.Name == category.Name);
            if (exists)
            {
                ModelState.AddModelError("", "Категорія з такою назвою вже існує.");
                return View(category);
            }

            _context.Add(category);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }

    // GET: Categories/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();

        return View(category);
    }

    // POST: Categories/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Name,Id")] Category category)
    {
        if (id != category.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(category);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Categories.Any(e => e.Id == category.Id))
                    return NotFound();
                else
                    throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }

    // GET: Categories/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var category = await _context.Categories
            .Include(c => c.CategoryDocuments)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (category == null) return NotFound();

        return View(category);
    }

    // POST: Categories/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var category = await _context.Categories
            .Include(c => c.CategoryDocuments)
            .ThenInclude(cd => cd.Document) 
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null) return NotFound();

        _context.CategoryDocuments.RemoveRange(category.CategoryDocuments);

        // Видаляються документи, які більше не мають ЖОДНОЇ категорій та авторів
        var orphanDocuments = category.CategoryDocuments
            .Select(cd => cd.Document)
            .Where(d => !_context.CategoryDocuments.Any(cd => cd.DocumentId == d.Id) &&
                        !_context.AuthorDocuments.Any(ad => ad.DocumentId == d.Id))
            .ToList();

        _context.Documents.RemoveRange(orphanDocuments);

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
