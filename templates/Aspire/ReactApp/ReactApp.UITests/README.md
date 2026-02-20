# ReactApp UI Tests

Comprehensive UI test suite for the Q&A System built with Playwright and MSTest.

## Overview

This test project uses:
- **Playwright** for browser automation
- **MSTest** for test framework
- **Page Object Model (POM)** pattern for maintainability

## Test Coverage

The `QAWorkflowTests.cs` includes a comprehensive test that covers:

1. ✅ Register a new account
2. ✅ Verify email using the confirmation link
3. ✅ Login with credentials
4. ✅ Create a Q&A room
5. ✅ Join room from different browser instance
6. ✅ Submit two questions from second browser
7. ✅ Reject one question (owner)
8. ✅ Approve second question (owner)
9. ✅ Verify approved question appears in real-time (SignalR)
10. ✅ Set question as current
11. ✅ Clear current question
12. ✅ Mark question as answered
13. ✅ Delete question
14. ✅ Delete room
15. ✅ Verify all account pages load
16. ✅ Delete user account

## Project Structure

```
ReactApp.UITests/
├── PageObjects/              # Page Object Model classes
│   ├── RegisterPage.cs       # Registration page
│   ├── LoginPage.cs          # Login page
│   ├── MyRoomsPage.cs        # My Rooms page
│   ├── RoomViewPage.cs       # Public room view
│   ├── ManageRoomPage.cs     # Room management dashboard
│   └── AccountPages.cs       # Account management pages
├── QAWorkflowTests.cs        # Main test suite
├── Test1.cs                  # Sample placeholder test
├── TestConfiguration.cs      # Configuration settings
└── MSTestSettings.cs         # MSTest configuration
```

## Configuration

### Base URL

By default, tests run against `https://localhost:7239`. You can override this using an environment variable:

**PowerShell:**
```powershell
$env:TEST_BASE_URL = "https://your-app-url.com"
dotnet test
```

**Command Prompt:**
```cmd
set TEST_BASE_URL=https://your-app-url.com
dotnet test
```

**Bash/Linux:**
```bash
export TEST_BASE_URL=https://your-app-url.com
dotnet test
```

### Timeouts

Default timeouts are configured in `TestConfiguration.cs`:
- `DefaultTimeout`: 10000ms (10 seconds) for page navigation and element visibility
- `SignalRTimeout`: 5000ms (5 seconds) for SignalR real-time updates

### Browser Display Mode

By default, tests run in **headless mode** (no visible browser window).

**Run in headed mode** (visible browser window - useful for debugging):

**PowerShell:**
```powershell
$env:HEADLESS = "false"
dotnet test
```

**Command Prompt:**
```cmd
set HEADLESS=false
dotnet test
```

**Bash/Linux:**
```bash
export HEADLESS=false
dotnet test
```

### Slow Motion (Debugging)

Add a delay between actions to watch tests execute:

```powershell
$env:SLOW_MO = "1000"  # 1 second delay between actions
dotnet test
```

## Running Tests

### Prerequisites

1. Install Playwright browsers (first time only):
```powershell
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install
```

Or use the Playwright CLI:
```powershell
playwright install
```

2. Ensure the ReactApp is running at the configured base URL

### Linux/Ubuntu-Specific Instructions

✅ **Good news**: The Vite configuration has been updated to support Linux! Tests should now work using either method below.

**Method 1: Standard (try this first)**
```bash
cd ReactApp.UITests
dotnet test
```

**Method 2: External AppHost (if Method 1 has issues)**

Terminal 1 - Start the AppHost:
```bash
dotnet run --project ReactApp.AppHost
```

Terminal 2 - Run tests:
```bash
# Get the frontend port from the Aspire dashboard
export FRONTEND_URL=http://localhost:<port>
cd ReactApp.UITests
dotnet test
```

See [RUNNING-ON-LINUX.md](RUNNING-ON-LINUX.md) for detailed troubleshooting.

### Run All Tests

```powershell
dotnet test
```

### Run Specific Test

```powershell
dotnet test --filter "FullyQualifiedName~CompleteQAWorkflow"
```

### Run with Specific Browser

```powershell
$env:BROWSER = "chromium"  # or "firefox", "webkit"
dotnet test
```

### Combine Multiple Options

```powershell
# Run in headed mode with slow motion against custom URL
$env:HEADLESS = "false"
$env:SLOW_MO = "500"
$env:TEST_BASE_URL = "https://staging.example.com"
dotnet test
```

## Test Execution Notes

### Multi-Browser Testing

The comprehensive workflow test uses **two browser instances** simultaneously to test real-time SignalR features:
- Primary browser: Room owner performing management actions
- Secondary browser: Anonymous user viewing the public room

### Rate Limiting

Tests respect the application's rate limiting (10 seconds between question submissions). The test includes appropriate delays to handle this.

### Email Verification

Tests extract the email confirmation link directly from the registration confirmation page, simulating clicking the link in an email.

### Test Isolation

Each test uses unique email addresses and room names (timestamped) to ensure isolation and avoid conflicts.

## Page Object Model

All UI element selectors and page interactions are consolidated in Page Object classes under `PageObjects/` directory. This makes tests:
- **Maintainable**: Update selectors in one place
- **Readable**: Tests read like user actions
- **Reusable**: Share common actions across tests

### Example Usage

```csharp
var loginPage = new LoginPage(Page);
await loginPage.NavigateAsync();
await loginPage.LoginAsync("user@example.com", "password");
Assert.IsTrue(await loginPage.IsLoggedInAsync());
```

## Debugging Tests

### View Test Output

```powershell
dotnet test --logger "console;verbosity=detailed"
```

### Run in Headed Mode (See Browser Window)

```powershell
$env:HEADLESS = "false"
dotnet test
```

### Slow Motion (Watch Actions Execute)

```powershell
$env:SLOW_MO = "1000"     # 1 second delay between actions
$env:HEADLESS = "false"  # Must use headed mode to see slow motion
dotnet test
```

### Screenshots on Failure

Playwright automatically captures screenshots on test failures. Check the test results output for the path.

## CI/CD Integration

### GitHub Actions Example

```yaml
- name: Install Playwright Browsers
  run: pwsh ReactApp.UITests/bin/Debug/net10.0/playwright.ps1 install --with-deps

- name: Run UI Tests
  env:
    TEST_BASE_URL: https://staging.example.com
  run: dotnet test ReactApp.UITests/ReactApp.UITests.csproj
```

### Azure DevOps Example

```yaml
- task: PowerShell@2
  displayName: 'Install Playwright'
  inputs:
    targetType: 'inline'
    script: |
      pwsh ReactApp.UITests/bin/Debug/net10.0/playwright.ps1 install --with-deps

- task: DotNetCoreCLI@2
  displayName: 'Run UI Tests'
  inputs:
    command: 'test'
    projects: 'ReactApp.UITests/ReactApp.UITests.csproj'
  env:
    TEST_BASE_URL: $(TestBaseUrl)
```

## Extending Tests

### Add New Page Object

1. Create new class in `PageObjects/` directory
2. Inherit from nothing (just use IPage)
3. Define locators as properties
4. Implement page actions as methods

### Add New Test

1. Add method to `QAWorkflowTests.cs` or create new test class
2. Use `[TestMethod]` attribute
3. Leverage existing Page Objects
4. Follow AAA pattern: Arrange, Act, Assert

## Troubleshooting

### "Browser not installed" error
Run: `pwsh bin/Debug/net10.0/playwright.ps1 install`

### Element not found errors
- Check if selectors in Page Objects match current UI
- Increase timeout in `TestConfiguration.cs`
- Add explicit waits: `await locator.WaitForAsync()`

### SignalR updates not received
- Ensure application is running and SignalR hub is configured
- Increase `SignalRTimeout` in `TestConfiguration.cs`
- Check browser console logs for WebSocket errors

### Tests fail on CI but pass locally
- Ensure Playwright browsers installed on CI agent
- Check base URL configuration
- Verify application is running and accessible

## Additional Resources

- [Playwright for .NET Documentation](https://playwright.dev/dotnet/)
- [MSTest Documentation](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-mstest)
- [Page Object Model Pattern](https://playwright.dev/dotnet/docs/pom)
