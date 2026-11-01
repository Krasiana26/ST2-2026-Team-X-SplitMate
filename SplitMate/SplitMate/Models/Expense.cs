
using System.ComponentModel.DataAnnotations;

namespace SplitMate.Models
{
    public class Expense
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Expense title is required.")]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 1000000)]
        public decimal Amount { get; set; }

        [Required]
        public string Category { get; set; } = string.Empty;

        [Required]
        public string PaidBy { get; set; } = string.Empty;

        [Required]
        public string GroupName { get; set; } = string.Empty;

        public DateTime ExpenseDate { get; set; } = DateTime.Today;
    }
}
