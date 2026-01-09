# Προδιαγραφές Εφαρμογής MembersHub

## Επισκόπηση Έργου

**Όνομα Έργου:** MembersHub
**Τεχνολογίες:** .NET 9, .NET Aspire, Blazor Server, MudBlazor, Entity Framework Core
**Βάση Δεδομένων:** SQL Server ή PostgreSQL
**Στόχος:** Διαχείριση συνδρομών, εισπράξεων και δαπανών αθλητικού σωματείου με cloud-native architecture

### Γιατί .NET Aspire;
- **Simplified Development:** Built-in service discovery, configuration, και health checks
- **Enhanced Observability:** Automatic telemetry, logging, και monitoring
- **Cloud-Native Ready:** Easy deployment σε Azure, AWS, ή Kubernetes
- **Developer Experience:** Integrated dashboard για development και debugging
- **Production Monitoring:** Real-time insights και performance metrics

## Τεχνική Αρχιτεκτονική

### Frontend
- **Blazor Server** με **MudBlazor** UI Framework
- **Progressive Web App (PWA)** για mobile support
- **Responsive Design** - λειτουργεί σε desktop, tablet, mobile
- **Offline capabilities** για εισπράκτορες (με sync όταν υπάρχει internet)

### Backend
- **.NET 9** με **.NET Aspire** για cloud-native architecture
- **ASP.NET Core 9** Web API
- **Entity Framework Core 9** για database access
- **JWT Authentication** για ασφάλεια
- **SignalR** για real-time ενημερώσεις

### .NET Aspire Benefits
- **Service Discovery:** Automatic service registration και discovery
- **Configuration Management:** Centralized configuration για όλα τα services
- **Health Checks:** Built-in health monitoring για database, email, SMS services
- **Observability:** Telemetry, logging, και metrics out-of-the-box
- **Development Experience:** Improved local development με Aspire Dashboard
- **Container Orchestration:** Easy deployment με Docker containers

### Εξωτερικές Υπηρεσίες
- **Email Services:** SMTP Server, SendGrid API, ή Mailgun API (configurable)
- **SMS Services:** Cosmote API, Vodafone Business SMS, ή Generic SMS Gateway (configurable)
- **PDF Generation:** για αποδείξεις
- **QR Code Generation:** για επαλήθευση αποδείξεων

## .NET Aspire Architecture

### Service Structure
```
MembersHub.AppHost              // Aspire Orchestration
├── MembersHub.Web              // Blazor Server App (MudBlazor)
├── MembersHub.ApiService       // Web API για mobile και external calls
├── MembersHub.Worker           // Background services (email/SMS queue)
├── MembersHub.ServiceDefaults  // Shared Aspire configuration
│
├── Shared Libraries:
├── MembersHub.Core             // Domain Models, Entities, Interfaces
├── MembersHub.Application      // Business Logic, Services, DTOs
├── MembersHub.Infrastructure   // Database, Repositories, External APIs
└── MembersHub.Shared           // Common utilities, Constants, Extensions
```

### Shared Libraries Architecture

#### MembersHub.Core
```csharp
// Domain entities που μοιράζονται παντού
public class Member { ... }
public class Payment { ... }  
public class Expense { ... }
public class User { ... }

// Interfaces για dependency injection
public interface IMemberService { ... }
public interface IPaymentService { ... }
public interface IEmailService { ... }
```

#### MembersHub.Application  
```csharp
// Business logic services
public class PaymentService : IPaymentService { ... }
public class MemberService : IMemberService { ... }

// DTOs για data transfer
public class CreatePaymentDto { ... }
public class MemberListDto { ... }

// Validation rules
public class CreatePaymentValidator : AbstractValidator<CreatePaymentDto> { ... }
```

#### MembersHub.Infrastructure
```csharp
// Database context που χρησιμοποιείται από όλα τα projects
public class MembersHubContext : DbContext { ... }

// Repository implementations
public class PaymentRepository : IPaymentRepository { ... }

// External service implementations  
public class EmailService : IEmailService { ... }
public class SmsService : ISmsService { ... }
```

### Project Dependencies
```
Web Project:
├── References: Core, Application, Infrastructure, Shared
├── Purpose: Blazor UI, Server-side rendering
└── Specific: MudBlazor components, SignalR hubs

ApiService Project:  
├── References: Core, Application, Infrastructure, Shared
├── Purpose: RESTful API για mobile app
└── Specific: API controllers, JWT authentication, Swagger

Worker Project:
├── References: Core, Application, Infrastructure, Shared  
├── Purpose: Background processing
└── Specific: Hangfire/Quartz jobs, Email/SMS queues
```

### Aspire Dashboard Features
- **Real-time Service Monitoring:** Όλα τα services σε ένα dashboard
- **Distributed Tracing:** End-to-end request tracking
- **Metrics Collection:** Performance και business metrics
- **Log Aggregation:** Centralized logging από όλα τα services
- **Health Status:** Visual health checks για dependencies

### Service Discovery & Configuration
```json
// appsettings.json με Aspire integration
{
  "Aspire": {
    "ServiceDiscovery": {
      "Enabled": true
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ClubManagementDB;Trusted_Connection=true"
  }
}
```

### Health Checks Integration
- **Database Health:** EF Core connection monitoring
- **Email Service Health:** SMTP/API endpoint checks  
- **SMS Service Health:** Provider API availability
- **External Dependencies:** Third-party service monitoring

### Observability
- **OpenTelemetry Integration:** Automatic telemetry collection
- **Custom Metrics:** Business metrics (payments/hour, failed emails)
- **Performance Counters:** Response times, throughput
- **Alert Rules:** Configurable alerting για critical failures

## Χρήστες & Δικαιώματα

### Ρόλοι Χρηστών

#### 1. **Administrator**
- Πλήρη πρόσβαση σε όλες τις λειτουργίες
- Διαχείριση χρηστών και δικαιωμάτων
- Ρυθμίσεις συστήματος (email/SMS providers)
- Backup και maintenance
- Οικονομικές αναφορές και στατιστικά

#### 2. **Secretary (Γραμματέας)**
- Διαχείριση μελών (προσθήκη, επεξεργασία, διαγραφή)
- Διαχείριση τύπων συνδρομών και τιμολογίων
- Έκδοση λογαριασμών συνδρομών
- Παρακολούθηση οφειλών
- Βασικές αναφορές

#### 3. **Collector (Εισπράκτορας)**
- Καταχώρηση εισπράξεων μέσω mobile
- Καταχώρηση δαπανών με φωτογραφίες αποδείξεων  
- Δημιουργία και αποστολή αποδείξεων
- Προβολή στοιχείων μελών και οφειλών
- Offline λειτουργία με sync

#### 4. **Member (Μέλος)** - Μελλοντική επέκταση
- Προβολή δικών του στοιχείων και οφειλών
- Ιστορικό πληρωμών

## Βάση Δεδομένων

### Κύριες Οντότητες

#### Users
```sql
Id (int, Primary Key)
Username (varchar(50), Unique)
Email (varchar(100))
PasswordHash (varchar(256))
Role (enum: Admin, Secretary, Collector, Member)
IsActive (bit)
CreatedAt (datetime)
LastLogin (datetime)
```

#### Members
```sql
Id (int, Primary Key)
FirstName (varchar(100))
LastName (varchar(100))
Email (varchar(100))
Phone (varchar(20))
DateOfBirth (date)
MembershipTypeId (int, Foreign Key)
MemberNumber (varchar(20), Unique)
Status (enum: Active, Inactive, Suspended)
CreatedAt (datetime)
UpdatedAt (datetime)
```

#### MembershipTypes
```sql
Id (int, Primary Key)
Name (varchar(100)) -- π.χ. "Ενήλικες", "Παιδιά", "Φοιτητές"
MonthlyFee (decimal)
Description (text)
IsActive (bit)
```

#### Subscriptions
```sql
Id (int, Primary Key)
MemberId (int, Foreign Key)
Year (int)
Month (int)
Amount (decimal)
DueDate (date)
Status (enum: Pending, Paid, Overdue)
Notes (text)
CreatedAt (datetime)
```

#### Payments
```sql
Id (int, Primary Key)
MemberId (int, Foreign Key)
SubscriptionId (int, Foreign Key, nullable)
Amount (decimal)
PaymentDate (datetime)
CollectorId (int, Foreign Key)
PaymentMethod (enum: Cash, Card, BankTransfer)
ReceiptNumber (varchar(20), Unique)
Notes (text)
IsSynced (bit) -- για offline payments
EmailSent (bit)
SMSSent (bit)
CreatedAt (datetime)
```

#### Expenses
```sql
Id (int, Primary Key)
CollectorId (int, Foreign Key)
Date (date)
Amount (decimal)
Category (varchar(100)) -- π.χ. "Καύσιμα", "Γεύματα", "Υλικά"
Description (text)
ReceiptImagePath (varchar(500))
IsApproved (bit, default false)
ApprovedBy (int, Foreign Key, nullable)
IsSynced (bit) -- για offline expenses
CreatedAt (datetime)
```

#### EmailLogs
```sql
Id (int, Primary Key)
ToEmail (varchar(100))
Subject (varchar(200))
Status (enum: Sent, Failed, Pending)
ErrorMessage (text)
PaymentId (int, Foreign Key, nullable)
SentAt (datetime)
```

#### SMSLogs
```sql
Id (int, Primary Key)
ToPhone (varchar(20))
Message (text)
Status (enum: Sent, Failed, Pending)
ErrorMessage (text)
PaymentId (int, Foreign Key, nullable)
SentAt (datetime)
```

## Βασικές Λειτουργίες

### 1. Διαχείριση Μελών

#### Λίστα Μελών
- **Filters:** Status, Membership Type, Registration Date
- **Search:** Όνομα, Email, Τηλέφωνο, Αριθμός Μέλους
- **Actions:** Προσθήκη, Επεξεργασία, Απενεργοποίηση
- **Export:** Excel, PDF

#### Φόρμα Μέλους
- **Required Fields:** Όνομα, Επώνυμο, Τηλέφωνο, Τύπος Συνδρομής
- **Optional Fields:** Email, Ημερομηνία Γέννησης, Σημειώσεις
- **Auto-generation:** Αυτόματος αριθμός μέλους
- **Validation:** Email format, Unique phone number

### 2. Διαχείριση Συνδρομών

#### Bulk Subscription Creation
- Δημιουργία συνδρομών για όλα τα ενεργά μέλη για συγκεκριμένο μήνα/έτος
- Automatic calculation βάσει membership type
- Email notifications για νέες οφειλές

#### Outstanding Balances
- Real-time view των οφειλών ανά μέλος
- Aging report (0-30, 31-60, 60+ ημέρες)
- Bulk email reminders

### 3. Mobile Payment Collection

#### Member Search
- **Search bar** με autocomplete
- **Recent members** list
- **QR Code scanner** για κάρτες μελών
- **Barcode scanner** για member numbers

#### Payment Entry
- **Large, finger-friendly buttons** για ποσά
- **Quick amounts:** €10, €20, €25, €30, Custom
- **Payment method selection:** Μετρητά, Κάρτα, Έμβασμα
- **Partial payment support**
- **Note field** για σχόλια

#### Receipt Generation & Delivery
- **One-tap receipt creation**
- **Automatic PDF generation**
- **Email delivery** με PDF attachment
- **SMS fallback** αν δεν υπάρχει email ή αποτύχει
- **Receipt preview** πριν την αποστολή

### 4. Expense Management

#### Expense Entry (Mobile)
- **Category dropdown:** Προκαθορισμένες κατηγορίες
- **Amount input** με μεγάλα κουμπιά
- **Description field**
- **Camera integration** για αποδείξεις
- **GPS location** auto-capture
- **Multiple receipt photos** support

#### Receipt Processing
- **Photo enhancement** για καλύτερη ποιότητα
- **OCR integration** για αυτόματη ανάγνωση ποσών (optional)
- **Image compression** για αποθήκευση

#### Approval Workflow
- **Pending expenses** list για admins
- **Bulk approval** functionality  
- **Expense reports** με φωτογραφίες αποδείξεων

## Responsive Design & PWA

### Mobile-First Approach
- **Touch-friendly interface:** Μεγάλα κουμπιά (min 44px)
- **Gesture support:** Swipe actions
- **Adaptive layouts:** Grid που προσαρμόζεται σε screen size
- **Fast loading:** Optimized για mobile networks

### PWA Features
- **Installable:** Add to Home Screen capability
- **Offline functionality:** Critical features work offline
- **Background sync:** Auto-sync όταν επιστρέφει connection
- **Push notifications:** Για νέες οφειλές και reminders

### Breakpoints
```css
Mobile: < 768px
Tablet: 768px - 1024px  
Desktop: > 1024px
```

## Email & SMS System

### Configuration Panel (Admin Only)

#### Email Provider Setup
```
○ SMTP Server Configuration
  - Host, Port, Username, Password
  - SSL/TLS Settings
  
○ SendGrid API
  - API Key Configuration
  - Template Management
  
○ Mailgun API  
  - Domain, API Key
  - Region Selection
```

#### SMS Provider Setup
```
○ Cosmote Business SMS
  - Username, Password
  - Sender ID
  
○ Vodafone Business SMS
  - API Credentials
  
○ Generic SMS Gateway
  - API URL, Headers, Payload Format
```

#### Fallback Logic
- **Primary:** Email delivery
- **Secondary:** SMS if email fails or no email exists
- **Retry:** 3 attempts για email, 2 για SMS
- **Logging:** Πλήρες log όλων των αποστολών

### Receipt Email Template

#### Στοιχεία Email
- **Subject:** "Απόδειξη Εισπραξης #{ReceiptNumber} - {ClubName}"
- **Header:** Logo και στοιχεία σωματείου
- **Body:** Ευχαριστήριο μήνυμα στα ελληνικά
- **Attachment:** PDF απόδειξη
- **Footer:** Στοιχεία επικοινωνίας σωματείου

#### PDF Receipt Format
```
ΑΠΟΔΕΙΞΗ ΕΙΣΠΡΑΞΗΣ #{ReceiptNumber}
───────────────────────────────────
{ClubName}
{ClubAddress}
ΑΦΜ: {ClubVAT}

Ημερομηνία: {PaymentDate}
Μέλος: {MemberName} (#{MemberNumber})
Περιγραφή: {Description}
Ποσό: €{Amount}

Εισπράκτορας: {CollectorName}
Υπογραφή: ________________

[QR Code για verification]
```

### SMS Templates

#### Payment Confirmation
```
"Αγαπητέ {FirstName}, η συνδρομή σας €{Amount} εισπράχθηκε επιτυχώς. 
Απόδειξη #{ReceiptNumber}. Ευχαριστούμε!
- {ClubName}"
```

#### Email Fallback
```
"Η απόδειξή σας δεν στάλθηκε με email. 
Παρακαλώ επικοινωνήστε στο {ClubPhone} για παραλαβή. 
Απόδειξη #{ReceiptNumber}"
```

## Offline Functionality

### Local Storage Strategy
- **SQLite database** για offline data
- **Critical data caching:** Members, Subscription types, Recent payments
- **Image storage:** Local για receipt photos
- **Sync queue:** Pending uploads

### Sync Process
- **Automatic sync** όταν επιστρέφει internet
- **Conflict resolution:** Last-write-wins για payments
- **Progress indication:** Visual feedback κατά τη διάρκεια sync
- **Error handling:** Retry mechanism με exponential backoff

### Offline Indicators
- **Connection status:** Visible indicator στο UI
- **Offline mode badge:** Στα synced/unsynced records
- **Data freshness:** Timestamp του τελευταίου sync

## Dashboards & Reporting

### Admin Dashboard
- **Today's Collections:** Real-time total
- **Outstanding Balance:** Total και breakdown
- **Active Members:** Count και trends
- **System Status:** Email/SMS health check
- **Recent Activity:** Live feed

### Collector Dashboard  
- **My Collections Today:** Personal stats
- **Pending Members:** Members με οφειλές στην περιοχή του
- **Expense Summary:** Daily/Weekly totals
- **Sync Status:** Unsynced items count

### Financial Reports
- **Monthly Collection Report:** Detailed με breakdown ανά collector
- **Outstanding Balances:** Aging report
- **Expense Report:** Categorized με photos
- **Member Activity:** Payment history per member

## Security & Authentication

### Authentication & Authorization
- **JWT Token-based** authentication
- **Role-based access control** (RBAC) - Admin, Secretary, Collector, Member
- **Session timeout:** Configurable με automatic refresh
- **Mobile authentication:** PIN ή fingerprint/Face ID για collectors

### Security Measures (Phase 1)

#### 1. Rate Limiting & Brute Force Protection
```csharp
// API rate limiting
builder.Services.AddRateLimiter(options =>
{
    // Login attempts
    options.AddFixedWindowLimiter("LoginPolicy", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(15);
    });
    
    // API calls  
    options.AddTokenBucketLimiter("ApiPolicy", opt =>
    {
        opt.TokenLimit = 100;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
    });
});
```

#### 2. Input Validation & XSS Prevention
```csharp
// FluentValidation με security rules
public class CreatePaymentValidator : AbstractValidator<CreatePaymentDto>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .Must(BeCleanInput) // XSS prevention
            .WithMessage("Μη έγκυρα χαρακτήρες στις σημειώσεις");
            
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .LessThan(10000); // Business rule + security
    }
}
```

#### 3. File Upload Security
```csharp
public class SecureFileUploadService : IFileUploadService
{
    private readonly string[] _allowedExtensions = {".jpg", ".jpeg", ".png", ".pdf"};
    private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB
    
    public async Task<FileUploadResult> UploadReceiptAsync(IFormFile file)
    {
        // File type validation
        // Size validation  
        // Virus scanning (future)
        // Content type verification
        // Safe file naming
    }
}
```

#### 4. CSRF Protection για Blazor
```csharp
// Anti-forgery tokens
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
```

#### 5. HTTPS & Security Headers
```csharp
// Security headers middleware
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

app.UseSecurityHeaders(policies =>
    policies.AddFrameOptionsDeny()
           .AddXssProtectionBlock()
           .AddContentTypeOptionsNoSniff()
           .AddReferrerPolicyStrictOriginWhenCrossOrigin()
);
```

#### 6. Data Protection & Encryption
```csharp
// Sensitive data encryption
[PersonalData]
public class Member
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    
    [Encrypted] // Custom attribute για field-level encryption
    public string? Phone { get; set; }
    
    [Encrypted]
    public string? Email { get; set; }
}
```

### Mobile Security (Collectors)

#### Device Authentication
```csharp
public class CollectorAuthService
{
    public async Task<AuthResult> AuthenticateAsync(
        string username,
        string pin,
        string deviceFingerprint)
    {
        // PIN verification με rate limiting
        // Device fingerprint validation
        // Optional location validation
        // Time-based access restrictions
        
        return new AuthResult 
        { 
            Success = true, 
            Token = jwtToken,
            ExpiresAt = DateTime.UtcNow.AddHours(8)
        };
    }
}
```

#### Trusted Devices
```csharp
public class TrustedDevice
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Fingerprint { get; set; } = string.Empty;
    public DateTime FirstSeen { get; set; }
    public DateTime LastUsed { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
}
```

### Audit Trail & Monitoring
```csharp
public class AuditLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty; // Login, Payment, etc.
    public string EntityType { get; set; } = string.Empty; // Member, Payment
    public int? EntityId { get; set; }
    public string? OldValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### API Security
```csharp
// API versioning & documentation security
builder.Services.AddApiVersioning();
builder.Services.AddVersionedApiExplorer();

// Swagger security (προσοχή σε production)
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference 
            { 
                Type = ReferenceType.SecurityScheme, 
                Id = "Bearer" 
            }},
            Array.Empty<string>()
        }
    });
});
```

## Deployment & Infrastructure

### .NET Aspire Deployment

#### Local Development
- **Aspire Dashboard:** `https://localhost:15888` για monitoring
- **Service Orchestration:** Automatic service startup και configuration
- **Hot Reload:** Blazor Server hot reload με Aspire integration
- **Container Support:** Optional containerization για development

#### Production Deployment Options

**Option 1: Azure Container Apps**
```yaml
# Aspire deployment με Azure Container Apps
services:
  - name: membershub-web
    image: membershub-web:latest
    resources:
      cpu: 1.0
      memory: 2Gi
  - name: membershub-api  
    image: membershub-api:latest
    resources:
      cpu: 0.5
      memory: 1Gi
```

**Option 2: Kubernetes**
```yaml
# Kubernetes deployment με Aspire-generated manifests
apiVersion: apps/v1
kind: Deployment
metadata:
  name: membershub-web
spec:
  replicas: 2
  template:
    spec:
      containers:
      - name: web
        image: membershub-web:latest
```

**Option 3: Traditional IIS/Linux**
- **Self-contained deployment** με Aspire runtime
- **Reverse proxy:** Nginx ή IIS για load balancing
- **Service discovery:** File-based ή database configuration

### Hosting Requirements
- **.NET 9 Runtime** με Aspire hosting support
- **Database:** SQL Server 2022+ ή PostgreSQL 15+
- **Redis Cache:** Optional για session state και caching
- **SSL Certificate:** Required
- **Storage:** 100GB+ για αποδείξεις, logs, και telemetry data

### Configuration
- **Aspire Configuration:** Service discovery και orchestration settings
- **appsettings.json:** Environment-specific settings per service
- **Connection strings:** Database, Email, SMS με Aspire service binding
- **Feature flags:** Enable/disable features across services
- **Telemetry:** OpenTelemetry configuration για monitoring

### Aspire Development Workflow
```bash
# 1. Create Aspire solution structure
dotnet new aspire-starter -n MembersHub
cd MembersHub

# 2. Create shared libraries (bottom-up approach)
dotnet new classlib -n MembersHub.Core
dotnet new classlib -n MembersHub.Application  
dotnet new classlib -n MembersHub.Infrastructure
dotnet new classlib -n MembersHub.Shared

# 3. Create application projects
dotnet new blazorserver -n MembersHub.Web -au None
dotnet new webapi -n MembersHub.ApiService
dotnet new worker -n MembersHub.Worker

# 4. Add project references
# Core layer (no dependencies)
dotnet add MembersHub.Application reference MembersHub.Core
dotnet add MembersHub.Infrastructure reference MembersHub.Core
dotnet add MembersHub.Infrastructure reference MembersHub.Application

# Application projects reference all shared libraries
dotnet add MembersHub.Web reference MembersHub.Core
dotnet add MembersHub.Web reference MembersHub.Application  
dotnet add MembersHub.Web reference MembersHub.Infrastructure
dotnet add MembersHub.Web reference MembersHub.Shared

dotnet add MembersHub.ApiService reference MembersHub.Core
dotnet add MembersHub.ApiService reference MembersHub.Application
dotnet add MembersHub.ApiService reference MembersHub.Infrastructure  
dotnet add MembersHub.ApiService reference MembersHub.Shared

dotnet add MembersHub.Worker reference MembersHub.Core
dotnet add MembersHub.Worker reference MembersHub.Application
dotnet add MembersHub.Worker reference MembersHub.Infrastructure
dotnet add MembersHub.Worker reference MembersHub.Shared

# 5. Register projects σε Aspire AppHost
dotnet add MembersHub.AppHost reference MembersHub.Web
dotnet add MembersHub.AppHost reference MembersHub.ApiService
dotnet add MembersHub.AppHost reference MembersHub.Worker

# 6. Run με Aspire orchestration
dotnet run --project MembersHub.AppHost
```

### Dependency Injection Setup
```csharp
// MembersHub.Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<MembersHubContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            
        // Repositories
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        
        // External Services  
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ISmsService, SmsService>();
        
        return services;
    }
}

// MembersHub.Application/DependencyInjection.cs  
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Business Services
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IMemberService, MemberService>();
        
        // Validation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        return services;
    }
}

// Usage σε Web & ApiService Program.cs
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
```

## Testing Requirements

### Test Project Structure
```
MembersHub.Core.Tests           // Domain logic unit tests
MembersHub.Application.Tests    // Business logic unit tests  
MembersHub.Infrastructure.Tests // Repository & external service tests
MembersHub.Web.Tests           // Blazor component tests
MembersHub.ApiService.Tests    // API controller tests
MembersHub.IntegrationTests    // End-to-end με Aspire TestHost
```

### Unit Tests
- **Core Project Tests:** Domain entity validation, business rules
- **Application Service Tests:** Business logic με mocked dependencies
- **Infrastructure Tests:** Repository patterns με EF Core InMemory
- **Shared Logic:** Validation, utilities, extensions

```csharp
// Example: PaymentService unit test στο Application.Tests
public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _mockRepo;
    private readonly PaymentService _service;
    
    [Fact]
    public async Task CreatePayment_ValidData_ReturnsSuccess()
    {
        // Arrange - χρησιμοποιεί shared models από Core
        var dto = new CreatePaymentDto { ... };
        
        // Act - καλεί το shared service
        var result = await _service.CreatePaymentAsync(dto);
        
        // Assert
        result.Should().NotBeNull();
    }
}
```

### Integration Tests
- **Aspire Test Host:** Integration testing με πραγματική service orchestration
```csharp
// MembersHub.IntegrationTests
public class PaymentIntegrationTests : IClassFixture<AspireIntegrationTestFactory>
{
    [Fact]
    public async Task Web_And_Api_Use_Same_PaymentService()
    {
        // Test ότι το Web UI και το API επιστρέφουν τα ίδια δεδομένα
        var webClient = _factory.CreateClient("MembersHub.Web");
        var apiClient = _factory.CreateClient("MembersHub.ApiService");
        
        // Κάνε payment μέσω Web UI
        var webResult = await CreatePaymentThroughWeb(webClient);
        
        // Retrieve μέσω API  
        var apiResult = await GetPaymentThroughApi(apiClient, webResult.Id);
        
        // Verify consistency
        webResult.Amount.Should().Be(apiResult.Amount);
    }
}
```

- **Database Integration:** Shared ClubContext testing με test containers
- **Email/SMS services:** Mock providers για CI/CD, real providers για UAT
- **Cross-Service Communication:** Web → API → Worker integration flows
- **Authentication Flow:** JWT validation across Web & API projects

### Component Tests
- **Blazor Components:** Testing components που χρησιμοποιούν shared services
- **API Controllers:** Testing endpoints που χρησιμοποιούν shared business logic
- **Background Services:** Worker job testing με shared infrastructure

### Shared Test Utilities
```csharp
// MembersHub.Testing (shared test project)
public static class TestDataFactory
{
    public static Member CreateTestMember() => new Member { ... };
    public static Payment CreateTestPayment() => new Payment { ... };
}

public class TestMembersHubContextFactory
{
    public static MembersHubContext CreateInMemoryContext()
    {
        // Returns configured test database context
    }
}
```

### Aspire-Specific Testing
- **Service Orchestration:** Startup sequence με shared dependencies
- **Configuration Binding:** Shared configuration validation across services
- **Health Checks:** Shared infrastructure health monitoring
- **Telemetry Collection:** Cross-service tracing verification
- **Container Deployment:** Multi-service integration με shared volumes

## Documentation Deliverables

### Technical Documentation
- **API Documentation:** Swagger/OpenAPI
- **Database Schema:** ERD diagram
- **Deployment Guide:** Step-by-step setup
- **Configuration Manual:** All settings explained

### User Documentation
- **Admin Manual:** System administration
- **Collector Mobile Guide:** Step-by-step χρήση
- **Troubleshooting Guide:** Common issues και solutions

## Future Enhancements (Phase 2)

### Advanced Security (Phase 2)

#### Passkey Authentication (WebAuthn)
```csharp
// Modern passwordless authentication
public class User
{
    // Traditional fallback
    public string? PasswordHash { get; set; }
    
    // WebAuthn credentials
    public List<UserCredential> WebAuthnCredentials { get; set; } = new();
}

public class UserCredential
{
    public string CredentialId { get; set; } = string.Empty;
    public byte[] PublicKey { get; set; } = Array.Empty<byte>();
    public string DeviceName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastUsed { get; set; }
}
```

**Benefits:**
- 🔐 **Phishing resistant:** Critical για οικονομικά δεδομένα
- 📱 **Mobile integration:** Face ID/Touch ID support
- 🚀 **Better UX:** No passwords to remember
- 🔒 **Hardware security:** TPM/Secure Enclave backed

**Implementation Libraries:**
- FIDO2.NET ή WebAuthn.Net
- Browser compatibility: 95%+ modern browsers
- Fallback: Traditional password login

#### Advanced Threat Detection
- **Behavioral Analytics:** Unusual login patterns
- **Geo-location Tracking:** Login από νέες τοποθεσίες
- **Device Risk Scoring:** Unknown devices
- **Real-time Alerts:** Suspicious activity notifications

#### Hardware Security Keys
- **FIDO2/WebAuthn support:** YubiKey, etc.
- **Admin-only requirement:** High-privilege operations
- **Backup keys:** Multiple key support

### Member Portal
- **Self-service:** Members βλέπουν οφειλές και πληρώνουν online
- **Payment integration:** Credit card processing (Stripe/PayPal)
- **Document access:** Download παλαιές αποδείξεις
- **Mobile app:** Native iOS/Android με push notifications

### Advanced Analytics
- **Revenue forecasting:** Predictive analytics με ML.NET
- **Member retention:** Churn analysis και early warning
- **Performance metrics:** Collector efficiency analysis
- **Custom dashboards:** Role-based views με charts
- **Business Intelligence:** Power BI integration

### Integrations
- **Accounting software:** Export σε ΛΟΓΙΣΤΗ, SingularLogic, SAP
- **Bank integration:** Automatic reconciliation με Open Banking APIs
- **Government reporting:** ΦΠΑ, ΕΡΓΑΝΗ, TAXIS integration
- **CRM systems:** HubSpot, Salesforce integration
- **Marketing automation:** Email campaigns, member segmentation

### Enhanced Mobile Experience
- **Native mobile apps:** iOS & Android με Xamarin/MAUI
- **Advanced offline:** Extended offline capabilities
- **Push notifications:** Payment reminders, system alerts  
- **Voice commands:** "Record payment for John Doe €25"
- **Apple Pay/Google Pay:** Contactless payment acceptance

### AI & Machine Learning
- **Receipt OCR:** Automatic data extraction από φωτογραφίες
- **Fraud detection:** Unusual payment patterns
- **Chatbot support:** Member service automation
- **Predictive insights:** Member behavior analysis
- **Smart reminders:** Optimal timing για payment reminders

### Advanced Reporting
- **Custom report builder:** Drag-and-drop interface
- **Scheduled reports:** Automatic email delivery
- **Real-time dashboards:** Live updates με SignalR
- **Data visualization:** Advanced charts με D3.js
- **Export options:** PDF, Excel, CSV με custom formatting

### Multi-tenant Architecture
- **SaaS offering:** Multiple clubs σε ένα instance
- **White-label solution:** Custom branding per club
- **Subscription billing:** Monthly/yearly plans
- **Feature toggles:** Different tiers με different features

### Advanced Notifications
- **Multi-channel:** Email, SMS, Push, WhatsApp
- **Template engine:** Custom templates per club
- **A/B testing:** Optimize notification effectiveness
- **Delivery optimization:** Best time to send analysis

---

## Επόμενα Βήματα

### 1. Aspire Project Setup
```bash
# Install Aspire workload
dotnet workload install aspire

# Create Aspire solution
dotnet new aspire-starter -n MembersHub
```

### 2. Development Milestones
1. **Aspire Infrastructure & Shared Libraries Setup:** Service orchestration, Clean Architecture foundation
2. **Core Domain Models:** Entities, interfaces στο Core project
3. **Database & Repository Layer:** EF Core context στο Infrastructure project  
4. **Business Logic Layer:** Services στο Application project
5. **Authentication & User Management:** JWT integration across Web & API
6. **Web UI - Member Management:** Blazor pages με MudBlazor components
7. **API Endpoints:** RESTful controllers που χρησιμοποιούν τα ίδια services
8. **Background Services:** Email/SMS worker που χρησιμοποιεί shared infrastructure
9. **Receipt Generation:** PDF service shared μεταξύ Web & API
10. **Mobile PWA & Offline:** Service worker με API integration
11. **Observability & Monitoring:** Telemetry across all services
12. **Testing Strategy:** Unit tests για shared libraries, integration tests για services
13. **Deployment & DevOps:** Container orchestration με shared dependencies

### Benefits του Shared Libraries Approach
✅ **Single Source of Truth:** Models και entities σε ένα μέρος
✅ **Code Reusability:** Business logic shared μεταξύ Web & API  
✅ **Consistency:** Ίδια validation rules παντού
✅ **Maintainability:** Αλλαγές στα models γίνονται μια φορά
✅ **Testing:** Unit tests για shared logic μια φορά
✅ **Deployment:** Shared libraries ως NuGet packages αν χρειαστεί

### Practical Examples για MembersHub

#### 1. Payment Processing
```csharp
// MembersHub.Application/Services/PaymentService.cs
public class PaymentService : IPaymentService
{
    public async Task<PaymentResult> ProcessPaymentAsync(CreatePaymentDto dto)
    {
        // Business logic shared μεταξύ Web UI και Mobile API
        var member = await _memberRepo.GetByIdAsync(dto.MemberId);
        var payment = new Payment { ... };
        
        // Email/SMS logic εδώ
        await _notificationService.SendReceiptAsync(payment);
        
        return new PaymentResult { Success = true, Payment = payment };
    }
}

// Used by Web (Blazor page)
// MembersHub.Web/Pages/Payments.razor.cs
public async Task OnPaymentSubmit()
{
    var result = await _paymentService.ProcessPaymentAsync(createDto);
    // Handle result
}

// Used by API (Controller)  
// MembersHub.ApiService/Controllers/PaymentsController.cs
[HttpPost]
public async Task<IActionResult> CreatePayment(CreatePaymentDto dto)
{
    var result = await _paymentService.ProcessPaymentAsync(dto);
    return Ok(result);
}
```

#### 2. Member Management
```csharp
// MembersHub.Core/Entities/Member.cs - shared model
public class Member
{
    public int Id { get; set; }
    public string FullName => $"{FirstName} {LastName}";
    public decimal OutstandingBalance => Subscriptions
        .Where(s => s.Status == SubscriptionStatus.Pending)
        .Sum(s => s.Amount);
}

// Used by Web για UI display
// MembersHub.Web/Components/MemberCard.razor
<MudCard>
    <h3>@Member.FullName</h3>
    <p>Οφειλή: €@Member.OutstandingBalance</p>
</MudCard>

// Used by API για mobile response
// MembersHub.ApiService/Controllers/MembersController.cs
[HttpGet("{id}")]
public async Task<MemberDto> GetMember(int id)
{
    var member = await _memberService.GetByIdAsync(id);
    return new MemberDto 
    {
        FullName = member.FullName,
        OutstandingBalance = member.OutstandingBalance
    };
}
```

#### 3. Receipt Generation
```csharp
// MembersHub.Application/Services/ReceiptService.cs
public class ReceiptService : IReceiptService
{
    public async Task<byte[]> GenerateReceiptPdfAsync(Payment payment)
    {
        // PDF generation logic shared everywhere
        // Used by Web για preview, API για mobile, Worker για email
    }
    
    public async Task SendReceiptAsync(Payment payment)
    {
        var pdf = await GenerateReceiptPdfAsync(payment);
        
        // Email/SMS logic shared
        if (!string.IsNullOrEmpty(payment.Member.Email))
            await _emailService.SendReceiptAsync(payment.Member.Email, pdf);
        else
            await _smsService.SendReceiptNotificationAsync(payment.Member.Phone);
    }
}
```

### Configuration Management με Shared Libraries

#### Shared Configuration Options
```csharp
// MembersHub.Core/Configuration/ClubSettings.cs
public class ClubSettings
{
    public const string SectionName = "ClubSettings";
    
    public string ClubName { get; set; } = string.Empty;
    public string ClubAddress { get; set; } = string.Empty;  
    public string ClubPhone { get; set; } = string.Empty;
    public string ClubEmail { get; set; } = string.Empty;
    public string ClubVAT { get; set; } = string.Empty;
    public string LogoPath { get; set; } = string.Empty;
}

// MembersHub.Core/Configuration/EmailSettings.cs
public class EmailSettings
{
    public const string SectionName = "EmailSettings";
    
    public string Provider { get; set; } = "SMTP"; // SMTP, SendGrid, Mailgun
    public SmtpSettings Smtp { get; set; } = new();
    public SendGridSettings SendGrid { get; set; } = new();
    public bool EnableFallbackSMS { get; set; } = true;
    public int RetryAttempts { get; set; } = 3;
}
```

#### Aspire Configuration Binding
```csharp
// MembersHub.ServiceDefaults/Extensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedConfiguration(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Bind shared configuration options
        services.Configure<ClubSettings>(
            configuration.GetSection(ClubSettings.SectionName));
            
        services.Configure<EmailSettings>(
            configuration.GetSection(EmailSettings.SectionName));
            
        services.Configure<SmsSettings>(
            configuration.GetSection(SmsSettings.SectionName));
            
        return services;
    }
}

// Used σε όλα τα projects (Web, API, Worker)
// Program.cs
builder.Services.AddSharedConfiguration(builder.Configuration);
```

#### appsettings.json (shared across all services)
```json
{
  "ClubSettings": {
    "ClubName": "Αθλητικό Σωματείο Νίκη",
    "ClubAddress": "Λεωφόρος Αθηνών 123, Αθήνα",
    "ClubPhone": "210-1234567",
    "ClubEmail": "info@nikiathletic.gr",
    "ClubVAT": "123456789",
    "LogoPath": "assets/logo.png"
  },
  "EmailSettings": {
    "Provider": "SMTP",
    "EnableFallbackSMS": true,
    "RetryAttempts": 3,
    "Smtp": {
      "Host": "smtp.gmail.com",
      "Port": 587,
      "Username": "club@nikiathletic.gr",
      "Password": "app-password",
      "EnableSsl": true
    }
  }
}
```

#### Usage στα Services
```csharp
// MembersHub.Infrastructure/Services/EmailService.cs
public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ClubSettings _clubSettings;
    
    public EmailService(
        IOptions<EmailSettings> emailOptions,
        IOptions<ClubSettings> clubOptions)
    {
        _emailSettings = emailOptions.Value;
        _clubSettings = clubOptions.Value;
    }
    
    public async Task SendReceiptAsync(string email, byte[] pdf)
    {
        var subject = $"Απόδειξη Εισπραξης - {_clubSettings.ClubName}";
        // Use shared configuration για email sending
    }
}
```

### 3. Technical Documentation
- **Aspire Architecture Diagram:** Service dependencies και communication
- **API Documentation:** Swagger/OpenAPI με service discovery
- **Database Schema:** ERD με migration scripts
- **Deployment Guide:** Aspire hosting options (Azure, K8s, Docker)
- **Monitoring Setup:** Telemetry configuration και alerting rules

### 4. Development Environment
- **Prerequisites:** .NET 9 SDK, Docker Desktop, Aspire workload
- **IDE:** Visual Studio 2022 17.8+ ή VS Code με Aspire extension
- **Local Services:** SQL Server LocalDB, Redis (optional)
- **Aspire Dashboard:** Development monitoring και debugging

**Προτεινόμενη Σειρά Ανάπτυξης με Aspire:**
1. **Aspire AppHost Setup:** Service orchestration foundation
2. **Shared Libraries:** Core, Application, Infrastructure, Shared projects
3. **Blazor Web Project:** UI με MudBlazor integration  
4. **API Service:** RESTful endpoints με service discovery
5. **Background Worker:** Email/SMS queue processing
6. **Database Integration:** EF Core με health checks
7. **Authentication & Security:** JWT, rate limiting, validation
8. **Mobile PWA:** Offline capabilities και sync
9. **Receipt Generation:** PDF creation και delivery system
10. **Observability:** Custom metrics και monitoring
11. **Container Deployment:** Production-ready containerization
12. **Performance Testing:** Load testing με telemetry analysis