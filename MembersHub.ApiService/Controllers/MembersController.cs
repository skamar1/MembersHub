using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MembersHub.Core.Interfaces;
using MembersHub.Core.Entities;
using System.Security.Claims;

namespace MembersHub.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires authentication for all endpoints
public class MembersController : ControllerBase
{
    private readonly IMemberService _memberService;
    private readonly IAuditService _auditService;
    private readonly ISessionService _sessionService;
    private readonly IHttpContextInfoService _httpContextInfo;

    public MembersController(
        IMemberService memberService,
        IAuditService auditService,
        ISessionService sessionService,
        IHttpContextInfoService httpContextInfo)
    {
        _memberService = memberService;
        _auditService = auditService;
        _sessionService = sessionService;
        _httpContextInfo = httpContextInfo;
    }

    [HttpGet]
    public async Task<IActionResult> GetMembers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        try
        {
            var members = await _memberService.GetAllAsync();
            
            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(search))
            {
                members = members.Where(m => 
                    m.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    m.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) == true ||
                    m.Phone.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Apply pagination
            var totalCount = members.Count();
            var pagedMembers = members
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new
            {
                data = pagedMembers.Select(m => new
                {
                    id = m.Id,
                    firstName = m.FirstName,
                    lastName = m.LastName,
                    fullName = m.FullName,
                    email = m.Email,
                    phone = m.Phone,
                    dateOfBirth = m.DateOfBirth,
                    memberNumber = m.MemberNumber,
                    status = m.Status.ToString(),
                    createdAt = m.CreatedAt
                }),
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetMember(int id)
    {
        try
        {
            var member = await _memberService.GetByIdAsync(id);
            if (member == null)
            {
                return NotFound(new { error = "Το μέλος δεν βρέθηκε" });
            }

            // Log view action
            var currentUser = await _sessionService.GetCurrentUserAsync();
            if (currentUser != null)
            {
                await _auditService.LogViewAsync(member, 
                    currentUser.Id, currentUser.Username, currentUser.FullName,
                    _httpContextInfo.GetIpAddress());
            }

            return Ok(new
            {
                id = member.Id,
                firstName = member.FirstName,
                lastName = member.LastName,
                fullName = member.FullName,
                email = member.Email,
                phone = member.Phone,
                dateOfBirth = member.DateOfBirth,
                memberNumber = member.MemberNumber,
                status = member.Status.ToString(),
                createdAt = member.CreatedAt,
                updatedAt = member.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Owner,Secretary")] // Only these roles can create members
    public async Task<IActionResult> CreateMember([FromBody] CreateMemberRequest request)
    {
        try
        {
            var member = new Member
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                DateOfBirth = request.DateOfBirth,
                MembershipTypeId = request.MembershipTypeId,
                Status = request.Status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _memberService.CreateAsync(member);

            // Log create action
            var currentUser = await _sessionService.GetCurrentUserAsync();
            if (currentUser != null)
            {
                await _auditService.LogCreateAsync(member,
                    currentUser.Id, currentUser.Username, currentUser.FullName,
                    _httpContextInfo.GetIpAddress());
            }

            return CreatedAtAction(nameof(GetMember), new { id = member.Id }, new
            {
                id = member.Id,
                firstName = member.FirstName,
                lastName = member.LastName,
                fullName = member.FullName,
                email = member.Email,
                phone = member.Phone,
                dateOfBirth = member.DateOfBirth,
                memberNumber = member.MemberNumber,
                status = member.Status.ToString(),
                createdAt = member.CreatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Owner,Secretary")] // Only these roles can update members
    public async Task<IActionResult> UpdateMember(int id, [FromBody] UpdateMemberRequest request)
    {
        try
        {
            var existingMember = await _memberService.GetByIdAsync(id);
            if (existingMember == null)
            {
                return NotFound(new { error = "Το μέλος δεν βρέθηκε" });
            }

            // Keep old values for audit
            var oldMember = new Member
            {
                Id = existingMember.Id,
                FirstName = existingMember.FirstName,
                LastName = existingMember.LastName,
                Email = existingMember.Email,
                Phone = existingMember.Phone,
                DateOfBirth = existingMember.DateOfBirth,
                MembershipTypeId = existingMember.MembershipTypeId,
                Status = existingMember.Status,
                MemberNumber = existingMember.MemberNumber,
                CreatedAt = existingMember.CreatedAt,
                UpdatedAt = existingMember.UpdatedAt
            };

            // Update member
            existingMember.FirstName = request.FirstName;
            existingMember.LastName = request.LastName;
            existingMember.Email = request.Email;
            existingMember.Phone = request.Phone;
            existingMember.DateOfBirth = request.DateOfBirth;
            existingMember.MembershipTypeId = request.MembershipTypeId;
            existingMember.Status = request.Status;
            existingMember.UpdatedAt = DateTime.UtcNow;

            await _memberService.UpdateAsync(existingMember);

            // Log update action
            var currentUser = await _sessionService.GetCurrentUserAsync();
            if (currentUser != null)
            {
                await _auditService.LogUpdateAsync(oldMember, existingMember,
                    currentUser.Id, currentUser.Username, currentUser.FullName,
                    _httpContextInfo.GetIpAddress());
            }

            return Ok(new
            {
                id = existingMember.Id,
                firstName = existingMember.FirstName,
                lastName = existingMember.LastName,
                fullName = existingMember.FullName,
                email = existingMember.Email,
                phone = existingMember.Phone,
                dateOfBirth = existingMember.DateOfBirth,
                memberNumber = existingMember.MemberNumber,
                status = existingMember.Status.ToString(),
                updatedAt = existingMember.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Owner")] // Only Admin and Owner can delete members
    public async Task<IActionResult> DeleteMember(int id)
    {
        try
        {
            var member = await _memberService.GetByIdAsync(id);
            if (member == null)
            {
                return NotFound(new { error = "Το μέλος δεν βρέθηκε" });
            }

            await _memberService.DeleteAsync(id);

            // Log delete action
            var currentUser = await _sessionService.GetCurrentUserAsync();
            if (currentUser != null)
            {
                await _auditService.LogDeleteAsync(member,
                    currentUser.Id, currentUser.Username, currentUser.FullName,
                    _httpContextInfo.GetIpAddress());
            }

            return Ok(new { message = $"Το μέλος '{member.FullName}' διαγράφηκε επιτυχώς" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class CreateMemberRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Phone { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public int MembershipTypeId { get; set; }
    public MemberStatus Status { get; set; }
}

public class UpdateMemberRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Phone { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public int MembershipTypeId { get; set; }
    public MemberStatus Status { get; set; }
}