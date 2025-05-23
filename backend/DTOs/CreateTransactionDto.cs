using GroupExpenseApp.Models;

namespace GroupExpenseApp.DTOs
{
    public class CreateTransactionDto
    {
        public int GroupId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int PayerId { get; set; }
        public SplitType SplitType { get; set; }
        public List<SplitDto> Splits { get; set; } = new();
    }

    public class SplitDto
    {
        public int MemberId { get; set; }
        public decimal Value { get; set; }
    }
}