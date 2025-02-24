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
    public class UsersController : Controller
    {
        private readonly DbarchiveContext _context;

        public UsersController(DbarchiveContext context)
        {
            _context = context;
        }

        // GET: Users
        public async Task<IActionResult> Index(string position, string email)
        {
            IQueryable<User> users = _context.Users.AsNoTracking();

            if (!string.IsNullOrEmpty(position))
            {
                users = users.Where(u => u.Position.Contains(position));
            }

            if (!string.IsNullOrEmpty(email))
            {
                users = users.Where(u => u.Email.Contains(email));
            }

            return View(await users.ToListAsync());
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (user == null) return NotFound();

            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,PhoneNumber,Email,ReaderCardNumber,Position,PasswordAccount,Id")] User user)
        {
            if (ModelState.IsValid)
            {
                // Перевірка на дублювання Email
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("", "Користувач з такою електронною поштою вже існує.");
                    return View(user);
                }

                // Переконуємось, що пароль — позитивне число
                if (user.PasswordAccount < 0)
                {
                    ModelState.AddModelError("", "Пароль не може бути від’ємним.");
                    return View(user);
                }

                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("FirstName,LastName,PhoneNumber,Email,ReaderCardNumber,Position,PasswordAccount,Id")] User user)
        {
            if (id != user.Id) return NotFound();

            if (ModelState.IsValid)
            {
                // Перевірка на дублювання Email при редагуванні
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == user.Email && u.Id != user.Id);

                if (existingUser != null)
                {
                    ModelState.AddModelError("", "Ця електронна пошта вже використовується іншим користувачем.");
                    return View(user);
                }

                // Переконуємось, що пароль — позитивне число
                if (user.PasswordAccount < 0)
                {
                    ModelState.AddModelError("", "Пароль не може бути від’ємним.");
                    return View(user);
                }

                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Users.Any(e => e.Id == user.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .Include(u => u.Reservations)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users
                .Include(u => u.Reservations)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            // Перевірка, чи користувач має бронювання
            if (user.Reservations.Any())
            {
                ModelState.AddModelError("", "Неможливо видалити користувача, оскільки він має бронювання.");
                return View(user);
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
