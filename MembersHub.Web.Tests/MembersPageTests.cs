using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using MembersHub.Core.Entities;
using MembersHub.Core.Interfaces;
using MembersHub.Infrastructure.Data;
using MembersHub.Web.Components.Pages;
using Microsoft.EntityFrameworkCore;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using Xunit;
using Microsoft.AspNetCore.Components;

namespace MembersHub.Web.Tests;

public class MembersPageTests : TestContext
{
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly Mock<ISnackbar> _mockSnackbar;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ISessionService> _mockSessionService;
    private readonly Mock<IHttpContextInfoService> _mockHttpContextInfoService;

    public MembersPageTests()
    {
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockDialogService = new Mock<IDialogService>();
        _mockSnackbar = new Mock<ISnackbar>();
        _mockAuditService = new Mock<IAuditService>();
        _mockSessionService = new Mock<ISessionService>();
        _mockHttpContextInfoService = new Mock<IHttpContextInfoService>();

        // Register MudBlazor services
        Services.AddMudServices();

        // Register test authorization
        this.AddTestAuthorization();

        // Register mocked services
        Services.AddSingleton(_mockScopeFactory.Object);
        Services.AddSingleton(_mockDialogService.Object);
        Services.AddSingleton(_mockSnackbar.Object);
        Services.AddSingleton(_mockAuditService.Object);
        Services.AddSingleton(_mockSessionService.Object);
        Services.AddSingleton(_mockHttpContextInfoService.Object);

        // Setup JSInterop for MudBlazor
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("mudPopover.initialize", _ => true);
        JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true);
        JSInterop.SetupVoid("mudScrollManager.lockScroll", _ => true);
        JSInterop.SetupVoid("mudScrollListener.listenForScroll", _ => true);

        // Add MudPopoverProvider to render tree
        ComponentFactories.AddStub<MudPopoverProvider>();
    }

    private void SetupMockDbContext(List<Member> members, List<MembershipType> membershipTypes)
    {
        var options = new DbContextOptionsBuilder<MembersHubContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new MembersHubContext(options);
        dbContext.MembershipTypes.AddRange(membershipTypes);
        dbContext.Members.AddRange(members);
        dbContext.SaveChanges();

        var mockServiceScope = new Mock<IServiceScope>();
        mockServiceScope.Setup(x => x.ServiceProvider.GetService(typeof(MembersHubContext)))
            .Returns(dbContext);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(x => x.GetService(typeof(MembersHubContext)))
            .Returns(dbContext);
        mockServiceScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);

        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockServiceScope.Object);
    }

    [Fact]
    public void Members_ShouldDisplayLastNameColumn()
    {
        // Arrange
        var membershipType = new MembershipType { Id = 1, Name = "Ενήλικες", MonthlyFee = 20, IsActive = true };
        var members = new List<Member>
        {
            new Member
            {
                Id = 1,
                FirstName = "Γιάννης",
                LastName = "Παπαδόπουλος",
                MemberNumber = "0001",
                Phone = "6901234567",
                MembershipTypeId = 1,
                MembershipType = membershipType,
                Status = MemberStatus.Active,
                CreatedAt = DateTime.UtcNow
            }
        };

        SetupMockDbContext(members, new List<MembershipType> { membershipType });

        // Act
        var cut = RenderComponent<Members>();

        // Assert
        Assert.Contains("Επώνυμο", cut.Markup);
    }

    [Fact]
    public void Members_ShouldDisplayFirstNameColumn()
    {
        // Arrange
        var membershipType = new MembershipType { Id = 1, Name = "Ενήλικες", MonthlyFee = 20, IsActive = true };
        var members = new List<Member>
        {
            new Member
            {
                Id = 1,
                FirstName = "Γιάννης",
                LastName = "Παπαδόπουλος",
                MemberNumber = "0001",
                Phone = "6901234567",
                MembershipTypeId = 1,
                MembershipType = membershipType,
                Status = MemberStatus.Active,
                CreatedAt = DateTime.UtcNow
            }
        };

        SetupMockDbContext(members, new List<MembershipType> { membershipType });

        // Act
        var cut = RenderComponent<Members>();

        // Assert
        Assert.Contains("Όνομα", cut.Markup);
    }

    [Fact]
    public void Members_ShouldNotDisplayFullNameColumn()
    {
        // Arrange
        var membershipType = new MembershipType { Id = 1, Name = "Ενήλικες", MonthlyFee = 20, IsActive = true };
        var members = new List<Member>
        {
            new Member
            {
                Id = 1,
                FirstName = "Γιάννης",
                LastName = "Παπαδόπουλος",
                MemberNumber = "0001",
                Phone = "6901234567",
                MembershipTypeId = 1,
                MembershipType = membershipType,
                Status = MemberStatus.Active,
                CreatedAt = DateTime.UtcNow
            }
        };

        SetupMockDbContext(members, new List<MembershipType> { membershipType });

        // Act
        var cut = RenderComponent<Members>();

        // Assert - "Ονοματεπώνυμο" should not be present as a column header
        Assert.DoesNotContain("Ονοματεπώνυμο", cut.Markup);
    }

    [Fact]
    public void Members_ShouldNotDisplayGenderColumn()
    {
        // Arrange
        var membershipType = new MembershipType { Id = 1, Name = "Ενήλικες", MonthlyFee = 20, IsActive = true };
        var members = new List<Member>
        {
            new Member
            {
                Id = 1,
                FirstName = "Γιάννης",
                LastName = "Παπαδόπουλος",
                MemberNumber = "0001",
                Phone = "6901234567",
                Gender = Gender.Male,
                MembershipTypeId = 1,
                MembershipType = membershipType,
                Status = MemberStatus.Active,
                CreatedAt = DateTime.UtcNow
            }
        };

        SetupMockDbContext(members, new List<MembershipType> { membershipType });

        // Act
        var cut = RenderComponent<Members>();

        // Assert - "Φύλο" should not be present as a column header
        Assert.DoesNotContain("Φύλο", cut.Markup);
    }

    [Fact]
    public void Members_ShouldNotDisplayCityColumn()
    {
        // Arrange
        var membershipType = new MembershipType { Id = 1, Name = "Ενήλικες", MonthlyFee = 20, IsActive = true };
        var members = new List<Member>
        {
            new Member
            {
                Id = 1,
                FirstName = "Γιάννης",
                LastName = "Παπαδόπουλος",
                MemberNumber = "0001",
                Phone = "6901234567",
                City = "Αθήνα",
                MembershipTypeId = 1,
                MembershipType = membershipType,
                Status = MemberStatus.Active,
                CreatedAt = DateTime.UtcNow
            }
        };

        SetupMockDbContext(members, new List<MembershipType> { membershipType });

        // Act
        var cut = RenderComponent<Members>();

        // Assert - "Πόλη" should not be present as a column header
        Assert.DoesNotContain("Πόλη", cut.Markup);
    }

    [Fact]
    public void Members_ShouldNotDisplayMembershipTypeColumn()
    {
        // Arrange
        var membershipType = new MembershipType { Id = 1, Name = "Ενήλικες", MonthlyFee = 20, IsActive = true };
        var members = new List<Member>
        {
            new Member
            {
                Id = 1,
                FirstName = "Γιάννης",
                LastName = "Παπαδόπουλος",
                MemberNumber = "0001",
                Phone = "6901234567",
                MembershipTypeId = 1,
                MembershipType = membershipType,
                Status = MemberStatus.Active,
                CreatedAt = DateTime.UtcNow
            }
        };

        SetupMockDbContext(members, new List<MembershipType> { membershipType });

        // Act
        var cut = RenderComponent<Members>();

        // Assert - "Τύπος Συνδρομής" should not be present as a column header
        Assert.DoesNotContain("Τύπος Συνδρομής", cut.Markup);
    }

    [Fact]
    public void Members_ShouldDisplayDepartmentColumn()
    {
        // Arrange
        var membershipType = new MembershipType { Id = 1, Name = "Ενήλικες", MonthlyFee = 20, IsActive = true };
        var members = new List<Member>
        {
            new Member
            {
                Id = 1,
                FirstName = "Γιάννης",
                LastName = "Παπαδόπουλος",
                MemberNumber = "0001",
                Phone = "6901234567",
                DepartmentId = 1,
                MembershipTypeId = 1,
                MembershipType = membershipType,
                Status = MemberStatus.Active,
                CreatedAt = DateTime.UtcNow
            }
        };

        SetupMockDbContext(members, new List<MembershipType> { membershipType });

        // Act
        var cut = RenderComponent<Members>();

        // Assert
        Assert.Contains("Τμήμα", cut.Markup);
    }
}
