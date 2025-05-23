using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GroupExpenseApp.Data;
using GroupExpenseApp.Models;
using System.Linq;
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
                        }
                    }

                    var payerSplit = transaction.Splits.FirstOrDefault(s => s.MemberId == transaction.PayerId);
                    var payerOwed = transaction.TotalAmount - (payerSplit?.Amount ?? 0);
                    memberBalances[transaction.PayerId] -= payerOwed;
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


        [HttpGet("{id}")]
        public IActionResult GetGroup(int id)
        {
            var group = _context.Groups.FirstOrDefault(g => g.Id == id);
            if (group == null) return NotFound();
            return Ok(group);
        }

        [HttpGet("{groupId}/members")]
        public IActionResult GetMembers(int groupId)
        {
            var group = _context.Groups
                .Include(g => g.Members)
                .Include(g => g.Transactions)
                    .ThenInclude(t => t.Splits)
                .FirstOrDefault(g => g.Id == groupId);

            if (group == null) return NotFound();

            var memberBalances = new Dictionary<int, decimal>();

            foreach (var member in group.Members)
            {
                memberBalances[member.Id] = 0;
            }

            foreach (var transaction in group.Transactions)
            {
                var totalSplitAmount = transaction.Splits.Sum(s => s.Amount);

                foreach (var split in transaction.Splits)
                {
                    if (split.MemberId != transaction.PayerId)
                    {
                        memberBalances[split.MemberId] += split.Amount;
                    }
                }

                var payerSplit = transaction.Splits.FirstOrDefault(s => s.MemberId == transaction.PayerId);
                var payerOwedAmount = transaction.TotalAmount - (payerSplit?.Amount ?? 0);

                memberBalances[transaction.PayerId] -= payerOwedAmount;
            }

            var members = group.Members.Select(m => new
            {
                Id = m.Id,
                Name = m.Name,
                Balance = memberBalances[m.Id]
            });

            return Ok(members);
        }

        [HttpGet("{groupId}/transactions")]
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

        [HttpPost("{groupId}/members")]
        public IActionResult AddMember(int groupId, [FromBody] AddMemberDto memberDto)
        {
            try
            {
                Console.WriteLine($"=== AddMember Debug ===");
                Console.WriteLine($"GroupId: {groupId}");
                Console.WriteLine($"Member name: {memberDto.Name}");
                
                if (string.IsNullOrEmpty(memberDto.Name))
                {
                    return BadRequest("Member name is required");
                }
                
                var group = _context.Groups.FirstOrDefault(g => g.Id == groupId);
                if (group == null)
                {
                    return NotFound($"Group {groupId} not found");
                }
                
                var newMember = new Member
                {
                    Name = memberDto.Name,
                    GroupId = groupId,
                    Balances = new Dictionary<int, decimal>()
                };
                
                _context.Members.Add(newMember);
                _context.SaveChanges();
                
                var result = new
                {
                    Id = newMember.Id,
                    Name = newMember.Name,
                    Balance = 0
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                return BadRequest($"Server error: {ex.Message}");
            }
        }

       [HttpDelete("{groupId}/members/{memberId}")]
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

            Console.WriteLine($"=== Attempting to remove member {memberId} from group {groupId}");

            var balance = 0m;
            foreach (var t in group.Transactions)
            {
                foreach (var s in t.Splits)
                {
                    if (s.MemberId == memberId && t.PayerId != memberId)
                        balance += s.Amount;
                    else if (t.PayerId == memberId && s.MemberId != memberId)
                        balance -= s.Amount;
                }
            }

            if (Math.Abs(balance) > 0.01m)
            {
                Console.WriteLine($"Balance check failed. Balance: {balance:F2}");
                return BadRequest($"Member must be settled first. Balance: {balance:F2}");
            }

            var transactions = group.Transactions.ToList();

            var splitsToRemove = transactions
                .SelectMany(t => t.Splits)
                .Where(s => s.MemberId == memberId)
                .ToList();

            Console.WriteLine($"Removing {splitsToRemove.Count} splits...");
            _context.Splits.RemoveRange(splitsToRemove);

            var transactionsToRemove = transactions
                .Where(t => t.PayerId == memberId)
                .ToList();

            Console.WriteLine($"Removing {transactionsToRemove.Count} transactions where member was payer...");
            _context.Transactions.RemoveRange(transactionsToRemove);

            _context.Members.Remove(member);

            try
            {
                _context.SaveChanges();
                Console.WriteLine($"Member {memberId} removed successfully.");
                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                return StatusCode(500, "Failed to delete member: " + ex.Message);
            }
        }



        [HttpPost("{groupId}/settle/{memberId}")]
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

            var balance = 0m;
            foreach (var t in group.Transactions)
            {
                foreach (var s in t.Splits)
                {
                    if (s.MemberId == memberId && t.PayerId != memberId)
                        balance += s.Amount;
                    else if (t.PayerId == memberId && s.MemberId != memberId)
                        balance -= s.Amount;
                }
            }

            if (Math.Abs(balance) < 0.01m)
                return Ok(new { message = "Already settled." });

            foreach (var other in group.Members.Where(m => m.Id != memberId))
            {
                var netBetween = 0m;

                foreach (var t in group.Transactions)
                {
                    foreach (var s in t.Splits)
                    {
                        if (s.MemberId == memberId && t.PayerId == other.Id)
                            netBetween += s.Amount;
                        else if (s.MemberId == other.Id && t.PayerId == memberId)
                            netBetween -= s.Amount;
                    }
                }

                if (Math.Abs(netBetween) > 0.01m)
                {
                    var settlementTx = new Transaction
                    {
                        Description = $"Settlement between {member.Name} and {other.Name}",
                        TotalAmount = Math.Abs(netBetween),
                        PayerId = netBetween > 0 ? memberId : other.Id,
                        Splits = new List<Split>
                        {
                            new Split { MemberId = netBetween > 0 ? other.Id : memberId, Amount = Math.Abs(netBetween) }
                        }
                    };

                    _context.Transactions.Add(settlementTx);
                    _context.Entry(settlementTx).Property("GroupId").CurrentValue = groupId;
                }
            }

            _context.SaveChanges();
            return Ok(new { message = "Settled." });
        }

    }
}