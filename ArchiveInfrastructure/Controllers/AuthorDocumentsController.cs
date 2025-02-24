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
    public class AuthorDocumentsController : Controller
    {
        private readonly DbarchiveContext _context;

        public AuthorDocumentsController(DbarchiveContext context)
        {
            _context = context;
        }

        // GET: AuthorDocuments
        public async Task<IActionResult> Index()
        {
            var dbarchiveContext = _context.AuthorDocuments
                .Include(a => a.Author)
                .Include(a => a.Document)
                .AsNoTracking();

            return View(await dbarchiveContext.ToListAsync());
        }

        // GET: AuthorDocuments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var authorDocument = await _context.AuthorDocuments
                .Include(a => a.Author)
                .Include(a => a.Document)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (authorDocument == null) return NotFound();

            return View(authorDocument);
        }

        // GET: AuthorDocuments/Create
        public IActionResult Create()
        {
            ViewData["AuthorId"] = new SelectList(_context.Authors, "Id", "Name");
            ViewData["DocumentId"] = new SelectList(_context.Documents, "Id", "Title");
            return View();
        }

        // POST: AuthorDocuments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DocumentId,AuthorId,Id")] AuthorDocument authorDocument)
        {
            if (ModelState.IsValid)
            {
                // Перевірка на дублювання
                var existingRelation = await _context.AuthorDocuments
                    .FirstOrDefaultAsync(ad => ad.AuthorId == authorDocument.AuthorId && ad.DocumentId == authorDocument.DocumentId);

                if (existingRelation != null)
                {
                    ModelState.AddModelError("", "Цей автор вже пов’язаний із цим документом.");
                    return View(authorDocument);
                }

                _context.Add(authorDocument);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["AuthorId"] = new SelectList(_context.Authors, "Id", "Name", authorDocument.AuthorId);
            ViewData["DocumentId"] = new SelectList(_context.Documents, "Id", "Title", authorDocument.DocumentId);
            return View(authorDocument);
        }

        // GET: AuthorDocuments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var authorDocument = await _context.AuthorDocuments.FindAsync(id);
            if (authorDocument == null) return NotFound();

            ViewData["AuthorId"] = new SelectList(_context.Authors, "Id", "Name", authorDocument.AuthorId);
            ViewData["DocumentId"] = new SelectList(_context.Documents, "Id", "Title", authorDocument.DocumentId);
            return View(authorDocument);
        }

        // POST: AuthorDocuments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DocumentId,AuthorId,Id")] AuthorDocument authorDocument)
        {
            if (id != authorDocument.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(authorDocument);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.AuthorDocuments.Any(e => e.Id == authorDocument.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["AuthorId"] = new SelectList(_context.Authors, "Id", "Name", authorDocument.AuthorId);
            ViewData["DocumentId"] = new SelectList(_context.Documents, "Id", "Title", authorDocument.DocumentId);
            return View(authorDocument);
        }

        // GET: AuthorDocuments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var authorDocument = await _context.AuthorDocuments
                .Include(a => a.Author)
                .Include(a => a.Document)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (authorDocument == null) return NotFound();

            return View(authorDocument);
        }

        // POST: AuthorDocuments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var authorDocument = await _context.AuthorDocuments.FindAsync(id);
            if (authorDocument != null)
            {
                _context.AuthorDocuments.Remove(authorDocument);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
