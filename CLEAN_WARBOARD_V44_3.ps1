$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

Write-Host ""
Write-Host "WARBOARD v44.3 - PROJECT CLEANUP" -ForegroundColor Cyan
Write-Host "================================"
Write-Host ""

$Root = $PSScriptRoot
$Assets = Join-Path $Root "Assets"
$Packages = Join-Path $Root "Packages"
$Settings = Join-Path $Root "ProjectSettings"

if (-not (Test-Path $Assets) -or
    -not (Test-Path $Packages) -or
    -not (Test-Path $Settings)) {
    Write-Host "ERROR: This cleanup must be extracted into the Warboard project root." -ForegroundColor Red
    Write-Host "Expected Assets, Packages and ProjectSettings beside this script."
    Read-Host "Press Enter to close"
    exit 1
}

$BuildInfo = Join-Path $Assets "Scripts\Core\WarboardBuildInfo.cs"
if (-not (Test-Path $BuildInfo) -or
    -not ([System.IO.File]::ReadAllText($BuildInfo).Contains('"v44"'))) {
    Write-Host "STOPPED: this cleanup is intended for the current Warboard v44 project." -ForegroundColor Yellow
    Write-Host "Nothing has been changed."
    Read-Host "Press Enter to close"
    exit 1
}

# Local safety archive. Library is generated/ignored, so these files stay on the
# developer machine but are removed from the repository working tree.
$ArchiveRoot = Join-Path $Root "Library\WarboardCleanupArchive\V44_3"
New-Item -ItemType Directory -Force -Path $ArchiveRoot | Out-Null

function Archive-And-Remove([string]$Path, [string]$Bucket) {
    if (-not (Test-Path -LiteralPath $Path)) { return $false }

    $BucketRoot = Join-Path $ArchiveRoot $Bucket
    New-Item -ItemType Directory -Force -Path $BucketRoot | Out-Null

    $relative = $Path.Substring($Root.Length).TrimStart('\','/')
    $safe = $relative.Replace('\','__').Replace('/','__')
    $destination = Join-Path $BucketRoot $safe

    if (Test-Path -LiteralPath $destination) {
        $destination = $destination + "." + [DateTime]::Now.ToString("yyyyMMddHHmmssfff")
    }

    if ((Get-Item -LiteralPath $Path).PSIsContainer) {
        Copy-Item -LiteralPath $Path -Destination $destination -Recurse -Force
        Remove-Item -LiteralPath $Path -Recurse -Force
    } else {
        Copy-Item -LiteralPath $Path -Destination $destination -Force
        Remove-Item -LiteralPath $Path -Force
    }

    Write-Host "  archived: $relative"
    return $true
}

# ---------------------------------------------------------------------------
# 1. Install a proper Unity .gitignore
# ---------------------------------------------------------------------------
$GitIgnore = @'
# Unity generated folders
/[Ll]ibrary/
/[Tt]emp/
/[Oo]bj/
/[Ll]ogs/
/[Uu]ser[Ss]ettings/
/[Mm]emoryCaptures/
/[Rr]ecordings/

# Build output
/[Bb]uild/
/[Bb]uilds/

# IDE / editor generated
/.vs/
/.idea/
/.vscode/
*.csproj
*.sln
*.suo
*.user
*.userprefs
*.pidb
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db

# OS files
.DS_Store
Thumbs.db
desktop.ini

# Temporary source backups / migration debris
*.bak
*.bak.meta
*.before_*
*.custodes-backup
*.custodes-backup.meta
*~

# Local Warboard maintenance archives are stored under Library and are ignored.
'@

[System.IO.File]::WriteAllText(
    (Join-Path $Root ".gitignore"),
    $GitIgnore,
    [System.Text.UTF8Encoding]::new($false)
)
Write-Host "[OK] Added proper Unity .gitignore" -ForegroundColor Green

# ---------------------------------------------------------------------------
# 2. Archive source backup debris from Assets
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "Cleaning source backup debris..."

$backupFiles = @(
    Get-ChildItem -Path $Assets -File -Recurse -ErrorAction SilentlyContinue |
    Where-Object {
        $_.Name -match '\.bak$' -or
        $_.Name -match '\.bak\.meta$' -or
        $_.Name -match '\.before_[^\\\/]+$' -or
        $_.Name -match '\.before_[^\\\/]+\.meta$' -or
        $_.Name -match '\.custodes-backup$' -or
        $_.Name -match '\.custodes-backup\.meta$'
    }
)

$backupCount = 0
foreach ($file in $backupFiles) {
    if (Archive-And-Remove $file.FullName "SourceBackups") {
        $backupCount++
    }
}
Write-Host "[OK] Removed $backupCount source backup/temp file(s) from Assets." -ForegroundColor Green

# ---------------------------------------------------------------------------
# 3. Remove one-time Necron migration installer only after successful marker
# ---------------------------------------------------------------------------
$NecronCatalog = Join-Path $Assets "Scripts\Factions\Necrons\NecronsFactionPack11.cs"
$NecronInstaller = Join-Path $Assets "Editor\WarboardV44NecronsFactionRules.cs"
$Marker = "WARBOARD_V44_FULL_NECRONS_FACTION_RULES"

if ((Test-Path $NecronInstaller) -and
    (Test-Path $NecronCatalog) -and
    ([System.IO.File]::ReadAllText($NecronCatalog).Contains($Marker))) {
    Archive-And-Remove $NecronInstaller "CompletedInstallers" | Out-Null

    $installerMeta = $NecronInstaller + ".meta"
    if (Test-Path $installerMeta) {
        Archive-And-Remove $installerMeta "CompletedInstallers" | Out-Null
    }

    Write-Host "[OK] Removed completed one-time v44 Necron migration installer." -ForegroundColor Green
} elseif (Test-Path $NecronInstaller) {
    Write-Host "[KEEP] Necron installer retained because the completed marker was not found." -ForegroundColor Yellow
}

# ---------------------------------------------------------------------------
# 4. Archive disposable root maintenance scripts / one-off fix notes
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "Archiving obsolete root maintenance files..."

$RootDisposablePatterns = @(
    "FIX_*.bat",
    "FIX_*.ps1",
    "FIX_*.txt",
    "ABILITY_*_INSTALLED.txt",
    "INSTALL_*.bat",
    "INSTALL_*.ps1",
    "ROLLBACK_*.bat",
    "ROLLBACK_*.ps1",
    "CLEAN_WARBOARD_FOLDER.bat",
    "CLEAN_WARBOARD_FOLDER.ps1",
    "CLEAN_WARBOARD_PROJECT.bat",
    "CLEAN_WARBOARD_PROJECT.ps1",
    "README_V44_1_COMPILE_FIX.txt",
    "README_V44_2_NECRON_MIGRATION.txt"
)

$ourFiles = @(
    "CLEAN_WARBOARD_V44_3.bat",
    "CLEAN_WARBOARD_V44_3.ps1",
    "README_V44_3_PROJECT_CLEANUP.txt"
)

$rootDisposable = New-Object System.Collections.Generic.List[System.IO.FileInfo]

foreach ($pattern in $RootDisposablePatterns) {
    foreach ($item in (Get-ChildItem -Path $Root -File -Filter $pattern -ErrorAction SilentlyContinue)) {
        if ($ourFiles -contains $item.Name) { continue }
        if (-not $rootDisposable.Contains($item)) {
            $rootDisposable.Add($item)
        }
    }
}

$rootDisposeCount = 0
foreach ($item in $rootDisposable) {
    if (Archive-And-Remove $item.FullName "RootMaintenance") {
        $rootDisposeCount++
    }
}
Write-Host "[OK] Archived $rootDisposeCount obsolete root maintenance file(s)." -ForegroundColor Green

# ---------------------------------------------------------------------------
# 5. Preserve old release history, but move it out of the project root
# ---------------------------------------------------------------------------
$ReleaseDocs = Join-Path $Root "Docs\Releases"
$AuditDocs = Join-Path $Root "Docs\Audits"
New-Item -ItemType Directory -Force -Path $ReleaseDocs | Out-Null
New-Item -ItemType Directory -Force -Path $AuditDocs | Out-Null

Write-Host ""
Write-Host "Organising historical documentation..."

$releaseMoveCount = 0

Get-ChildItem -Path $Root -File -ErrorAction SilentlyContinue |
Where-Object {
    (
        $_.Name -match '^README_V[0-9].*\.md$' -and
        $_.Name -ne 'README_V44.md'
    ) -or (
        $_.Name -match '^V[0-9].*_PATCH_MANIFEST\.txt$' -and
        $_.Name -ne 'V44_PATCH_MANIFEST.txt'
    )
} |
ForEach-Object {
    $dest = Join-Path $ReleaseDocs $_.Name
    if (Test-Path $dest) { Remove-Item $dest -Force }
    Move-Item -LiteralPath $_.FullName -Destination $dest
    $script:releaseMoveCount++
}

$auditMoveCount = 0
Get-ChildItem -Path $Root -File -ErrorAction SilentlyContinue |
Where-Object { $_.Name -match '^CORE_RULES.*AUDIT.*\.md$' } |
ForEach-Object {
    $dest = Join-Path $AuditDocs $_.Name
    if (Test-Path $dest) { Remove-Item $dest -Force }
    Move-Item -LiteralPath $_.FullName -Destination $dest
    $script:auditMoveCount++
}

Write-Host "[OK] Moved $releaseMoveCount historical release document(s) to Docs\Releases." -ForegroundColor Green
Write-Host "[OK] Moved $auditMoveCount rules audit document(s) to Docs\Audits." -ForegroundColor Green

# A few old root notes are no longer canonical; archive them rather than delete.
$OldRootNotes = @(
    "README.txt",
    "README_FIRST.txt",
    "README_CUSTODES_MODELPACK.txt",
    "FIX_README.txt",
    "ABILITY_WARNING_FIX_V2_INSTALLED.txt"
)
foreach ($name in $OldRootNotes) {
    $p = Join-Path $Root $name
    if (Test-Path $p) {
        Archive-And-Remove $p "LegacyNotes" | Out-Null
    }
}

# ---------------------------------------------------------------------------
# 6. Stop tracking Unity-generated folders WITHOUT deleting them locally
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "Cleaning Git tracking..."

$git = Get-Command git -ErrorAction SilentlyContinue
$gitRepo = Test-Path (Join-Path $Root ".git")

if ($null -ne $git -and $gitRepo) {
    $generated = @(
        "Library",
        "Logs",
        "UserSettings",
        "Temp",
        "obj",
        "Obj",
        ".vs"
    )

    & git rm -r -q --cached --ignore-unmatch -- $generated 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[WARN] Git could not untrack one or more generated folders automatically." -ForegroundColor Yellow
        Write-Host "       Your local files were not deleted."
    } else {
        Write-Host "[OK] Library / Logs / UserSettings etc. are no longer tracked by Git." -ForegroundColor Green
        Write-Host "     They remain on your computer for Unity to keep using."
    }
} else {
    Write-Host "[WARN] Git repository/command not detected. .gitignore was still installed." -ForegroundColor Yellow
}

# ---------------------------------------------------------------------------
# 7. Write a concise current project structure note
# ---------------------------------------------------------------------------
$Structure = @'
WARBOARD v44 - CURRENT PROJECT STRUCTURE

Warboard/
  .gitignore
  Assets/
    Resources/
      Armies/
        Models/
      Factions/
    Scripts/
      Core/
      Factions/
  Docs/
    Audits/
    Releases/
  Packages/
  ProjectSettings/
  README_V44.md
  V44_PATCH_MANIFEST.txt
  BUILD_REPORT.json

TRACK IN GIT
  Assets/
  Packages/
  ProjectSettings/
  Docs/
  current release documentation

DO NOT TRACK
  Library/
  Logs/
  UserSettings/
  Temp/
  obj/
  IDE caches
  temporary *.bak / *.before_* source backups

The cleanup does not delete Library locally. Git simply stops tracking it.
'@

[System.IO.File]::WriteAllText(
    (Join-Path $Root "WARBOARD_PROJECT_STRUCTURE.txt"),
    $Structure,
    [System.Text.UTF8Encoding]::new($false)
)

Write-Host ""
Write-Host "================================" 
Write-Host "WARBOARD v44.3 CLEANUP COMPLETE" -ForegroundColor Green
Write-Host "================================"
Write-Host ""
Write-Host "Kept untouched:"
Write-Host "  Assets game content"
Write-Host "  Packages"
Write-Host "  ProjectSettings"
Write-Host "  README_V44.md / V44_PATCH_MANIFEST.txt"
Write-Host "  BUILD_REPORT.json"
Write-Host ""
Write-Host "Your old scripts/backups are recoverable at:"
Write-Host "  Library\WarboardCleanupArchive\V44_3"
Write-Host ""
Write-Host "IMPORTANT:"
Write-Host "  Open GitHub Desktop after this. You should see a large cleanup change,"
Write-Host "  especially Library/Logs/UserSettings deletions from Git. Commit and push it."
Write-Host ""
Write-Host "This cleans the CURRENT branch. Old Git history still contains previously"
Write-Host "committed Unity Library data; that can be history-purged separately later."
Write-Host ""
Read-Host "Press Enter to close"
