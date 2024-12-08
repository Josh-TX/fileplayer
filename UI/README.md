# local debugging

When you use `npm run dev`, it creates a local server that auto rebuilds and refreshes on code change. This also uses the .env.development file, causing the base URL for API calls to point to the dotnet server's localhost port.

When you use `npm run build`, it outputs to `../Server/wwwroot`, which is where the dotnet server serves files from.

The former has the advantage of auto-rebuild & refresh, but the latter is more true to how it actually works once deployed. 
