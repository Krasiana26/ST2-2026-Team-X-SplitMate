
using SplitMate.Models;

namespace SplitMate.Services;

// Factory Pattern: centralizes the creation of Expense objects.
public static class ExpenseFactory
{
    public static Expense Create(
        string title,
        decimal amount,
        string category,
        string paidBy,
        string groupName,
        DateTime expenseDate)
    {
        return new Expense
        {
            Title = title,
            Amount = amount,
            Category = category,
            PaidBy = paidBy,
            GroupName = groupName,
            ExpenseDate = expenseDate
        };
    }
}
