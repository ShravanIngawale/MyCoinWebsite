using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Expense_Tracker.Models;
using NuGet.Protocol.Core.Types;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Expense_Tracker.Controllers
{
    [Authorize]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Category
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return View(await _context.Categories.Where(c => c.UserId == userId).ToListAsync());
        }


        // GET: Category/AddOrEdit
        public IActionResult AddOrEdit(int id = 0)
        {
            if (id == 0)
                return View(new Category());
            else
                return View(_context.Categories.Find(id));
        }

        // POST: Category/AddOrEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit([Bind("CategoryId,Title,Icon,Type")] Category category)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (category.CategoryId == 0)
            {
                category.UserId = userId;
                _context.Add(category);
            }
            else
            {
                var existingCategory = await _context.Categories.FindAsync(category.CategoryId);
                if (existingCategory.UserId != userId)
                    return Unauthorized();
                existingCategory.Title = category.Title;
                existingCategory.Icon = category.Icon;
                existingCategory.Type = category.Type;
                _context.Update(existingCategory);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }



        // POST: Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Categories == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Categories'  is null.");
            }
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


    }
}