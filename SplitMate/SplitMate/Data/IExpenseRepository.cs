
using SplitMate.Models;

namespace SplitMate.Data;

// Repository Pattern: defines the contract
// for expense data operations.
public interface IExpenseRepository
{
    IReadOnlyList<Expense> GetAll(
        string? searchTerm = null,
        string? category = null,
        string? groupName = null);

    Expense? GetById(int id);

    int Add(Expense expense);

    bool Update(Expense expense);

    bool Delete(int id);
}
