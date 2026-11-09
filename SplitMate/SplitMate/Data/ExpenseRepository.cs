
using SplitMate.Models;

namespace SplitMate.Data;

// Repository Pattern: handles expense data access
// and delegates database operations to IDatabase.
public class ExpenseRepository : IExpenseRepository
{
    private readonly IDatabase _database;

    public ExpenseRepository(IDatabase database)
    {
        _database = database;
    }

    public IReadOnlyList<Expense> GetAll(
        string? searchTerm = null,
        string? category = null,
        string? groupName = null)
    {
        return _database.GetAll(searchTerm, category, groupName);
    }

    public Expense? GetById(int id)
    {
        return _database.GetById(id);
    }

    public int Add(Expense expense)
    {
        return _database.Add(expense);
    }

    public bool Update(Expense expense)
    {
        return _database.Update(expense);
    }

    public bool Delete(int id)
    {
        return _database.Delete(id);
    }
}
