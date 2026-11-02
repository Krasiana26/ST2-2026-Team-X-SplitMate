
using Microsoft.Data.Sqlite;
using SplitMate.Models;

namespace SplitMate.Data;

public sealed class Database : IDatabase
{
    private static Database? _instance;
    private static readonly object _lock = new();

    private readonly string _connectionString;

    private Database(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static Database Initialize(string connectionString)
    {
        if (_instance == null)
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new Database(connectionString);
                    _instance.CreateDatabase();
                }
            }
        }

        return _instance;
    }

    private void CreateDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS Expenses (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Amount REAL NOT NULL,
                Category TEXT NOT NULL,
                PaidBy TEXT NOT NULL,
                GroupName TEXT NOT NULL,
                ExpenseDate TEXT NOT NULL
            );
            """;

        command.ExecuteNonQuery();
    }

    public IReadOnlyList<Expense> GetAll(
        string? searchTerm = null,
        string? category = null,
        string? groupName = null)
    {
        var expenses = new List<Expense>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();

        var sql = "SELECT * FROM Expenses WHERE 1 = 1";

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            sql += " AND (Title LIKE @search OR PaidBy LIKE @search)";
            command.Parameters.AddWithValue("@search", $"%{searchTerm}%");
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            sql += " AND Category = @category";
            command.Parameters.AddWithValue("@category", category);
        }

        if (!string.IsNullOrWhiteSpace(groupName))
        {
            sql += " AND GroupName = @groupName";
            command.Parameters.AddWithValue("@groupName", groupName);
        }

        sql += " ORDER BY ExpenseDate DESC";

        command.CommandText = sql;

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var expense = new Expense
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Amount = reader.GetDecimal(2),
                Category = reader.GetString(3),
                PaidBy = reader.GetString(4),
                GroupName = reader.GetString(5),
                ExpenseDate = DateTime.Parse(reader.GetString(6))
            };

            expenses.Add(expense);
        }

        return expenses;
    }

    public Expense? GetById(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT Id, Title, Amount, Category, PaidBy, GroupName, ExpenseDate
            FROM Expenses
            WHERE Id = @id;
            """;

        command.Parameters.AddWithValue("@id", id);

        using var reader = command.ExecuteReader();

        if (reader.Read())
        {
            return new Expense
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Amount = reader.GetDecimal(2),
                Category = reader.GetString(3),
                PaidBy = reader.GetString(4),
                GroupName = reader.GetString(5),
                ExpenseDate = DateTime.Parse(reader.GetString(6))
            };
        }

        return null;
    }

    public int Add(Expense expense)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();

        command.CommandText =
            """
            INSERT INTO Expenses
            (Title, Amount, Category, PaidBy, GroupName, ExpenseDate)
            VALUES
            (@title, @amount, @category, @paidBy, @groupName, @expenseDate);

            SELECT last_insert_rowid();
            """;

        command.Parameters.AddWithValue("@title", expense.Title);
        command.Parameters.AddWithValue("@amount", expense.Amount);
        command.Parameters.AddWithValue("@category", expense.Category);
        command.Parameters.AddWithValue("@paidBy", expense.PaidBy);
        command.Parameters.AddWithValue("@groupName", expense.GroupName);
        command.Parameters.AddWithValue(
            "@expenseDate",
            expense.ExpenseDate.ToString("yyyy-MM-dd"));

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public bool Update(Expense expense)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();

        command.CommandText =
            """
            UPDATE Expenses
            SET Title = @title,
                Amount = @amount,
                Category = @category,
                PaidBy = @paidBy,
                GroupName = @groupName,
                ExpenseDate = @expenseDate
            WHERE Id = @id;
            """;

        command.Parameters.AddWithValue("@id", expense.Id);
        command.Parameters.AddWithValue("@title", expense.Title);
        command.Parameters.AddWithValue("@amount", expense.Amount);
        command.Parameters.AddWithValue("@category", expense.Category);
        command.Parameters.AddWithValue("@paidBy", expense.PaidBy);
        command.Parameters.AddWithValue("@groupName", expense.GroupName);
        command.Parameters.AddWithValue(
            "@expenseDate",
            expense.ExpenseDate.ToString("yyyy-MM-dd"));

        return command.ExecuteNonQuery() > 0;
    }

    public bool Delete(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();

        command.CommandText = "DELETE FROM Expenses WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", id);

        return command.ExecuteNonQuery() > 0;
    }
}
