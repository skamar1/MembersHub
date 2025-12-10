using System.Collections.Generic;
using System.Threading.Tasks;
using MembersHub.Core.Entities;

namespace MembersHub.Core.Interfaces;

public interface IMemberService
{
    Task<Member?> GetByIdAsync(int id);
    Task<Member?> GetByMemberNumberAsync(string memberNumber);
    Task<IEnumerable<Member>> GetAllAsync();
    Task<IEnumerable<Member>> GetAllActiveAsync();
    Task<IEnumerable<Member>> SearchAsync(string searchTerm);
    Task<Member> CreateAsync(Member member, int? createdByUserId = null);
    Task UpdateAsync(Member member);
    Task DeleteAsync(int id);
    Task<decimal> GetOutstandingBalanceAsync(int memberId);
    Task<bool> ExistsAsync(string memberNumber);

    // Additional business methods
    Task<IEnumerable<Member>> GetMembersByStatusAsync(MemberStatus status);
    Task<IEnumerable<Member>> GetMembersWithOverduePaymentsAsync();
    Task<int> GetTotalMembersCountAsync();
    Task<decimal> GetTotalMonthlyRevenueAsync();
    Task ActivateMemberAsync(int memberId);
    Task DeactivateMemberAsync(int memberId);
    Task SuspendMemberAsync(int memberId, string reason);
}