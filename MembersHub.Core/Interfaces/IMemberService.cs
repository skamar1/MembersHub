using System.Collections.Generic;
using System.Threading.Tasks;
using MembersHub.Core.Entities;

namespace MembersHub.Core.Interfaces;

public interface IMemberService
{
    Task<Member?> GetByIdAsync(int id);
    Task<Member?> GetByMemberNumberAsync(string memberNumber);
    Task<IEnumerable<Member>> GetAllActiveAsync();
    Task<IEnumerable<Member>> SearchAsync(string searchTerm);
    Task<Member> CreateAsync(Member member);
    Task UpdateAsync(Member member);
    Task<decimal> GetOutstandingBalanceAsync(int memberId);
    Task<bool> ExistsAsync(string memberNumber);
}