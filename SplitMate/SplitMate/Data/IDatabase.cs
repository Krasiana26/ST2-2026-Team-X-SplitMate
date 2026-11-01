
using SplitMate.Models;

namespace SplitMate.Data;

public interface IDatabase
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
