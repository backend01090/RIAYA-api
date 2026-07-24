# Deployment Guide

## Local Docker Verification

```powershell
copy .env.example .env
docker compose up --build -d
docker compose ps
Invoke-WebRequest -UseBasicParsing http://localhost:8080/health
```

The API is available at:

```text
http://localhost:8080
http://localhost:8080/swagger
```

Stop the local stack when finished:

```powershell
docker compose down
```

## Production Notes

- Set `ASPNETCORE_ENVIRONMENT=Production`.
- Provide `ConnectionStrings__DefaultConnection` from the deployment secret store.
- Provide a strong `Jwt__Key` with at least 32 characters.
- Keep `Jwt__RefreshTokenDurationInDays` short enough for the risk profile of the deployment.
- Apply migrations explicitly in CI/CD or by an operator command before starting the app.
- Do not enable demo seed data in production.
- Put the API behind HTTPS and a reverse proxy or cloud gateway.
- Keep `/health` available to the orchestrator.

## Database Migration Command

```powershell
dotnet ef database update --project ".\Riaya.Api\Riaya.Api.csproj" --startup-project ".\Riaya.Api\Riaya.Api.csproj"
```

