# Stage 1: Build the webview (React/Vite)
FROM node:22-slim AS webview-build
WORKDIR /src/webview
COPY webview/package.json webview/package-lock.json* ./
RUN npm ci
COPY webview/ ./
RUN npm run build

# Stage 2: Build the .NET API server
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS api-build
WORKDIR /src
COPY api/ ./api/
RUN dotnet publish api --configuration Release --output /app/publish

# Stage 3: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=api-build /app/publish ./
COPY --from=webview-build /src/api/fragments/ ./fragments/

# ASP.NET listens on 8080 by default in .NET 8+
EXPOSE 8080

ENTRYPOINT ["dotnet", "api.dll"]
