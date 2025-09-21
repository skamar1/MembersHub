using System.Collections.Generic;
using System.Threading.Tasks;
using MembersHub.Core.Entities;

namespace MembersHub.Core.Interfaces;

public interface IMembershipTypeService
{
    Task<IEnumerable<MembershipType>> GetAllAsync();
    Task<MembershipType?> GetByIdAsync(int id);
    Task<MembershipType> CreateAsync(MembershipType membershipType);
    Task UpdateAsync(MembershipType membershipType);
    Task DeleteAsync(int id);
    Task<bool> CanDeleteAsync(int id);
}