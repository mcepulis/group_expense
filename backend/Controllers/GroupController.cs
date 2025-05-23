using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GroupExpenseApp.Data;
using GroupExpenseApp.Models;
using GroupExpenseApp.DTOs;

namespace GroupExpenseApp.Controllers
{
    [ApiController]
    [Route("api/group")]
    public class GroupsController : ControllerBase
    {
        private readonly AppDbContext _context;
        
        public GroupsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetGroups()
        {
            var groups = _context.Groups
                .Include(g => g.Members)
                .Include(g => g.Transactions)
                    .ThenInclude(t => t.Splits)
                .ToList();

            var result = groups.Select(group =>
            {
                var memberBalances = group.Members.ToDictionary(m => m.Id, m => 0m);

                foreach (var transaction in group.Transactions)
                {
                    foreach (var split in transaction.Splits)
                    {
                        if (split.MemberId != transaction.PayerId)
                        {
                            memberBalances[split.MemberId] += split.Amount;
                            memberBalances[transaction.PayerId] -= split.Amount;
                        }
                    }
                }

                var userBalance = memberBalances.Values.FirstOrDefault();

                return new
                {
                    Id = group.Id,
                    Title = group.Title,
                    Balance = userBalance
                };
            });

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public IActionResult GetGroup(int id)
        {
            var group = _context.Groups.FirstOrDefault(g => g.Id == id);
            if (group == null) return NotFound();
            return Ok(group);
        }

        [HttpGet("{groupId:int}/members")]
        public IActionResult GetMembers(int groupId)
        {
            var group = _context.Groups
                .Include(g => g.Members)
                .Include(g => g.Transactions)
                    .ThenInclude(t => t.Splits)
                .FirstOrDefault(g => g.Id == groupId);

            if (group == null) return NotFound();

            var memberBalances = group.Members.ToDictionary(m => m.Id, m => 0m);

            foreach (var transaction in group.Transactions)
            {
                foreach (var split in transaction.Splits)
                {
                    if (split.MemberId != transaction.PayerId)
                    {
                        memberBalances[split.MemberId] += split.Amount;
                        memberBalances[transaction.PayerId] -= split.Amount;
                    }
                }
            }

            var members = group.Members.Select(m => new
            {
                Id = m.Id,
                Name = m.Name,
                Balance = memberBalances[m.Id]
            });

            return Ok(members);
        }

        [HttpGet("{groupId:int}/transactions")]
        public IActionResult GetTransactions(int groupId)
        {
            var group = _context.Groups
                .Include(g => g.Transactions)
                    .ThenInclude(t => t.Payer)
                .FirstOrDefault(g => g.Id == groupId);
            
            if (group == null) return NotFound();

            var transactions = group.Transactions.Select(t => new {
                Id = t.Id,
                Description = t.Description,
                Amount = t.TotalAmount,
                PaidByName = t.Payer.Name
            });

            return Ok(transactions);
        }

        [HttpPost]
        public IActionResult CreateGroup([FromBody] Group group)
        {
            _context.Groups.Add(group);
            _context.SaveChanges();
            return Ok(group);
        }

        [HttpPost("{groupId:int}/members")]
        public IActionResult AddMember(int groupId, [FromBody] AddMemberDto memberDto)
        {
            if (string.IsNullOrWhiteSpace(memberDto.Name))
            {
                return BadRequest("Member name is required");
            }
            
            var group = _context.Groups.FirstOrDefault(g => g.Id == groupId);
            if (group == null)
            {
                return NotFound($"Group not found");
            }
            
            var newMember = new Member
            {
                Name = memberDto.Name.Trim(),
                GroupId = groupId,
                Balances = new Dictionary<int, decimal>()
            };
            
            _context.Members.Add(newMember);
            _context.SaveChanges();
            
            return Ok(new
            {
                Id = newMember.Id,
                Name = newMember.Name,
                Balance = 0m
            });
        }

        [HttpDelete("{groupId:int}/members/{memberId:int}")]
        public IActionResult RemoveMember(int groupId, int memberId)
        {
            var group = _context.Groups
                .Include(g => g.Members)
                .Include(g => g.Transactions)
                    .ThenInclude(t => t.Splits)
                .FirstOrDefault(g => g.Id == groupId);

            if (group == null) return NotFound();
            
            var member = group.Members.FirstOrDefault(m => m.Id == memberId);
            if (member == null) return NotFound();

            var balance = CalculateMemberBalance(group.Transactions, memberId);

            if (Math.Abs(balance) > 0.01m)
            {
                return BadRequest($"Member must be settled first. Balance: {balance:F2}");
            }

            var splitsToRemove = group.Transactions
                .SelectMany(t => t.Splits)
                .Where(s => s.MemberId == memberId);

            _context.Splits.RemoveRange(splitsToRemove);

            var transactionsToRemove = group.Transactions
                .Where(t => t.PayerId == memberId);

            _context.Transactions.RemoveRange(transactionsToRemove);
            _context.Members.Remove(member);

            try
            {
                _context.SaveChanges();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Failed to remove member");
            }
        }

        [HttpPost("{groupId:int}/settle/{memberId:int}")]
        public IActionResult Settle(int groupId, int memberId)
        {
            var group = _context.Groups
                .Include(g => g.Members)
                .Include(g => g.Transactions)
                    .ThenInclude(t => t.Splits)
                .FirstOrDefault(g => g.Id == groupId);
            
            if (group == null) return NotFound();
            
            var member = group.Members.FirstOrDefault(m => m.Id == memberId);
            if (member == null) return NotFound();

            var balance = CalculateMemberBalance(group.Transactions, memberId);

            if (Math.Abs(balance) < 0.01m)
                return Ok(new { message = "Already settled" });

            foreach (var otherMember in group.Members.Where(m => m.Id != memberId))
            {
                var netAmount = CalculateNetAmount(group.Transactions, memberId, otherMember.Id);

                if (Math.Abs(netAmount) > 0.01m)
                {
                    var settlementTransaction = new Transaction
                    {
                        Description = $"Settlement: {member.Name} & {otherMember.Name}",
                        TotalAmount = Math.Abs(netAmount),
                        PayerId = netAmount > 0 ? memberId : otherMember.Id,
                        Splits = new List<Split>
                        {
                            new Split 
                            { 
                                MemberId = netAmount > 0 ? otherMember.Id : memberId, 
                                Amount = Math.Abs(netAmount) 
                            }
                        }
                    };

                    _context.Transactions.Add(settlementTransaction);
                    _context.Entry(settlementTransaction).Property("GroupId").CurrentValue = groupId;
                }
            }

            _context.SaveChanges();
            return Ok(new { message = "Settlement completed" });
        }

        private decimal CalculateMemberBalance(IEnumerable<Transaction> transactions, int memberId)
        {
            decimal balance = 0m;
            
            foreach (var transaction in transactions)
            {
                foreach (var split in transaction.Splits)
                {
                    if (split.MemberId == memberId && transaction.PayerId != memberId)
                        balance += split.Amount;
                    else if (transaction.PayerId == memberId && split.MemberId != memberId)
                        balance -= split.Amount;
                }
            }
            
            return balance;
        }

        private decimal CalculateNetAmount(IEnumerable<Transaction> transactions, int memberId1, int memberId2)
        {
            decimal netAmount = 0m;
            
            foreach (var transaction in transactions)
            {
                foreach (var split in transaction.Splits)
                {
                    if (split.MemberId == memberId1 && transaction.PayerId == memberId2)
                        netAmount += split.Amount;
                    else if (split.MemberId == memberId2 && transaction.PayerId == memberId1)
                        netAmount -= split.Amount;
                }
            }
            
            return netAmount;
        }
    }
}
