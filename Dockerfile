# Stage 1: Build the menu webview (React/Vite)
FROM node:22-slim AS menu-build
WORKDIR /src/menu
COPY menu/package.json menu/package-lock.json* ./
RUN npm install
COPY menu/ ./
RUN npm run build

# Stage 2: Build the port webview (React/Vite)
FROM node:22-slim AS webview-build
WORKDIR /src/webview
COPY webview/package.json webview/package-lock.json* ./
RUN npm install
COPY webview/ ./
RUN npm run build -- --base=/fragments/webview/

# Stage 3: Build the admin panel (React/Vite)
FROM node:22-slim AS admin-build
WORKDIR /src/admin
COPY admin/package.json admin/package-lock.json* ./
RUN npm install
COPY admin/ ./
RUN npm run build

# Stage 4: Build the .NET API server
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS api-build
WORKDIR /src
COPY api/ ./api/
RUN dotnet publish api --configuration Release --output /app/publish

# Stage 5: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=api-build /app/publish ./
COPY --from=menu-build /src/api/fragments/menu/ ./fragments/menu/
COPY --from=webview-build /src/api/fragments/webview/ ./fragments/webview/
COPY --from=admin-build /src/api/wwwroot/admin/ ./wwwroot/admin/

# ASP.NET listens on 8080 by default in .NET 8+
EXPOSE 8080

ENTRYPOINT ["dotnet", "api.dll"]
