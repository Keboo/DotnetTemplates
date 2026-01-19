# Quick Start Guide - React Migration

## Prerequisites
- Node.js 18+ and npm
- .NET 10 SDK
- SQL Server or SQLite

## Development Setup

### Option 1: Run with Aspire AppHost (Recommended)

The easiest way to run the application is using the Aspire AppHost, which orchestrates both the backend API and the React frontend automatically.

```powershell
# Navigate to the AppHost project
cd ReactApp.AppHost

# Run the Aspire AppHost
dotnet run
```

This will:
- Start the SQL Server container (or use Azure SQL if in publish mode)
- Start the backend API (ReactApp)
- Start the Vite dev server for the React frontend
- Open the Aspire dashboard where you can monitor all services

**Access points:**
- **Aspire Dashboard**: Displayed in terminal output (typically http://localhost:15888)
- **Frontend**: http://localhost:5173 (React + Vite with HMR)
- **Backend API**: https://localhost:5000
- **Swagger**: https://localhost:5000/swagger

### Option 2: Run Manually (Advanced)

If you prefer to run services individually:

### 1. Restore Dependencies

```powershell
# Restore .NET packages
dotnet restore

# Install npm packages for React frontend
cd ReactApp\ReactApp.Web
npm install
cd ..\..
```

### 2. Run in Development Mode

**Terminal 1 - Backend (ASP.NET Core):**
```powershell
cd ReactApp\ReactApp
dotnet run
```
This starts the backend API at `https://localhost:5000`

**Terminal 2 - Frontend (React + Vite):**
```powershell
cd ReactApp\ReactApp.Web
npm run dev
```
This starts Vite dev server at `http://localhost:5173` with HMR (Hot Module Replacement)

### 3. Access the Application

**With Aspire:**
- Check the Aspire dashboard for all service URLs
- Frontend is usually at http://localhost:5173
- All services have health checks and telemetry

**Without Aspire:**
Open your browser to: **http://localhost:5173**

The Vite dev server automatically proxies API calls (`/api/*` and `/hubs/*`) to the ASP.NET Core backend.

## Aspire Dashboard Features

When running with Aspire, you get:

```powershell
cd ReactApp\ReactApp
dotnet publish -c Release -o ./publish
```

This will:
1. Build the ASP.NET Core backend
2. Run `npm install` in ReactApp.Web
3. Run `npm run build` to create optimized React production build
4. Copy React build output to `wwwroot/` in the publish folder

The published application can be deployed to Azure App Service, IIS, or any ASP.NET Core hosting environment.

## Project Structure Overview

```
ReactApp.Web/               # React Frontend
├── src/
│   ├── components/          # Layout, ProtectedRoute
│   ├── contexts/            # AuthContext, ThemeContext
│   ├── hooks/               # useRoomHub
│   ├── pages/               # Home, Login, Register, Room, RoomManage, MyRooms, QuestionDisplay
│   ├── services/            # apiClient, roomHub
│   └── types/               # TypeScript definitions
├── package.json
├── vite.config.ts
└── tsconfig.json

ReactApp/                   # ASP.NET Core Backend
├── Controllers/             # AuthController, RoomsController
├── Program.cs               # App configuration, middleware
└── ReactApp.csproj
```

## Key Technologies

- **Frontend**: React 18, TypeScript, Material-UI, Vite, SignalR JS Client
- **Backend**: ASP.NET Core 10, Identity, SignalR, Entity Framework Core
- **Build**: MSBuild integration via wrapper .csproj

## Common Commands

**Aspire:**
```powershell
# Run everything with Aspire
cd ReactApp.AppHost
dotnet run

# Publish Aspire manifest for deployment
dotnet publish
```

**React Frontend:**
```powershell
# Run linting
cd ReactApp\ReactApp.Web
npm run lint

# Build React for production
npm run build

# Preview production build
npm run preview

# Run backend tests
cd ..\..
dotnet test

# Build entire solution
dotnet build
```

## API Documentation

When running in development, Swagger UI is available at:
**https://localhost:5000/swagger**

## Troubleshooting

**Problem**: Aspire dashboard won't open  
**Solution**: Check terminal output for the dashboard URL, typically http://localhost:15888

**Problem**: Frontend service fails to start in Aspire  
**Solution**: Ensure Node.js is installed and `npm install` has been run in `ReactApp/ReactApp.Web`

**Problem**: npm packages not found  
**Solution**: Run `npm install` in `ReactApp\ReactApp.Web`

**Problem**: Backend API not responding  
**Solution**: Ensure `dotnet run` is running and listening on port 5000, or check Aspire dashboard

**Problem**: SignalR connection fails  
**Solution**: Check that both frontend (5173) and backend (5000) are running

**Problem**: TypeScript errors  
**Solution**: npm packages will auto-install during first `dotnet build`. If issues persist, manually run `npm install`

## Next Steps

- Update UI tests in `ReactApp.UITests` to work with React DOM
- Configure email service for Identity (currently using no-op sender)
- Add environment-specific configuration
- Set up CI/CD pipeline for automated builds

For detailed migration information, see [MIGRATION.md](MIGRATION.md)
