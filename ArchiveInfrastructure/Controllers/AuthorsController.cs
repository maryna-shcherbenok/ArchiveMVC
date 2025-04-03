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
public class AuthorsController : Controller
{
    private readonly DbarchiveContext _context;

    public AuthorsController(DbarchiveContext context)
    {
        _context = context;
    }

    // GET: Authors
    public async Task<IActionResult> Index()
    {
        return View(await _context.Authors.AsNoTracking().ToListAsync());
    }

    // GET: Authors/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var author = await _context.Authors
            .Include(a => a.AuthorDocuments)
            .ThenInclude(ad => ad.Document)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (author == null) return NotFound();

        return RedirectToAction("Index", "Documents", new { authorId = author.Id });
    }

    // GET: Authors/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Authors/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Id")] Author author)
    {
        if (ModelState.IsValid)
        {
            // Додала перевірку на дублювання імені, щоб не було декілька разів одного (розглядаю лише випадки, коли ім'я різне)
            bool exists = await _context.Authors.AnyAsync(a => a.Name == author.Name);
            if (exists)
            {
                ModelState.AddModelError("", "Автор документа з таким іменем (назвою) вже існує.");
                return View(author);
            }

            _context.Add(author);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(author);
    }

    // GET: Authors/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var author = await _context.Authors.FindAsync(id);
        if (author == null) return NotFound();

        return View(author);
    }

    // POST: Authors/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Name,Id")] Author author)
    {
        if (id != author.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(author);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Authors.Any(e => e.Id == author.Id))
                    return NotFound();
                else
                    throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(author);
    }

    // GET: Authors/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var author = await _context.Authors
            .Include(a => a.AuthorDocuments)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (author == null) return NotFound();

        return View(author);
    }

    // POST: Authors/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var author = await _context.Authors
            .Include(a => a.AuthorDocuments)
            .ThenInclude(ad => ad.Document)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (author == null) return NotFound();

        _context.AuthorDocuments.RemoveRange(author.AuthorDocuments);

        // Видаляються документи, які більше не мають ЖОДНОЇ категорій та авторів
        var orphanDocuments = author.AuthorDocuments
            .Select(ad => ad.Document)
            .Where(d => !_context.AuthorDocuments.Any(ad => ad.DocumentId == d.Id) &&
                        !_context.CategoryDocuments.Any(cd => cd.DocumentId == d.Id))
            .ToList();

        _context.Documents.RemoveRange(orphanDocuments);

        _context.Authors.Remove(author);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
