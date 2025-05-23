using System.Transactions;
using System.ComponentModel.DataAnnotations;

namespace GroupExpenseApp.Models;

public class Group
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<Member> Members { get; set; } = [];
    public List<Transaction> Transactions { get; set; } = [];
}
