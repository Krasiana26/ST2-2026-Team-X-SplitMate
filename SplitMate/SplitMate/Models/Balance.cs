
namespace SplitMate.Models;

public class Balance
{
    public string PersonName { get; set; } = string.Empty;

    public decimal TotalPaid { get; set; }

    public decimal TotalOwed { get; set; }

    public decimal NetBalance => TotalPaid - TotalOwed;
}
