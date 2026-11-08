
using Microsoft.AspNetCore.Mvc;
using SplitMate.Data;
using SplitMate.Models;

namespace SplitMate.Controllers
{
    public class ExpensesController : Controller
    {
        private readonly IDatabase _database;

        public ExpensesController(IDatabase database)
        {
            _database = database;
        }

        // Показване и филтриране на разходите
        public IActionResult Index(
            string? searchTerm,
            string? category,
            string? groupName)
        {
            var expenses = _database.GetAll(
                searchTerm,
                category,
                groupName);

            return View(expenses);
        }

        // Отваряне на формата за нов разход
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Expense());
        }

        // Записване на нов разход
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Expense expense)
        {
            if (!ModelState.IsValid)
            {
                return View(expense);
            }

            _database.Add(expense);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var expense = _database.GetById(id);

            if (expense == null)
            {
                return NotFound();
            }

            return View(expense);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Expense expense)
        {
            if (_database.GetById(id) == null)
            {
                return NotFound();
            }

            expense.Id = id;

            if (!ModelState.IsValid)
            {
                return View(expense);
            }

            if (!_database.Update(expense))
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var expense = _database.GetById(id);

            if (expense == null)
            {
                return NotFound();
            }

            return View(expense);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            if (!_database.Delete(id))
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
