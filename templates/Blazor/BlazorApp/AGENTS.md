# Q&A System - Agent Context

## Project Overview

This is a Blazor-based Q&A (Question and Answer) system built with .NET Aspire. The application allows authenticated users to create Q&A rooms, while both authenticated and unauthenticated users can participate by asking questions and viewing approved questions in real-time.

## Architecture

### Technology Stack
- **.NET 10.0** - Primary framework
- **Blazor Server & WebAssembly** - Hybrid rendering mode
- **ASP.NET Core Identity** - Authentication and authorization
- **Entity Framework Core** - Data access with Azure SQL
- **SignalR** - Real-time communication
- **Radzen Blazor Components** - UI component library
- **.NET Aspire** - Cloud-native application orchestration

### Project Structure

```
BlazorApp/
├── BlazorApp.AppHost/          # Aspire orchestration project
├── BlazorApp.ServiceDefaults/  # Shared service defaults
├── BlazorApp.Data/             # Data models and EF Core context
│   ├── Room.cs                 # Room entity (formerly TicketQueue)
│   ├── Question.cs             # Question entity
│   ├── ApplicationDbContext.cs # EF Core DbContext
│   └── Migrations/             # Database migrations
├── BlazorApp.Core/             # Business logic and services
│   ├── QA/
│   │   ├── IRoomService.cs     # Room service interface
│   │   ├── RoomService.cs      # Room service implementation
│   │   ├── IQuestionService.cs # Question service interface
│   │   └── QuestionService.cs  # Question service implementation
│   └── Hubs/
│       └── RoomHub.cs   # SignalR hub for real-time updates
└── BlazorApp/BlazorApp/        # Main Blazor application
    └── Components/
        ├── QA/Pages/
        │   ├── JoinRoom.razor      # Home page - room entry
        │   ├── RoomView.razor      # Public room view
        │   ├── MyRooms.razor       # Authenticated user's rooms
        │   └── ManageRoom.razor    # Room owner dashboard
        ├── Account/                # Identity pages
        └── Layout/                 # Layout components
```

## Data Model

### Room Entity
Represents a Q&A room where questions can be asked and answered.

**Properties:**
- `Id` (Guid) - Primary key
- `FriendlyName` (string, required, max 200) - Short name used in URLs (case-insensitive, unique)
- `CreatedByUserId` (string, required) - Owner/creator user ID
- `CreatedBy` (ApplicationUser) - Navigation property to owner
- `CreatedDate` (DateTimeOffset) - When the room was created
- `CurrentQuestionId` (Guid?, nullable) - ID of the current active question
- `CurrentQuestion` (Question) - Navigation property to current question
- `Questions` (ICollection<Question>) - All questions in the room
- `RowVersion` (byte[], timestamp) - Concurrency token

**Database Table:** `Rooms`

**Indexes:**
- Unique index on `FriendlyName` for case-insensitive lookups
- Foreign key to `AspNetUsers` (CreatedBy)
- Foreign key to `Questions` (CurrentQuestion) with SetNull on delete

### Question Entity
Represents a question asked in a room.

**Properties:**
- `Id` (Guid) - Primary key
- `RoomId` (Guid, required) - Foreign key to Room
- `Room` (Room) - Navigation property to room
- `QuestionText` (string, required, max 2000) - The question content
- `AuthorName` (string?, optional, max 100) - Display name of the asker
- `IsAnswered` (bool, default false) - Whether the question has been answered
- `IsApproved` (bool, default false) - Whether the question is approved for public view
- `CreatedDate` (DateTimeOffset) - When the question was asked
- `LastModifiedDate` (DateTimeOffset?, nullable) - Last edit timestamp
- `RowVersion` (byte[], timestamp) - Concurrency token

**Database Table:** `Questions`

**Indexes:**
- Foreign key to `Rooms` with Cascade delete
- Index on `RoomId`
- Composite index on `(RoomId, IsApproved, IsAnswered)` for efficient filtering

## User Roles & Permissions

### Authenticated Users
- Can create new rooms
- Automatically become owner of rooms they create
- Can view and manage their own rooms

### Room Owners (Authenticated)
Exclusive permissions for rooms they created:
- Approve or reject pending questions
- Mark approved questions as answered
- Set any approved question as the "current" question
- Clear the current question
- Delete questions
- Delete the room

### All Users (Authenticated & Unauthenticated)
When viewing a room:
- Set a display name (persisted in browser via ProtectedLocalStorage)
- Submit questions (subject to rate limiting)
- View the current question
- View all approved questions (both answered and unanswered)
- See real-time updates via SignalR
- Edit their own questions before approval

**Note:** Authenticated users who are NOT the room owner see the same public view as unauthenticated users, with their name pre-populated from their identity.

## Key Features

### Room Management
- **Case-insensitive room names** - "TechTalk" and "techtalk" are treated as the same
- **Unique room names** - Enforced at database level
- **Room creation** - Only authenticated users can create rooms
- **Owner dashboard** - Manage questions, approvals, and current question

### Question Workflow
1. **Submit** - User submits a question (rate limited)
2. **Pending** - Question awaits owner approval (only owner sees it)
3. **Approved** - Owner approves, question becomes visible to all
4. **Answered** - Owner marks question as answered (visual indicator)
5. **Current** - Owner optionally sets question as "current" (prominently displayed)

### Rate Limiting
- **Unauthenticated users:** 1 question per 10 seconds per client ID
- **Client ID:** Stored in browser local storage, persists across sessions
- **Implementation:** In-memory tracking with `ConcurrentDictionary` in `QuestionService`
- **Countdown timer:** UI shows remaining seconds before next submission

### Real-time Updates (SignalR)
All changes broadcast in real-time to connected clients:

**Hub:** `RoomHub` at `/hubs/room`

**SignalR Groups:**
- `room-{roomId}` - All participants in a room
- `room-{roomId}-owner` - Room owner only

**Events:**
- `QuestionSubmitted` - New question (to owner only)
- `QuestionApproved` - Question approved (to all in room)
- `QuestionAnswered` - Question answered (to all in room)
- `CurrentQuestionChanged` - Current question updated (to all in room)
- `RoomCreated` - New room created (to all)
- `RoomDeleted` - Room deleted (to all in room)
- `QuestionDeleted` - Question removed (to all in room)

**Hub Methods:**
- `JoinRoom(string roomId)` - Join room as participant
- `LeaveRoom(string roomId)` - Leave room
- `JoinRoomAsOwner(string roomId)` - Join as owner (gets both groups)
- `LeaveRoomAsOwner(string roomId)` - Leave as owner

## Service Layer

### IRoomService / RoomService
**Purpose:** Manage Q&A rooms

**Methods:**
- `GetAllRoomsAsync()` - Get all rooms
- `GetRoomsByUserIdAsync(string userId)` - Get user's rooms
- `GetRoomByIdAsync(Guid id)` - Get room by ID
- `GetRoomByFriendlyNameAsync(string friendlyName)` - Get room by name (case-insensitive)
- `CreateRoomAsync(string friendlyName, string userId, CancellationToken)` - Create new room
- `SetCurrentQuestionAsync(Guid roomId, Guid? questionId, string userId, CancellationToken)` - Set/clear current question
- `DeleteRoomAsync(Guid roomId, string userId, CancellationToken)` - Delete room (owner only)

**Authorization:** All mutation methods verify room ownership

### IQuestionService / QuestionService
**Purpose:** Manage questions within rooms

**Methods:**
- `GetQuestionsByRoomIdAsync(Guid roomId)` - Get all questions in room
- `GetApprovedQuestionsByRoomIdAsync(Guid roomId)` - Get approved questions only
- `GetQuestionByIdAsync(Guid id)` - Get single question
- `SubmitQuestionAsync(Guid roomId, string questionText, string? authorName, CancellationToken)` - Submit new question
- `UpdateQuestionAsync(Guid questionId, string questionText, string? authorName, CancellationToken)` - Edit unapproved question
- `ApproveQuestionAsync(Guid questionId, string userId, CancellationToken)` - Approve question (owner only)
- `MarkAsAnsweredAsync(Guid questionId, string userId, CancellationToken)` - Mark as answered (owner only)
- `DeleteQuestionAsync(Guid questionId, string userId, CancellationToken)` - Delete question (owner only)
- `CanSubmitQuestionAsync(string clientId)` - Check rate limit

**Authorization:** Approval/answering/deletion methods verify room ownership

## Page Routes

### Public Routes (No Authentication Required)
- `/` - **JoinRoom.razor** - Home page to enter room name
- `/room/{FriendlyName}` - **RoomView.razor** - Public room view with Q&A interface

### Authenticated Routes
- `/my-rooms` - **MyRooms.razor** - List user's rooms, create new rooms
- `/room/{FriendlyName}/manage` - **ManageRoom.razor** - Owner dashboard

## UI Components

### JoinRoom.razor (/)
- Simple form to enter room name
- Navigate to room or show error if not found
- Link to "My Rooms" for authenticated users

### RoomView.razor (/room/{name})
**Display Name Flow:**
1. Check ProtectedLocalStorage for saved name
2. If authenticated and no saved name, use identity name
3. If neither, show name entry dialog
4. Save to ProtectedLocalStorage on submission

**UI Sections:**
- Header with room name and current user badge
- Current question card (if set)
- Ask question form (with rate limit countdown)
- Approved questions list (with answered badges)

**Real-time Features:**
- Auto-updates when questions approved/answered
- Current question changes immediately
- Rate limit countdown timer

### MyRooms.razor (/my-rooms)
- Grid of user's rooms
- Create room modal dialog
- Quick links to manage or view public side
- Shows current question if set

### ManageRoom.razor (/room/{name}/manage)
**Owner Authorization:** Redirects if not room owner

**UI Sections:**
- Current question panel with "Clear" button
- Tabs: "Pending Approval" and "Approved"
- Pending tab: Approve/delete buttons
- Approved tab: Set as current, mark as answered, delete

**Real-time Features:**
- Auto-updates when new questions submitted
- Live refresh on all question state changes

## Database Configuration

### Connection String
Key: `ConnectionStrings:Database` in appsettings.json

### Migrations
Located in: `BlazorApp.Data/Migrations/`

**Key Migration:** `TransformToQASystem`
- Renames `TicketQueues` table to `Rooms`
- Changes `DateTime` to `DateTimeOffset`
- Adds `CurrentQuestionId` column
- Creates `Questions` table
- Adds unique index on `FriendlyName`
- Adds composite indexes for performance

**Apply Migration:**
```bash
dotnet ef database update --project BlazorApp.Data --startup-project BlazorApp.AppHost
```

### EF Core Configuration
- **DbContext:** `ApplicationDbContext`
- **Provider:** Azure SQL (via Aspire)
- **Factory Pattern:** Uses `IDbContextFactory<ApplicationDbContext>` for scalability
- **Identity:** Integrates with ASP.NET Core Identity for user management

## Dependency Injection

### Service Registration (BlazorApp.Core/DependencyInjection.cs)

```csharp
builder.AddServiceDefaults()      // Aspire defaults
    .AddDatabase()                 // EF Core + Identity
    .AddQAServices();              // Room & Question services
```

**AddQAServices Extension:**
- Registers `IRoomService` → `RoomService` (Scoped)
- Registers `IQuestionService` → `QuestionService` (Scoped)

### SignalR Configuration (Program.cs)
```csharp
builder.Services.AddSignalR();
app.MapHub<RoomHub>("/hubs/room");
```

## State Management

### Client-Side State (ProtectedLocalStorage)
- `qa_clientId` - UUID for rate limiting
- `qa_displayName` - User's chosen display name

**Note:** ProtectedLocalStorage encrypts data, making it secure but browser-specific

### Server-Side State
- **Rate Limiting:** `ConcurrentDictionary<string, DateTimeOffset>` in QuestionService
- **SignalR Connections:** Managed by SignalR hub

## Common Development Tasks

### Add a New Property to Room
1. Update `Room.cs` in BlazorApp.Data
2. Create migration: `dotnet ef migrations add AddRoomProperty --project BlazorApp.Data --startup-project BlazorApp.AppHost`
3. Apply migration: `dotnet ef database update --project BlazorApp.Data --startup-project BlazorApp.AppHost`
4. Update `RoomService` if business logic needed
5. Update UI components as needed

### Add a New Page
1. Create `.razor` file in appropriate folder under `Components/`
2. Add `@page` directive with route
3. Add to navigation in `NavMenu.razor` if needed
4. Add `@attribute [Authorize]` if authentication required

### Debug SignalR Issues
1. Check browser console for connection errors
2. Verify hub URL in component matches `Program.cs` mapping
3. Check that `JoinRoom`/`JoinRoomAsOwner` called after connection
4. Ensure hub methods match server-side signatures exactly

### Test Rate Limiting
1. Clear browser local storage to reset client ID
2. Submit question
3. Try immediate resubmit - should show 10-second countdown
4. Wait for countdown - submit button re-enables

## Security Considerations

### Authorization
- Room operations verify ownership via `CreatedByUserId`
- Unauthorized access throws `UnauthorizedAccessException`
- `[Authorize]` attribute protects authenticated-only pages

### Input Validation
- String length limits enforced via data annotations
- Required fields validated
- Case-insensitive uniqueness on room names

### Rate Limiting
- Prevents spam from unauthenticated users
- Per-client tracking via persistent ID
- 10-second window between submissions

### SQL Injection
- Prevented by Entity Framework Core parameterization
- No raw SQL queries used

## Troubleshooting

### Build Errors
- Ensure all NuGet packages restored: `dotnet restore`
- Clean and rebuild: `dotnet clean && dotnet build`
- Check for missing using statements

### Database Issues
- Verify connection string in appsettings.json
- Check migration status: `dotnet ef migrations list --project BlazorApp.Data --startup-project BlazorApp.AppHost`
- Reset database (dev only): `dotnet ef database drop --project BlazorApp.Data --startup-project BlazorApp.AppHost`

### SignalR Not Updating
- Check browser console for hub connection errors
- Verify hub is mapped in Program.cs
- Ensure clients join correct groups
- Check network tab for WebSocket/SSE connections

### Rate Limiting Not Working
- Check browser local storage for `qa_clientId`
- Verify QuestionService is registered as Scoped
- Check server logs for rate limit checks

## Future Enhancements

### Potential Features
- **Question voting** - Upvote/downvote questions
- **Question ordering** - Sort by votes, date, answered status
- **Room settings** - Public/private, moderation options
- **Export functionality** - Download questions as CSV/JSON
- **Search/filter** - Find specific questions
- **Analytics** - Room statistics, participation metrics
- **Notifications** - Email/push when question answered
- **Rich text** - Markdown support for questions
- **Attachments** - Allow images/files with questions
- **Multiple moderators** - Share room management

### Technical Improvements
- **Caching** - Redis for approved questions
- **Pagination** - For rooms with many questions
- **Background jobs** - Cleanup old unapproved questions
- **Audit logging** - Track all room/question changes
- **Health checks** - Monitor service availability
- **Rate limit persistence** - Redis instead of in-memory

## Related Documentation
- **README.md** - General project information
- **Migration Files** - Database schema evolution
- **.editorconfig** - Code style rules
- **Directory.Build.props** - Shared MSBuild properties
