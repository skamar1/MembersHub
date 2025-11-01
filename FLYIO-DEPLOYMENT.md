# MembersHub - Fly.io Deployment Guide

## Προετοιμασία

### 1. Εγκατάσταση flyctl (Ολοκληρώθηκε)
```bash
brew install flyctl
flyctl version
```

### 2. Login στο Fly.io (Ολοκληρώθηκε)
```bash
flyctl auth login
```

---

## Deployment Steps

### Βήμα 1: Deploy PostgreSQL Database

```bash
# Δημιουργία PostgreSQL cluster
flyctl postgres create \
  --name membershub-db \
  --region ams \
  --vm-size shared-cpu-1x \
  --initial-cluster-size 1 \
  --volume-size 10

# Σημείωσε το connection string που θα εμφανιστεί
```

**Κόστος:** ~€5-8/μήνα

---

### Βήμα 2: Deploy Upstash Redis

```bash
# Δημιουργία Upstash Redis
flyctl redis create --name membershub-redis

# Το REDIS_URL θα δημιουργηθεί αυτόματα
```

**Κόστος:** ΔΩΡΕΑΝ (free tier: 100MB, 10K commands/day)

---

### Βήμα 3: Configure Secrets

Πρώτα, πάρε τα connection strings από τα παραπάνω βήματα.

```bash
# Set PostgreSQL connection string για το API Service
flyctl secrets set -a membershub-api \
  CONNECTION_STRING="Host=membershub-db.internal;Database=membersdb;Username=postgres;Password=YOUR_PASSWORD"

# Set PostgreSQL connection string για το Web App
flyctl secrets set -a membershub-web \
  CONNECTION_STRING="Host=membershub-db.internal;Database=membersdb;Username=postgres;Password=YOUR_PASSWORD"

# Set SMTP credentials (αν χρειάζεται)
flyctl secrets set -a membershub-web \
  SMTP_HOST="smtp.gmail.com" \
  SMTP_PORT="587" \
  SMTP_USERNAME="your-email@gmail.com" \
  SMTP_PASSWORD="your-app-password"

# Set JWT Secret (δημιούργησε ένα random string)
flyctl secrets set -a membershub-api \
  JWT_SECRET="your-super-secret-jwt-key-min-32-characters-long"
```

---

### Βήμα 4: Deploy API Service

```bash
cd MembersHub.ApiService

# Launch (θα δημιουργήσει το app)
flyctl launch --no-deploy

# Όταν σε ρωτήσει:
# - App name: membershub-api
# - Region: ams (Amsterdam)
# - Would you like to set up a Postgresql database? NO (έχουμε ήδη)
# - Would you like to set up an Upstash Redis database? NO (έχουμε ήδη)

# Deploy
flyctl deploy

# Verify deployment
flyctl status -a membershub-api
flyctl logs -a membershub-api
```

---

### Βήμα 5: Deploy Web App

```bash
cd ../MembersHub.Web

# Launch
flyctl launch --no-deploy

# Όταν σε ρωτήσει:
# - App name: membershub-web
# - Region: ams (Amsterdam)
# - Would you like to set up a Postgresql database? NO
# - Would you like to set up an Upstash Redis database? NO

# Deploy
flyctl deploy

# Verify deployment
flyctl status -a membershub-web
flyctl logs -a membershub-web
```

---

### Βήμα 6: Configure Private Networking

Τα apps πρέπει να μιλούν μεταξύ τους μέσω private network:

```bash
# Το Web app πρέπει να καλεί το API μέσω internal URL
flyctl secrets set -a membershub-web \
  API_URL="http://membershub-api.internal:8080"
```

---

## Verification

### Έλεγχος Deployments

```bash
# Check Web App
flyctl open -a membershub-web

# Check API Service
flyctl open -a membershub-api

# View logs
flyctl logs -a membershub-web
flyctl logs -a membershub-api

# Check database
flyctl postgres connect -a membershub-db
```

### Health Checks

Επισκέψου τα health endpoints:
- Web: `https://membershub-web.fly.dev/health`
- API: `https://membershub-api.fly.dev/health`

---

## Monitoring & Management

### View Costs

```bash
# Dashboard
flyctl dashboard

# Ή στο web: https://fly.io/dashboard
```

### Scaling

```bash
# Scale Web App (αν χρειαστεί)
flyctl scale memory 1024 -a membershub-web

# Scale API Service
flyctl scale memory 1024 -a membershub-api

# Horizontal scaling (Web app - Blazor Server needs sticky sessions)
flyctl scale count 2 --max-per-region 1 -a membershub-web
```

### Database Backup

```bash
# List backups
flyctl postgres backup list -a membershub-db

# Create manual backup
flyctl postgres backup create -a membershub-db
```

---

## Troubleshooting

### Logs

```bash
# Live tail logs
flyctl logs -a membershub-web
flyctl logs -a membershub-api

# SSH into machine
flyctl ssh console -a membershub-web
```

### Database Issues

```bash
# Connect to database
flyctl postgres connect -a membershub-db

# Check tables
\dt

# Check migrations
SELECT * FROM "__EFMigrationsHistory";
```

### Redis Issues

```bash
# Check Redis connection
flyctl redis proxy -a membershub-redis

# In another terminal
redis-cli -h localhost -p 6379
```

---

## Cost Optimization

### Development/Test (€3-5/μήνα)
- Web (512MB): ΔΩΡΕΑΝ (free tier)
- API (512MB, scale-to-zero): ΔΩΡΕΑΝ
- PostgreSQL (10GB): €1.50
- Redis: ΔΩΡΕΑΝ

### Production (€15-20/μήνα)
- Web (512MB, always-on): €3.88
- API (512MB): €3.88
- PostgreSQL managed: €5-8
- Redis pay-as-you-go: €2-3

### Tips
- Enable `auto_stop_machines` για dev environments
- Use scale-to-zero για API Service αν δεν χρειάζεται 24/7
- Monitor bandwidth (160GB δωρεάν)

---

## Custom Domain (Optional)

```bash
# Add custom domain
flyctl certs create membershub.gr -a membershub-web

# Update DNS CNAME record
# CNAME: membershub-web.fly.dev
```

---

## CI/CD με GitHub Actions (Optional)

Δημιούργησε `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Fly.io

on:
  push:
    branches: [main]

jobs:
  deploy-api:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: superfly/flyctl-actions/setup-flyctl@master
      - run: flyctl deploy --remote-only -a membershub-api
        env:
          FLY_API_TOKEN: ${{ secrets.FLY_API_TOKEN }}
        working-directory: ./MembersHub.ApiService

  deploy-web:
    runs-on: ubuntu-latest
    needs: deploy-api
    steps:
      - uses: actions/checkout@v4
      - uses: superfly/flyctl-actions/setup-flyctl@master
      - run: flyctl deploy --remote-only -a membershub-web
        env:
          FLY_API_TOKEN: ${{ secrets.FLY_API_TOKEN }}
        working-directory: ./MembersHub.Web
```

---

## Useful Commands

```bash
# Restart app
flyctl apps restart membershub-web

# Destroy app (careful!)
flyctl apps destroy membershub-web

# SSH into machine
flyctl ssh console -a membershub-web

# Check machine status
flyctl machine status -a membershub-web

# View app info
flyctl info -a membershub-web

# List all apps
flyctl apps list
```

---

## Support

- **Fly.io Docs:** https://fly.io/docs
- **Community Forum:** https://community.fly.io
- **Status Page:** https://status.fly.io

---

## Notes

- **Blazor Server:** Χρειάζεται sticky sessions (ήδη configured στο fly.toml)
- **Database Migrations:** Εκτελούνται αυτόματα στο startup του Web app
- **Environment Variables:** Χρησιμοποιήστε `flyctl secrets` για sensitive data
- **Region:** Χρησιμοποιούμε `ams` (Amsterdam) γιατί είναι πιο κοντά στην Ελλάδα
- **Auto-scaling:** Το API Service μπορεί να κάνει scale-to-zero για cost savings

---

**Last Updated:** 2025-01-11
**Author:** Claude Code & Aris Kamagakis
