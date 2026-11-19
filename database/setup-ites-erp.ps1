[CmdletBinding()]
param(
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'
$server = 'L-PANKAJ\PKSQLEXP'
$database = 'ITES_ERP'
$scriptPath = Join-Path $PSScriptRoot 'migrations\ites-erp-idempotent.sql'

if (-not $Apply) {
    Write-Output 'No database changes were made. Review this script and rerun with -Apply only after database-owner approval.'
    exit 0
}

if (-not (Test-Path $scriptPath)) {
    throw "The generated migration script is missing. Generate it with dotnet ef migrations script --idempotent."
}

$existsQuery = "SET NOCOUNT ON; SELECT CASE WHEN DB_ID(N'$database') IS NULL THEN 'absent' ELSE 'present' END"
$existsResult = & sqlcmd -S $server -E -d master -b -W -h -1 -Q $existsQuery 2>&1
if ($LASTEXITCODE -ne 0) {
    throw 'Could not verify the target database using Windows Authentication.'
}
$databaseExists = (($existsResult | Out-String).Trim() -split '\s+') -contains 'present'

if ($databaseExists) {
    $schemaQuery = @"
SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
    SELECT '0|0|' + CAST((SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0) AS nvarchar(10));
ELSE
    SELECT '1|' + CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId LIKE N'%_InitialErpSchema') THEN 1 ELSE 0 END AS nvarchar(10))
        + '|' + CAST((SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0 AND name <> N'__EFMigrationsHistory') AS nvarchar(10));
"@
    $schemaResult = & sqlcmd -S $server -E -d $database -b -W -h -1 -Q $schemaQuery 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not verify the target database schema.'
    }
    $schemaLine = $schemaResult | Where-Object { $_ -match '^\s*[01]\|[01]\|\d+\s*$' } | Select-Object -First 1
    if (-not $schemaLine) {
        throw 'The target database schema check returned no result.'
    }
    $schemaParts = $schemaLine.Trim().Split('|')
    if ($schemaParts[0] -eq '0' -and [int]$schemaParts[2] -gt 0) {
        throw 'The existing database has tables but no EF migration history. No schema changes were applied; review and baseline it first.'
    }
    if ($schemaParts[0] -eq '1' -and $schemaParts[1] -eq '0') {
        throw 'The existing migration history does not contain this ERP initial migration. No schema changes were applied; review and baseline it first.'
    }
}
else {
    & sqlcmd -S $server -E -d master -b -i (Join-Path $PSScriptRoot 'create-ites-erp.sql')
    if ($LASTEXITCODE -ne 0) {
        throw 'Database creation failed. No migration script was applied.'
    }
}

& sqlcmd -S $server -E -d $database -b -i $scriptPath
if ($LASTEXITCODE -ne 0) {
    throw 'Applying the idempotent migration script failed. Review the SQL Server error and database state.'
}

Write-Output 'The idempotent ERP migration script completed successfully.'
