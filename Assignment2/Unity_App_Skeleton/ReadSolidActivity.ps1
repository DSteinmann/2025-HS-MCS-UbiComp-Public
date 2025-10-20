# PowerShell script to read current activity from Solid pod
# Usage: .\ReadSolidActivity.ps1 -Email "your@email.com" -Password "yourpassword"

param(
    [Parameter(Mandatory=$true)]
    [string]$Email,

    [Parameter(Mandatory=$true)]
    [string]$Password
)

# Configuration - modify these as needed
$serverUrl = "https://wiser-solid-xi.interactions.ics.unisg.ch/"
$webId = "https://wiser-solid-xi.interactions.ics.unisg.ch/dominik-ubicomp2025/profile/card#me"

Write-Host "Reading current activity from Solid pod..." -ForegroundColor Green
Write-Host "Server: $serverUrl" -ForegroundColor Gray
Write-Host "WebID: $webId" -ForegroundColor Gray
Write-Host ""

try {
    # Run the C# program
    & dotnet run --project "SolidActivityReader.csproj" $serverUrl $webId $Email $Password
}
catch {
    Write-Host "Error running the activity reader: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Make sure you have:" -ForegroundColor Yellow
    Write-Host "1. .NET SDK installed" -ForegroundColor Yellow
    Write-Host "2. Created a SolidActivityReader.csproj file" -ForegroundColor Yellow
    Write-Host "3. The SolidInteractionLibrary is available" -ForegroundColor Yellow
}