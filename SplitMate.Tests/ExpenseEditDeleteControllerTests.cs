using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using SplitMate.Controllers;
using SplitMate.Data;
using SplitMate.Models;
using SplitMate.Services;

namespace SplitMate.Tests;

public class ExpenseEditDeleteControllerTests
{
    [Fact]
    public void EditGet_ReturnsExistingExpense()
    {
        var expense = CreateExpense(1);
        var database = new TestDatabase(expense);
        var controller = new ExpensesController(database);

        var result = controller.Edit(expense.Id);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(expense, viewResult.Model);
    }

    [Fact]
    public void EditGet_ReturnsNotFoundForMissingExpense()
    {
        var controller = new ExpensesController(new TestDatabase());

        Assert.IsType<NotFoundResult>(controller.Edit(1));
    }

    [Fact]
    public void EditPost_UpdatesExistingExpenseAndUsesRouteId()
    {
        var original = CreateExpense(1);
        var database = new TestDatabase(original);
        var controller = new ExpensesController(database);
        var updated = CreateExpense(99);
        updated.Title = "Updated dinner";
        updated.Amount = 120m;
        updated.PaidBy = "Maria";

        var result = controller.Edit(original.Id, updated);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(original.Id, database.GetById(original.Id)!.Id);
        Assert.Equal("Updated dinner", database.GetById(original.Id)!.Title);
        Assert.Equal(120m, database.GetById(original.Id)!.Amount);
        Assert.Equal("Maria", database.GetById(original.Id)!.PaidBy);
    }

    [Fact]
    public void EditPost_DoesNotUpdateWhenModelIsInvalid()
    {
        var original = CreateExpense(1);
        var database = new TestDatabase(original);
        var controller = new ExpensesController(database);
        controller.ModelState.AddModelError("Amount", "Amount is invalid.");
        var submitted = CreateExpense(1);
        submitted.Amount = 0m;

        var result = controller.Edit(original.Id, submitted);

        Assert.IsType<ViewResult>(result);
        Assert.Equal(90m, database.GetById(original.Id)!.Amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000001)]
    public void ExpenseValidation_RejectsInvalidAmounts(decimal amount)
    {
        var expense = CreateExpense(1);
        expense.Amount = amount;
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            expense,
            new ValidationContext(expense),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, result =>
            result.MemberNames.Contains(nameof(Expense.Amount)));
    }

    [Fact]
    public void EditPost_ReturnsNotFoundForMissingExpense()
    {
        var controller = new ExpensesController(new TestDatabase());

        Assert.IsType<NotFoundResult>(
            controller.Edit(1, CreateExpense(1)));
    }

    [Fact]
    public void DeleteGet_ReturnsConfirmationForExistingExpense()
    {
        var expense = CreateExpense(1);
        var controller = new ExpensesController(new TestDatabase(expense));

        var result = controller.Delete(expense.Id);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(expense, viewResult.Model);
    }

    [Fact]
    public void DeleteConfirmed_RemovesExpense()
    {
        var expense = CreateExpense(1);
        var database = new TestDatabase(expense);
        var controller = new ExpensesController(database);

        var result = controller.DeleteConfirmed(expense.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Null(database.GetById(expense.Id));
    }

    [Fact]
    public void DeleteConfirmed_ReturnsNotFoundForMissingExpense()
    {
        var controller = new ExpensesController(new TestDatabase());

        Assert.IsType<NotFoundResult>(controller.DeleteConfirmed(1));
    }

    [Fact]
    public void EditAndDeletePostActions_RequireAntiforgeryValidation()
    {
        var editMethod = typeof(ExpensesController).GetMethod(
            nameof(ExpensesController.Edit),
            new[] { typeof(int), typeof(Expense) });
        var deleteMethod = typeof(ExpensesController).GetMethod(
            nameof(ExpensesController.DeleteConfirmed),
            new[] { typeof(int) });

        Assert.NotNull(editMethod);
        Assert.NotNull(deleteMethod);
        Assert.NotNull(editMethod.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.NotNull(deleteMethod.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }

    [Fact]
    public void EditingAndDeletingExpense_ChangesReloadedGroupBalances()
    {
        var firstExpense = CreateExpense(1);
        var secondExpense = CreateExpense(2);
        secondExpense.Amount = 30m;
        secondExpense.PaidBy = "Maria";
        var database = new TestDatabase(firstExpense, secondExpense);
        var controller = new ExpensesController(database);
        var editedExpense = CreateExpense(firstExpense.Id);
        editedExpense.Amount = 120m;
        editedExpense.PaidBy = "Maria";

        controller.Edit(firstExpense.Id, editedExpense);

        var service = new ExpenseSplittingService();
        var balancesAfterEdit = service.CalculateBalances(
            database.GetAll(groupName: "Friends"),
            new[] { "Ivan", "Maria" });
        Assert.Equal(-75m, balancesAfterEdit.Single(b => b.PersonName == "Ivan").NetBalance);
        Assert.Equal(75m, balancesAfterEdit.Single(b => b.PersonName == "Maria").NetBalance);

        controller.DeleteConfirmed(secondExpense.Id);

        var balancesAfterDelete = service.CalculateBalances(
            database.GetAll(groupName: "Friends"),
            new[] { "Ivan", "Maria" });
        Assert.Equal(-60m, balancesAfterDelete.Single(b => b.PersonName == "Ivan").NetBalance);
        Assert.Equal(60m, balancesAfterDelete.Single(b => b.PersonName == "Maria").NetBalance);
    }

    private static Expense CreateExpense(int id) => new()
    {
        Id = id,
        Title = "Dinner",
        Amount = 90m,
        Category = "Food",
        PaidBy = "Ivan",
        GroupName = "Friends",
        ExpenseDate = new DateTime(2026, 11, 8)
    };

    private sealed class TestDatabase : IExpenseRepository
    {
        private readonly Dictionary<int, Expense> _expenses;

        public TestDatabase(params Expense[] expenses)
        {
            _expenses = expenses.ToDictionary(expense => expense.Id);
        }

        public IReadOnlyList<Expense> GetAll(
            string? searchTerm = null,
            string? category = null,
            string? groupName = null)
        {
            IEnumerable<Expense> expenses = _expenses.Values;

            if (!string.IsNullOrWhiteSpace(groupName))
            {
                expenses = expenses.Where(expense =>
                    expense.GroupName == groupName);
            }

            return expenses.ToList();
        }

        public Expense? GetById(int id) =>
            _expenses.TryGetValue(id, out var expense) ? expense : null;

        public int Add(Expense expense)
        {
            var id = _expenses.Count == 0 ? 1 : _expenses.Keys.Max() + 1;
            expense.Id = id;
            _expenses.Add(id, expense);
            return id;
        }

        public bool Update(Expense expense)
        {
            if (!_expenses.ContainsKey(expense.Id))
            {
                return false;
            }

            _expenses[expense.Id] = expense;
            return true;
        }

        public bool Delete(int id) => _expenses.Remove(id);
    }
}
