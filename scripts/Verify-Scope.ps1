$ErrorActionPreference = 'Stop'
Push-Location (Split-Path -Parent $PSScriptRoot)
try {
    $paths = @('src/Web', 'src/Worker', 'src/Worker.Shared', 'tests', 'scripts')
    $exclude = @('-g','!**/bin/**','-g','!**/obj/**','-g','!**/node_modules/**','-g','!**/.git/**',
        '-g','!**/layout_assets/**','-g','!**/lib/**','-g','!**/logs/**','-g','!**/uploads/**',
        '-g','!**/.keys/**','-g','!**/.dist/**','-g','!*.ps1')
    foreach ($entry in @(
        @{Name='PostgreSQL'; Pattern='Npgsql|UseNpgsql|PostgreSQL|\bILIKE\b|ON\s+CONFLICT|\bjsonb\b|NpgsqlValueGenerationStrategy'},
        @{Name='DMC'; Pattern='DMC'},
        @{Name='IRF'; Pattern='IRF'}
    )) {
        $matches = @(rg -il $entry.Pattern @paths @exclude)
        if ($LASTEXITCODE -gt 1) { throw 'rg failed.' }
        if ($matches.Count) { $matches | Write-Output; throw "$($entry.Name) references remain." }
        Write-Output "$($entry.Name) source references: 0"
    }
    $names = @(rg --files @paths @exclude | Where-Object { $_ -match '(?i)dmc|irf' })
    if ($names.Count) { $names | Write-Output; throw 'Forbidden module files remain.' }
    Write-Output 'Forbidden module filenames: 0'
} finally { Pop-Location }
