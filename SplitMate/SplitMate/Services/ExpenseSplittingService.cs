
using SplitMate.Models;

namespace SplitMate.Services;

public class ExpenseSplittingService
{
    public List<Balance> CalculateBalances(
        IEnumerable<Expense> expenses,
        IEnumerable<string> members)
    {
        var participants = members
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Select(m => m.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var expenseList = expenses.ToList();

        if (participants.Count == 0)
            throw new ArgumentException(
                "At least one participant is required.");

        var balances = participants.Select(name => new Balance
        {
            PersonName = name
        }).ToList();

        decimal totalExpenses = expenseList.Sum(e => e.Amount);
        decimal share = totalExpenses / participants.Count;

        foreach (var balance in balances)
        {
            balance.TotalPaid = expenseList
                .Where(e => string.Equals(
                    e.PaidBy?.Trim(),
                    balance.PersonName,
                    StringComparison.OrdinalIgnoreCase))
                .Sum(e => e.Amount);

            balance.TotalOwed = share;
        }

        if (expenseList.Any(e =>
            !participants.Contains(
                e.PaidBy?.Trim() ?? "",
                StringComparer.OrdinalIgnoreCase)))
        {
            throw new ArgumentException(
                "Every payer must be a group member.");
        }

        return balances;
    }
}
