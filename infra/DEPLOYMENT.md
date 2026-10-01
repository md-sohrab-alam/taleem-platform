# Deployment

The Taleem API is a standard Docker image. It is **not** coupled to Railway, AWS, Azure, or GCP.

## Environment variables

| Variable | Purpose |
|----------|---------|
| `ConnectionStrings__Default` or `DATABASE_URL` | PostgreSQL connection (pgvector required) |
| `AdminAuth__Username` / `AdminAuth__Password` | Dev admin credentials (replace with real IdP later) |
| `AdminAuth__JwtSigningKey` | JWT signing secret |
| `EMBEDDING_PROVIDER` | `fake` or `openai` |
| `EMBEDDING_API_KEY` | Required for openai |
| `CORS_ORIGINS` | Comma-separated browser origins |
| `Storage__RootPath` | Writable volume for uploaded PDFs |
| `ASPNETCORE_ENVIRONMENT` | `Development` / `Production` |

## Railway (simple initial host)

1. Create a PostgreSQL addon that supports extensions; enable `vector`.
2. Deploy from the `backend/Dockerfile`.
3. Set env vars above.
4. Attach a persistent volume for `/app/storage` (or later swap to S3-compatible storage behind `IObjectStorage`).
5. Health checks: `/health` and `/health/ready`.

## Moving elsewhere later

Push the same image to any registry and run on Fly.io, Azure Container Apps, ECS, Cloud Run, or Kubernetes with the same env vars and a Postgres+pgvector instance.
