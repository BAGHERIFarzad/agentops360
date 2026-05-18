# AgentOps 360 — Deployment Guide

## Recommended Deployment

Frontend:

- Vercel
- or Vultr / Coolify

Backend:

- Vultr VM
- ASP.NET Core Web API

Database:

- SQL Server
- or PostgreSQL for production deployment

---

## Environment Variables

Do not commit API keys.

Use environment variables:

```txt
Gemini__ApiKey
Gemini__Model
Speechmatics__ApiKey
Speechmatics__BaseUrl
ConnectionStrings__DefaultConnection
```

---

## Backend Deployment Checklist

1. Publish backend:

```bash
dotnet publish -c Release
```

2. Configure environment variables.

3. Configure database connection string.

4. Run migrations:

```bash
dotnet ef database update
```

5. Start backend service.

6. Verify Swagger or health endpoint.

---

## Frontend Deployment Checklist

1. Update API URL for production.

2. Build frontend:

```bash
npm run build
```

3. Deploy generated `dist` folder.

4. Verify API calls from browser.

---

## Security Checklist

Before pushing to GitHub:

- Remove API keys from `appsettings.json`
- Regenerate exposed keys
- Add `.env` files to `.gitignore`
- Do not commit database files
- Do not commit `bin` or `obj`
- Do not commit `node_modules`
