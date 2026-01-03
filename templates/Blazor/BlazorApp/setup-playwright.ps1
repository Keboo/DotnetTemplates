# Playwright Setup Script for BlazorApp UI Tests

Write-Host "Setting up Playwright for BlazorApp UI Tests..." -ForegroundColor Green

# Build the test project first
Write-Host "`nBuilding test project..." -ForegroundColor Yellow
dotnet build BlazorApp.UITests/BlazorApp.UITests.csproj

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed. Please fix build errors first." -ForegroundColor Red
    exit 1
}

# Install Playwright browsers
Write-Host "`nInstalling Playwright browsers..." -ForegroundColor Yellow
pwsh BlazorApp.UITests/bin/Debug/net10.0/playwright.ps1 install

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✓ Playwright setup complete!" -ForegroundColor Green
    Write-Host "`nYou can now run tests with:" -ForegroundColor Cyan
    Write-Host "  dotnet test BlazorApp.UITests/BlazorApp.UITests.csproj" -ForegroundColor White
    Write-Host "`nOr set a custom base URL:" -ForegroundColor Cyan
    Write-Host "  `$env:TEST_BASE_URL = 'https://your-url.com'" -ForegroundColor White
    Write-Host "  dotnet test BlazorApp.UITests/BlazorApp.UITests.csproj" -ForegroundColor White
} else {
    Write-Host "`n✗ Playwright installation failed." -ForegroundColor Red
    exit 1
}
