using Microsoft.AspNetCore.Mvc;
using GroupExpenseApp.Data;
using GroupExpenseApp.DTOs;
using GroupExpenseApp.Models;
using Microsoft.EntityFrameworkCore;

namespace GroupExpenseApp.Controllers
{
    [ApiController]
    [Route("api/transaction")]
    public class TransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TransactionsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult CreateTransaction([FromBody] CreateTransactionDto dto)
        {
            try
            {
                Console.WriteLine("=== CreateTransaction Debug ===");
                Console.WriteLine($"DTO received: {System.Text.Json.JsonSerializer.Serialize(dto)}");
                Console.WriteLine($"GroupId: {dto.GroupId}");
                Console.WriteLine($"Description: {dto.Description}");
                Console.WriteLine($"TotalAmount: {dto.TotalAmount}");
                Console.WriteLine($"PayerId: {dto.PayerId}");
                Console.WriteLine($"SplitType: {dto.SplitType}");
                Console.WriteLine($"Splits count: {dto.Splits?.Count ?? 0}");

                if (dto.Splits != null)
                {
                    foreach (var split in dto.Splits)
                    {
                        Console.WriteLine($"Split - MemberId: {split.MemberId}, Value: {split.Value}");
                    }
                }

                var group = _context.Groups
                    .Include(g => g.Members)
                    .FirstOrDefault(g => g.Id == dto.GroupId);
                
                if (group == null) 
                {
                    Console.WriteLine($"ERROR: Group {dto.GroupId} not found");
                    return NotFound($"Group {dto.GroupId} not found");
                }

                Console.WriteLine($"Group found: {group.Title} with {group.Members.Count} members");

                var payer = group.Members.FirstOrDefault(m => m.Id == dto.PayerId);
                if (payer == null) 
                {
                    Console.WriteLine($"ERROR: Payer {dto.PayerId} not found in group");
                    return BadRequest($"Payer with ID {dto.PayerId} not found in group");
                }

                Console.WriteLine($"Payer found: {payer.Name}");

                var transaction = new Transaction
                {
                    Description = dto.Description,
                    TotalAmount = dto.TotalAmount,
                    PayerId = dto.PayerId
                };

                Console.WriteLine("Processing splits...");

                switch (dto.SplitType)
                {
                    case SplitType.Equal:
                        Console.WriteLine("Processing Equal split");
                        var equalAmount = dto.TotalAmount / group.Members.Count;
                        foreach (var member in group.Members)
                        {
                            transaction.Splits.Add(new Split { MemberId = member.Id, Amount = equalAmount });
                            Console.WriteLine($"Equal split: {member.Name} = {equalAmount}");
                        }
                        break;

                    case SplitType.Percentage:
                        Console.WriteLine("Processing Percentage split");
                        if (dto.Splits == null || !dto.Splits.Any())
                        {
                            return BadRequest("Splits are required for percentage split");
                        }
                        foreach (var split in dto.Splits)
                        {
                            var amount = dto.TotalAmount * (split.Value / 100);
                            transaction.Splits.Add(new Split { MemberId = split.MemberId, Amount = amount });
                            Console.WriteLine($"Percentage split: MemberId {split.MemberId} = {amount}");
                        }
                        break;

                    case SplitType.Custom:
                        Console.WriteLine("Processing Custom split");
                        if (dto.Splits == null || !dto.Splits.Any())
                        {
                            return BadRequest("Splits are required for custom split");
                        }
                        foreach (var split in dto.Splits)
                        {
                            transaction.Splits.Add(new Split { MemberId = split.MemberId, Amount = split.Value });
                            Console.WriteLine($"Custom split: MemberId {split.MemberId} = {split.Value}");
                        }
                        break;
                }

                Console.WriteLine("Adding transaction to context...");
                _context.Transactions.Add(transaction);

                _context.Entry(transaction).Property("GroupId").CurrentValue = dto.GroupId;

                Console.WriteLine("Updating member balances...");
                foreach (var split in transaction.Splits)
                {
                    if (split.MemberId == dto.PayerId) continue; 
                    
                    var borrower = group.Members.First(m => m.Id == split.MemberId);

                    if (!borrower.Balances.ContainsKey(payer.Id))
                        borrower.Balances[payer.Id] = 0;
                    if (!payer.Balances.ContainsKey(borrower.Id))
                        payer.Balances[borrower.Id] = 0;

                    borrower.Balances[payer.Id] += split.Amount;
                    payer.Balances[borrower.Id] -= split.Amount;
                    
                    Console.WriteLine($"Balance update: {borrower.Name} owes {payer.Name} an additional {split.Amount}");
                }

                Console.WriteLine("Saving changes...");
                _context.SaveChanges();
                
                Console.WriteLine($"Transaction created successfully with ID: {transaction.Id}");
                
                var response = new
                {
                    Id = transaction.Id,
                    Description = transaction.Description,
                    TotalAmount = transaction.TotalAmount,
                    PayerId = transaction.PayerId,
                    PayerName = payer.Name,
                    Date = transaction.Date,
                    SplitsCount = transaction.Splits.Count
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR in CreateTransaction ===");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                
                return BadRequest($"Server error: {ex.Message}");
            }
        }
    }
}