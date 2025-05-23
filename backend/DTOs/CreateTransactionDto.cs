using GroupExpenseApp.Models;

namespace GroupExpenseApp.DTOs
{
    public class CreateTransactionDto
    {
        public int GroupId { get; set; }
        public string Description { get; set; }
        public decimal TotalAmount { get; set; }
        public int PayerId { get; set; }
        public SplitType SplitType { get; set; }
        public List<SplitDto> Splits { get; set; } = new();
    }
}
