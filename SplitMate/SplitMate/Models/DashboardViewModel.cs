
using System.Collections.Generic;

namespace SplitMate.Models
{
    public class DashboardViewModel
    {
        public decimal TotalExpenses { get; set; }

        public int ExpenseCount { get; set; }

        public IReadOnlyList<Expense> RecentExpenses { get; set; }
            = new List<Expense>();
    }
}
