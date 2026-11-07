
using Microsoft.AspNetCore.Mvc;
using SplitMate.Data;
using SplitMate.Models;
using SplitMate.Services;

namespace SplitMate.Controllers;

public class SplittingController : Controller
{
    private readonly IDatabase _database;
    private readonly ExpenseSplittingService _splittingService;

    public SplittingController(IDatabase database)
    {
        _database = database;
        _splittingService = new ExpenseSplittingService();
    }

    [HttpGet]
    public IActionResult Index(string? groupName, string? members)
    {
        ViewBag.GroupName = groupName;
        ViewBag.Members = members;

        if (string.IsNullOrWhiteSpace(groupName) ||
            string.IsNullOrWhiteSpace(members))
        {
            return View(new List<Balance>());
        }

        var participants = members
            .Split(',', StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries)
            .ToList();

        var expenses = _database.GetAll(groupName: groupName);

        try
        {
            var balances = _splittingService.CalculateBalances(
                expenses, participants);

            return View(balances);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(new List<Balance>());
        }
    }
}
