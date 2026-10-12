[CmdletBinding()]
param(
    [string]$ConnectionName = "AppConnection"
)

$ErrorActionPreference = "Stop"

$databaseDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $databaseDirectory "..\App.csproj"

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Project file not found: $projectPath"
}

Push-Location (Split-Path -Parent $projectPath)
try {
    dotnet tool restore

    dotnet tool run dotnet-ef dbcontext scaffold `
        "Name=ConnectionStrings:$ConnectionName" `
        Microsoft.EntityFrameworkCore.SqlServer `
        --project $projectPath `
        --startup-project $projectPath `
        --context AppDbContext `
        --context-dir Data `
        --output-dir Data\Entities `
        --namespace App.Data.Entities `
        --context-namespace App.Data `
        --no-onconfiguring `
        --schema dbo `
        --use-database-names `
        --force
}
finally {
    Pop-Location
}
