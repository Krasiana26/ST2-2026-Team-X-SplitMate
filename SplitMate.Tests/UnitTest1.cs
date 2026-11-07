
using SplitMate.Models;
using SplitMate.Services;
using Xunit;

namespace SplitMate.Tests;

public class UnitTest1
{
    [Fact]
    public void CalculateBalances_ShouldSplitExpensesEqually()
    {
        // Подготвяме участниците
        var members = new List<string>
        {
            "Ivan",
            "Maria",
            "Georgi"
        };

        // Иван е платил 90 евро
        var expenses = new List<Expense>
        {
            new Expense
            {
                Title = "Dinner",
                Amount = 90m,
                Category = "Food",
                PaidBy = "Ivan",
                GroupName = "Friends",
                ExpenseDate = DateTime.Today
            }
        };

        // Изчисляваме балансите
        var service = new ExpenseSplittingService();
        var balances = service.CalculateBalances(expenses, members);

        // Проверяваме резултатите
        Assert.Equal(3, balances.Count);

        Assert.Equal(60m,
            balances.Single(b => b.PersonName == "Ivan").NetBalance);

        Assert.Equal(-30m,
            balances.Single(b => b.PersonName == "Maria").NetBalance);

        Assert.Equal(-30m,
            balances.Single(b => b.PersonName == "Georgi").NetBalance);
    }
    
[Fact]
public void CalculateBalances_MultiplePayers()
{
    var members = new List<string> { "Ivan", "Maria" };

    var expenses = new List<Expense>
    {
        new Expense { Amount = 80m, PaidBy = "Ivan" },
        new Expense { Amount = 20m, PaidBy = "Maria" }
    };

    var service = new ExpenseSplittingService();
    var balances = service.CalculateBalances(expenses, members);

    Assert.Equal(30m,
        balances.Single(b => b.PersonName == "Ivan").NetBalance);

    Assert.Equal(-30m,
        balances.Single(b => b.PersonName == "Maria").NetBalance);
}

[Fact]
public void CalculateBalances_NoExpenses()
{
    var members = new List<string> { "Ivan", "Maria" };
    var expenses = new List<Expense>();

    var service = new ExpenseSplittingService();
    var balances = service.CalculateBalances(expenses, members);

    Assert.Equal(2, balances.Count);
    Assert.All(balances, b => Assert.Equal(0m, b.NetBalance));
}

[Fact]
public void CalculateBalances_InvalidPayer()
{
    var members = new List<string> { "Ivan", "Maria" };

    var expenses = new List<Expense>
    {
        new Expense { Amount = 50m, PaidBy = "Georgi" }
    };

    var service = new ExpenseSplittingService();

    Assert.Throws<ArgumentException>(() =>
        service.CalculateBalances(expenses, members));
}

}
