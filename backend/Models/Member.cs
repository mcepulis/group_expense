using System.ComponentModel.DataAnnotations.Schema;

namespace GroupExpenseApp.Models
{
    public class Member
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int GroupId { get; set; }
        public Group Group { get; set; } = null!;

        [NotMapped]
        public Dictionary<int, decimal> Balances { get; set; } = new();
    }
}
