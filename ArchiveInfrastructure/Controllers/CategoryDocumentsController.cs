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
    public class CategoryDocumentsController : Controller
    {
        private readonly DbarchiveContext _context;

        public CategoryDocumentsController(DbarchiveContext context)
        {
            _context = context;
        }

        // GET: CategoryDocuments
        public async Task<IActionResult> Index()
        {
            var dbarchiveContext = _context.CategoryDocuments
                .Include(c => c.Category)
                .Include(c => c.Document)
                .AsNoTracking();

            return View(await dbarchiveContext.ToListAsync());
        }

        // GET: CategoryDocuments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var categoryDocument = await _context.CategoryDocuments
                .Include(c => c.Category)
                .Include(c => c.Document)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (categoryDocument == null) return NotFound();

            return View(categoryDocument);
        }

        // GET: CategoryDocuments/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            ViewData["DocumentId"] = new SelectList(_context.Documents, "Id", "Title");
            return View();
        }

        // POST: CategoryDocuments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DocumentId,CategoryId,Id")] CategoryDocument categoryDocument)
        {
            if (ModelState.IsValid)
            {
                // Перевірка на дублювання
                var existingRelation = await _context.CategoryDocuments
                    .FirstOrDefaultAsync(cd => cd.CategoryId == categoryDocument.CategoryId && cd.DocumentId == categoryDocument.DocumentId);

                if (existingRelation != null)
                {
                    ModelState.AddModelError("", "Ця категорія вже пов’язана з цим документом.");
                    return View(categoryDocument);
                }

                _context.Add(categoryDocument);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", categoryDocument.CategoryId);
            ViewData["DocumentId"] = new SelectList(_context.Documents, "Id", "Title", categoryDocument.DocumentId);
            return View(categoryDocument);
        }

        // GET: CategoryDocuments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var categoryDocument = await _context.CategoryDocuments.FindAsync(id);
            if (categoryDocument == null) return NotFound();

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", categoryDocument.CategoryId);
            ViewData["DocumentId"] = new SelectList(_context.Documents, "Id", "Title", categoryDocument.DocumentId);
            return View(categoryDocument);
        }

        // POST: CategoryDocuments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DocumentId,CategoryId,Id")] CategoryDocument categoryDocument)
        {
            if (id != categoryDocument.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(categoryDocument);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.CategoryDocuments.Any(e => e.Id == categoryDocument.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", categoryDocument.CategoryId);
            ViewData["DocumentId"] = new SelectList(_context.Documents, "Id", "Title", categoryDocument.DocumentId);
            return View(categoryDocument);
        }

        // GET: CategoryDocuments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var categoryDocument = await _context.CategoryDocuments
                .Include(c => c.Category)
                .Include(c => c.Document)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (categoryDocument == null) return NotFound();

            return View(categoryDocument);
        }

        // POST: CategoryDocuments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var categoryDocument = await _context.CategoryDocuments.FindAsync(id);
            if (categoryDocument != null)
            {
                _context.CategoryDocuments.Remove(categoryDocument);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
