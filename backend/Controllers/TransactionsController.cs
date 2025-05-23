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
                var group = _context.Groups
                    .Include(g => g.Members)
                    .FirstOrDefault(g => g.Id == dto.GroupId);
                
                if (group == null) 
                    return NotFound("Group not found");

                var payer = group.Members.FirstOrDefault(m => m.Id == dto.PayerId);
                if (payer == null) 
                    return BadRequest("Invalid payer");

                var transaction = new Transaction
                {
                    Description = dto.Description,
                    TotalAmount = dto.TotalAmount,
                    PayerId = dto.PayerId
                };

                ProcessSplits(transaction, dto, group);
                
                _context.Transactions.Add(transaction);
                _context.Entry(transaction).Property("GroupId").CurrentValue = dto.GroupId;

                UpdateMemberBalances(transaction, group, payer);
                
                _context.SaveChanges();
                
                return Ok(new
                {
                    Id = transaction.Id,
                    Description = transaction.Description,
                    TotalAmount = transaction.TotalAmount,
                    PayerId = transaction.PayerId,
                    PayerName = payer.Name,
                    Date = transaction.Date,
                    SplitsCount = transaction.Splits.Count
                });
            }
            catch (Exception)
            {
                return BadRequest("Transaction creation failed");
            }
        }

        private void ProcessSplits(Transaction transaction, CreateTransactionDto dto, Group group)
        {
            switch (dto.SplitType)
            {
                case SplitType.Equal:
                    var equalAmount = dto.TotalAmount / group.Members.Count;
                    foreach (var member in group.Members)
                    {
                        transaction.Splits.Add(new Split 
                        { 
                            MemberId = member.Id, 
                            Amount = equalAmount 
                        });
                    }
                    break;

                case SplitType.Percentage:
                    if (dto.Splits?.Any() != true)
                        throw new ArgumentException("Splits required for percentage split");
                    
                    foreach (var splitItem in dto.Splits)
                    {
                        var amount = dto.TotalAmount * (splitItem.Value / 100m);
                        transaction.Splits.Add(new Split 
                        { 
                            MemberId = splitItem.MemberId, 
                            Amount = amount 
                        });
                    }
                    break;

                case SplitType.Custom:
                    if (dto.Splits?.Any() != true)
                        throw new ArgumentException("Splits required for custom split");
                    
                    foreach (var splitItem in dto.Splits)
                    {
                        transaction.Splits.Add(new Split 
                        { 
                            MemberId = splitItem.MemberId, 
                            Amount = splitItem.Value 
                        });
                    }
                    break;
            }
        }

        private void UpdateMemberBalances(Transaction transaction, Group group, Member payer)
        {
            foreach (var splitItem in transaction.Splits)
            {
                if (splitItem.MemberId == payer.Id) continue;
                
                var borrower = group.Members.First(m => m.Id == splitItem.MemberId);

                if (!borrower.Balances.ContainsKey(payer.Id))
                    borrower.Balances[payer.Id] = 0m;
                if (!payer.Balances.ContainsKey(borrower.Id))
                    payer.Balances[borrower.Id] = 0m;

                borrower.Balances[payer.Id] += splitItem.Amount;
                payer.Balances[borrower.Id] -= splitItem.Amount;
            }
        }
    }
}