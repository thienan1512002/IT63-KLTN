param([string]$ConnectionString = $env:ConnectionStrings__DefaultConnection)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    $ConnectionString = 'Server=localhost;Database=AutomaticApprovalDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True'
}
Push-Location (Split-Path -Parent $PSScriptRoot)
try {
    dotnet ef database update --project src/Web --startup-project src/Web --context ApplicationDbContext --connection $ConnectionString -- --environment Development
    if ($LASTEXITCODE -ne 0) { throw 'Web migration failed.' }
    dotnet ef database update --project src/Worker --startup-project src/Worker --context WorkerDbContext --connection $ConnectionString
    if ($LASTEXITCODE -ne 0) { throw 'Worker migration failed.' }
    Write-Output 'SQL Server migrations and demo seed: Pass'
} finally { Pop-Location }
