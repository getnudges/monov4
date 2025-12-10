# Graph Monitor

GraphMonitor is a simple HTTP-based cache service that stores and retrieves GraphQL endpoint URLs and schemas. It acts as a registry for GraphQL information, allowing services to look up endpoints without direct knowledge of them.

## Endpoints

| Method | Path | Purpose | Auth |
|--------|------|---------|------|
| GET | `/health` | Health check | None |
| GET | `/{name}` | Retrieve stored graph URL | X-Api-Key |
| POST | `/{name}` | Store a graph URL | X-Api-Key |
| GET | `/schema` | Retrieve stored GraphQL schema | X-Api-Key |
| POST | `/schema` | Store GraphQL schema | X-Api-Key |

## Usage

### Store a Graph URL

```bash
curl -X POST http://localhost:5145/my-api \
  -H "X-Api-Key: your-key" \
  -H "Content-Type: text/plain" \
  -d "http://api.example.com/graphql"
```

### Retrieve a Graph URL

```bash
curl http://localhost:5145/my-api \
  -H "X-Api-Key: your-key"
```

### Health Check

```bash
curl http://localhost:5145/health
```

## Running

### Local Development

```powershell
cd dotnet/GraphMonitor
$env:REDIS_URL="localhost:6379"
$env:MONITOR_API_KEY="dev-key"
dotnet run
```

Default port: `5145`

### Docker

```bash
docker build -f dotnet/GraphMonitor.Dockerfile -t graph-monitor .
docker run -e REDIS_URL=redis:6379 -e MONITOR_API_KEY=secret -p 5145:5145 graph-monitor
```

## Configuration

Required environment variables:

```ini
REDIS_URL=localhost:6379
MONITOR_API_KEY=your-secret-key
```

## Notes

- All endpoints except `/health` require the `X-Api-Key` header
- Data is stored in Redis with keys like `graph:{name}` and `graph:schema`
- Built with AOT compilation for fast startup and small image size
