# Blazor to React Migration

This project has been migrated from Blazor WebAssembly to React with TypeScript and Material-UI.

## Architecture

### Frontend (BlazorApp.Web)
- **Framework**: React 18 with TypeScript
- **UI Library**: Material-UI (MUI) v6
- **Build Tool**: Vite 6
- **Routing**: React Router v7
- **Real-time**: SignalR JavaScript Client
- **State Management**: React Context API
- **Notifications**: Notistack

### Backend (BlazorApp)
- **Framework**: ASP.NET Core 10
- **Authentication**: ASP.NET Core Identity with cookie-based auth
- **API**: RESTful Web API controllers
- **Real-time**: SignalR Hubs (dual auth: cookies + JWT)
- **Database**: Entity Framework Core with SQLite/SQL Server

## Project Structure

```
BlazorApp/
├── BlazorApp.Web/              # React TypeScript frontend
│   ├── src/
│   │   ├── components/         # Reusable React components
│   │   ├── contexts/           # React Context providers (Auth, Theme)
│   │   ├── hooks/              # Custom React hooks (useRoomHub)
│   │   ├── pages/              # Page components
│   │   ├── services/           # API client, SignalR hub
│   │   └── types/              # TypeScript type definitions
│   ├── package.json            # npm dependencies
│   ├── tsconfig.json           # TypeScript configuration
│   ├── vite.config.ts          # Vite build configuration
│   └── BlazorApp.Web.csproj    # MSBuild wrapper
│
└── BlazorApp/                  # ASP.NET Core backend
    ├── Controllers/            # Web API controllers
    │   ├── AuthController.cs   # Login/Register/Logout
    │   └── RoomsController.cs  # Room and Question API
    ├── Program.cs              # App configuration
    └── BlazorApp.csproj
```

## Development Workflow

### Running the Application

**Option 1: Development Mode (Recommended)**

1. Start the ASP.NET Core backend:
   ```powershell
   cd BlazorApp\BlazorApp
   dotnet run
   ```

2. In a separate terminal, start the Vite dev server:
   ```powershell
   cd BlazorApp\BlazorApp.Web
   npm run dev
   ```

3. Open `http://localhost:5173` (Vite proxies API calls to ASP.NET Core at port 5000)

**Option 2: Production Build**

```powershell
cd BlazorApp\BlazorApp
dotnet publish -c Release
```

This will:
1. Run `npm install` in BlazorApp.Web
2. Run `npm run build` to create production React build
3. Copy the `dist/` output to `wwwroot/` in the published output

### Key Features Preserved

✅ **Authentication**: Cookie-based with ASP.NET Core Identity  
✅ **Real-time Updates**: SignalR for live question updates  
✅ **Owner Authorization**: JWT tokens for SignalR owner operations  
✅ **Theme Support**: Light/Dark mode with localStorage persistence  
✅ **All User Workflows**:
- Join public Q&A rooms
- Submit questions anonymously
- Create/manage rooms (authenticated users)
- Approve/answer/delete questions (room owners)
- Full-screen question display

## API Endpoints

### Authentication
- `POST /api/auth/login` - Login with email/password
- `POST /api/auth/register` - Register new account
- `POST /api/auth/logout` - Logout (requires auth)
- `GET /api/auth/user` - Get current user info
- `GET /api/auth/signalr-token` - Get JWT token for SignalR (requires auth)

### Rooms & Questions
- `GET /api/rooms` - List all rooms
- `GET /api/rooms/my` - Get user's rooms (requires auth)
- `GET /api/rooms/{id}` - Get room by ID
- `GET /api/rooms/name/{friendlyName}` - Get room by friendly name
- `POST /api/rooms` - Create room (requires auth)
- `DELETE /api/rooms/{id}` - Delete room (requires auth)
- `GET /api/rooms/{roomId}/questions` - Get all questions
- `GET /api/rooms/{roomId}/questions/approved` - Get approved questions
- `POST /api/rooms/{roomId}/questions` - Submit question
- `PUT /api/rooms/{roomId}/questions/{id}/approve` - Approve question (requires auth)
- `PUT /api/rooms/{roomId}/questions/{id}/answer` - Mark answered (requires auth)
- `DELETE /api/rooms/{roomId}/questions/{id}` - Delete question (requires auth)
- `PUT /api/rooms/{roomId}/current-question/{id}` - Set current question (requires auth)

### SignalR Hub
- **Endpoint**: `ws://localhost:5000/hubs/room`
- **Methods**: `JoinAsParticipant`, `JoinAsOwner`, `LeaveAsParticipant`, `LeaveAsOwner`
- **Events**: `QuestionCreated`, `QuestionApproved`, `QuestionAnswered`, `QuestionDeleted`, `CurrentQuestionChanged`, `RoomUpdated`

## Testing

UI tests in `BlazorApp.UITests` will need to be updated for React DOM structure. Update page object selectors to use `data-testid` attributes instead of Blazor-specific selectors.

## Migration Notes

### Removed
- Blazor WebAssembly client project (`BlazorApp.Client`)
- Razor components and pages
- MudBlazor (replaced with Material-UI)
- Blazor-specific services (ThemeService, IdentityRedirectManager, etc.)

### Added
- React TypeScript SPA (`BlazorApp.Web`)
- Web API controllers for REST endpoints
- Material-UI component library
- Vite build tooling
- SignalR JavaScript client integration

### Changed
- Authentication now uses REST API endpoints instead of Razor Pages
- Frontend state management uses React Context instead of Blazor state
- Build process uses Vite instead of Blazor build
- Deployment includes static React build in `wwwroot/`
