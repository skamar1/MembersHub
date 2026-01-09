# MembersHub

Σύστημα διαχείρισης συνδρομών, εισπράξεων και δαπανών για αθλητικά σωματεία.

## 🚀 Tech Stack

- **.NET 9** - Framework
- **Blazor Server** - Frontend με **MudBlazor** UI components
- **Entity Framework Core 9** - ORM
- **PostgreSQL** - Database
- **Fly.io** - Production hosting
- **JWT Authentication** - Ασφάλεια

## 📦 Project Structure

```
MembersHub/
├── MembersHub.Core/           # Domain entities & interfaces
├── MembersHub.Application/    # Business logic & services
├── MembersHub.Infrastructure/ # Database & external services
├── MembersHub.Shared/         # Shared utilities
├── MembersHub.Web/            # Blazor Server UI
├── MembersHub.ApiService/     # REST API (future mobile support)
├── MembersHub.ServiceDefaults/# Shared configuration
└── MembersHub.AppHost/        # .NET Aspire orchestration
```

## 🏃 Getting Started

### Prerequisites

- .NET 9 SDK
- PostgreSQL 15+
- Docker Desktop (optional για local PostgreSQL)

### Local Development

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd MembersHub
   ```

2. **Setup Database**
   ```bash
   # Αν χρησιμοποιείς Docker
   docker run --name membershub-postgres -e POSTGRES_PASSWORD=yourpassword -p 5432:5432 -d postgres:15

   # Ή εγκατέστησε PostgreSQL τοπικά
   ```

3. **Update Connection String**
   Στο `MembersHub.Web/appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=MembersHubDB;Username=postgres;Password=yourpassword"
     }
   }
   ```

4. **Run Migrations**
   ```bash
   cd MembersHub.Web
   dotnet ef database update
   ```

5. **Run the application**
   ```bash
   dotnet run --project MembersHub.Web
   ```

6. **Access the application**
   - Blazor Web: `https://localhost:7001`
   - Default admin credentials will be generated on first run (check console logs)

## 🌐 Deployment

### Production Environment (Fly.io)

**Application:** `melas`
**URL:** https://membershub.gr
**Database:** PostgreSQL cluster στο Fly.io

#### Deploy to Production

```bash
# Deploy to melas (production)
fly deploy -c fly.melas.toml --now
```

### Development/Testing Environment

**Application:** `membershub-web`
**URL:** https://membershub-web.fly.dev

```bash
# Deploy to development
fly deploy --now
```

## 🔧 Configuration

### Fly.io Secrets

```bash
# Database connection (already configured)
fly secrets list -a melas

# Set new secret
fly secrets set KEY=value -a melas
```

### Environment Variables

- `DATABASE_URL` - PostgreSQL connection string
- `ASPNETCORE_ENVIRONMENT` - Production/Development
- `JWT_SECRET` - JWT signing key (auto-generated)

## 👥 User Roles

1. **Administrator** - Πλήρης πρόσβαση
2. **Secretary** - Διαχείριση μελών & συνδρομών
3. **Treasurer** - Οικονομική διαχείριση
4. **Cashier** - Εισπράξεις & πληρωμές
5. **Collector** - Mobile εισπράξεις (future)

## 🗄️ Database Migrations

### Create Migration

```bash
cd MembersHub.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../MembersHub.Web
```

### Apply Migration

```bash
# Local
cd MembersHub.Web
dotnet ef database update

# Production (Fly.io)
fly ssh console -a melas
cd /app
./MembersHub.Web --migrate
```

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test MembersHub.Application.Tests
```

## 📝 Recent Updates

### Latest Changes (January 2026)

- ✅ **Subscription Generation Fix** - Τώρα δημιουργούνται σωστά οι συνδρομές μόνο για μέλη που δεν έχουν
- ✅ **Greek Localization** - MudDataGrid pagination μεταφρασμένο στα ελληνικά
- ✅ **Phone Input Fix** - Αφαιρέθηκε το mask που προκαλούσε cursor jumping
- ✅ **Date Format Standardization** - Όλες οι ημερομηνίες σε dd/MM/yyyy format
- ✅ **Security Improvements** - Enhanced audit logging & IP tracking
- ✅ **Role-based Expense Filtering** - Cashiers βλέπουν μόνο δικά τους expenses

### Previous Updates

- ✅ Department-based filtering για subscriptions & payments
- ✅ Security dashboard με audit logs
- ✅ Account lockout protection
- ✅ Real-time client IP detection
- ✅ Enhanced authentication flow

## 🔐 Security Features

- JWT-based authentication
- Role-based authorization (RBAC)
- Account lockout after failed login attempts
- Audit logging για όλες τις ενέργειες
- IP tracking & security monitoring
- Device management
- Real-time security notifications

## 📚 Documentation

- **[SPECIFICATIONS.md](./SPECIFICATIONS.md)** - Αναλυτικές προδιαγραφές & αρχιτεκτονική
- **[Entity Relationships](./docs/ERD.md)** - Database schema (if exists)
- **API Documentation** - Available at `/swagger` in development mode

## 🤝 Contributing

1. Create a feature branch from `main`
2. Make your changes
3. Test locally
4. Deploy to `membershub-web` (development) για testing
5. Create a pull request
6. Μετά την έγκριση, deploy στο `melas` (production)

## 🐛 Troubleshooting

### Common Issues

**Connection refused to PostgreSQL**
```bash
# Check if PostgreSQL is running
docker ps | grep postgres

# Restart PostgreSQL container
docker restart membershub-postgres
```

**Migration errors**
```bash
# Reset database (development only!)
dotnet ef database drop --force
dotnet ef database update
```

**Fly.io deployment issues**
```bash
# Check app status
fly status -a melas

# View logs
fly logs -a melas

# Restart app
fly apps restart melas
```

## 📞 Support

Για ερωτήσεις ή προβλήματα, επικοινωνήστε με την ομάδα ανάπτυξης.

## 📄 License

[Specify license here]

---

**Production URL:** https://membershub.gr
**Development URL:** https://membershub-web.fly.dev
**Fly.io Dashboard:** https://fly.io/dashboard
