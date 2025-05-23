namespace GroupExpenseApp.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public decimal TotalAmount { get; set; }
        public int PayerId { get; set; } 
        public Member Payer { get; set; } = null!; 

        public DateTime Date { get; set; } = DateTime.UtcNow;
        public List<Split> Splits { get; set; } = new();
    }

    public class Split
    {
        public int Id { get; set; } 

        public int MemberId { get; set; }
        public Member Member { get; set; } = null!;

        public int TransactionId { get; set; }
        public Transaction Transaction { get; set; } = null!;

        public decimal Amount { get; set; }
    }
}
