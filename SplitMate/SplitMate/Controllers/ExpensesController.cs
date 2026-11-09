
using Microsoft.AspNetCore.Mvc;
using SplitMate.Data;
using SplitMate.Models;
using SplitMate.Services;

namespace SplitMate.Controllers
{
    public class ExpensesController : Controller
    {
        private readonly IExpenseRepository _repository;

        public ExpensesController(IExpenseRepository repository)
        {
            _repository = repository;
        }

        // Display and filter expenses
        public IActionResult Index(
            string? searchTerm,
            string? category,
            string? groupName)
        {
            var expenses = _repository.GetAll(
                searchTerm,
                category,
                groupName);

            return View(expenses);
        }

        // Open the form for a new expense
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Expense());
        }

        // Save a new expense using Factory Pattern
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Expense expense)
        {
            if (!ModelState.IsValid)
            {
                return View(expense);
            }

            var newExpense = ExpenseFactory.Create(
                expense.Title,
                expense.Amount,
                expense.Category,
                expense.PaidBy,
                expense.GroupName,
                expense.ExpenseDate);

            _repository.Add(newExpense);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var expense = _repository.GetById(id);

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
            if (_repository.GetById(id) == null)
            {
                return NotFound();
            }

            expense.Id = id;

            if (!ModelState.IsValid)
            {
                return View(expense);
            }

            if (!_repository.Update(expense))
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var expense = _repository.GetById(id);

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
            if (!_repository.Delete(id))
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
