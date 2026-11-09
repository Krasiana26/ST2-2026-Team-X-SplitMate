
using Microsoft.AspNetCore.Mvc;
using SplitMate.Data;
using SplitMate.Models;
using System.Diagnostics;

namespace SplitMate.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDatabase _database;

        public HomeController(IDatabase database)
        {
            _database = database;
        }

        public IActionResult Index()
        {
            var expenses = _database.GetAll();

            var model = new DashboardViewModel
            {
                TotalExpenses = expenses.Sum(e => e.Amount),
                ExpenseCount = expenses.Count,
                RecentExpenses = expenses
                    .OrderByDescending(e => e.ExpenseDate)
                    .Take(5)
                    .ToList()
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
