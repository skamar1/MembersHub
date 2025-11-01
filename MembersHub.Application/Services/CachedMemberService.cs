using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using MembersHub.Application.DTOs;
using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;

namespace MembersHub.Application.Services;

/// <summary>
/// Cached wrapper for MemberService using Redis distributed caching
/// </summary>
public class CachedMemberService : IMemberService
{
    private readonly IMemberService _innerService;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachedMemberService> _logger;

    // Cache settings
    private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(5);
    private const string MemberByIdPrefix = "member:id:";
    private const string MemberByNumberPrefix = "member:number:";
    private const string AllMembersKey = "members:all";

    public CachedMemberService(
        IMemberService innerService,
        IDistributedCache cache,
        ILogger<CachedMemberService> logger)
    {
        _innerService = innerService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Member?> GetByIdAsync(int id)
    {
        var cacheKey = $"{MemberByIdPrefix}{id}";

        try
        {
            // Try to get from cache
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (cachedData != null)
            {
                _logger.LogInformation("Cache hit for member ID {MemberId}", id);
                return JsonSerializer.Deserialize<Member>(cachedData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading from cache for member ID {MemberId}", id);
        }

        // Cache miss - get from database
        _logger.LogInformation("Cache miss for member ID {MemberId}", id);
        var member = await _innerService.GetByIdAsync(id);

        if (member != null)
        {
            try
            {
                // Store in cache
                var serialized = JsonSerializer.Serialize(member);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = DefaultCacheExpiration
                };
                await _cache.SetStringAsync(cacheKey, serialized, options);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error writing to cache for member ID {MemberId}", id);
            }
        }

        return member;
    }

    public async Task<Member?> GetByMemberNumberAsync(string memberNumber)
    {
        var cacheKey = $"{MemberByNumberPrefix}{memberNumber}";

        try
        {
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (cachedData != null)
            {
                _logger.LogInformation("Cache hit for member number {MemberNumber}", memberNumber);
                return JsonSerializer.Deserialize<Member>(cachedData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading from cache for member number {MemberNumber}", memberNumber);
        }

        _logger.LogInformation("Cache miss for member number {MemberNumber}", memberNumber);
        var member = await _innerService.GetByMemberNumberAsync(memberNumber);

        if (member != null)
        {
            try
            {
                var serialized = JsonSerializer.Serialize(member);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = DefaultCacheExpiration
                };
                await _cache.SetStringAsync(cacheKey, serialized, options);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error writing to cache for member number {MemberNumber}", memberNumber);
            }
        }

        return member;
    }

    public async Task<IEnumerable<Member>> GetAllAsync()
    {
        try
        {
            var cachedData = await _cache.GetStringAsync(AllMembersKey);
            if (cachedData != null)
            {
                _logger.LogInformation("Cache hit for all members");
                return JsonSerializer.Deserialize<List<Member>>(cachedData) ?? new List<Member>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading all members from cache");
        }

        _logger.LogInformation("Cache miss for all members");
        var members = await _innerService.GetAllAsync();

        try
        {
            var serialized = JsonSerializer.Serialize(members);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2) // Shorter cache for list
            };
            await _cache.SetStringAsync(AllMembersKey, serialized, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error writing all members to cache");
        }

        return members;
    }

    public Task<IEnumerable<Member>> GetAllActiveAsync() => _innerService.GetAllActiveAsync();

    public Task<IEnumerable<Member>> SearchAsync(string searchTerm) => _innerService.SearchAsync(searchTerm);

    public async Task<Member> CreateAsync(Member member)
    {
        var result = await _innerService.CreateAsync(member);
        await InvalidateCacheAsync();
        return result;
    }

    public async Task UpdateAsync(Member member)
    {
        await _innerService.UpdateAsync(member);
        await InvalidateCacheAsync();
        await InvalidateMemberCacheAsync(member.Id);
    }

    public async Task DeleteAsync(int id)
    {
        await _innerService.DeleteAsync(id);
        await InvalidateCacheAsync();
        await InvalidateMemberCacheAsync(id);
    }

    public Task<decimal> GetOutstandingBalanceAsync(int memberId) => _innerService.GetOutstandingBalanceAsync(memberId);

    public Task<bool> ExistsAsync(string memberNumber) => _innerService.ExistsAsync(memberNumber);

    public Task<IEnumerable<Member>> GetMembersByStatusAsync(MemberStatus status) => _innerService.GetMembersByStatusAsync(status);

    public Task<IEnumerable<Member>> GetMembersWithOverduePaymentsAsync() => _innerService.GetMembersWithOverduePaymentsAsync();

    public Task<int> GetTotalMembersCountAsync() => _innerService.GetTotalMembersCountAsync();

    public Task<decimal> GetTotalMonthlyRevenueAsync() => _innerService.GetTotalMonthlyRevenueAsync();

    public async Task ActivateMemberAsync(int memberId)
    {
        await _innerService.ActivateMemberAsync(memberId);
        await InvalidateCacheAsync();
        await InvalidateMemberCacheAsync(memberId);
    }

    public async Task DeactivateMemberAsync(int memberId)
    {
        await _innerService.DeactivateMemberAsync(memberId);
        await InvalidateCacheAsync();
        await InvalidateMemberCacheAsync(memberId);
    }

    public async Task SuspendMemberAsync(int memberId, string reason)
    {
        await _innerService.SuspendMemberAsync(memberId, reason);
        await InvalidateCacheAsync();
        await InvalidateMemberCacheAsync(memberId);
    }

    // Cache invalidation methods
    private async Task InvalidateCacheAsync()
    {
        try
        {
            await _cache.RemoveAsync(AllMembersKey);
            _logger.LogInformation("Invalidated all members cache");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error invalidating all members cache");
        }
    }

    private async Task InvalidateMemberCacheAsync(int id)
    {
        try
        {
            await _cache.RemoveAsync($"{MemberByIdPrefix}{id}");
            _logger.LogInformation("Invalidated cache for member ID {MemberId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error invalidating cache for member ID {MemberId}", id);
        }
    }
}
