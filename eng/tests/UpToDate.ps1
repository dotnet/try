# This script verifies that subsequent calls to `build.cmd` don't cause js/css to be unnecessarily rebuilt.

[CmdletBinding(PositionalBinding=$false)]
param (
    [string][Alias('c')]$configuration = "Debug",
    [parameter(ValueFromRemainingArguments=$true)][string[]]$properties
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    $RepoRoot = Join-Path $PSScriptRoot ".." | Join-Path -ChildPath ".." -Resolve
    $BuildScript = Join-Path $RepoRoot "build.cmd"

    # do first build
    & $BuildScript -configuration $configuration @properties
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error running first build."
        exit 1
    }

    # list of sentinel files to check
    $FileList = Get-ChildItem -Path "$RepoRoot\MLS.Agent\wwwroot" -Recurse -File | %{ $_.FullName }

    # gather file timestamps
    $InitialFilesAndTimes = @{}
    foreach ($f in $FileList) {
        $LastWriteTime = (Get-Item $f).LastWriteTimeUtc
        $InitialFilesAndTimes.Add($f, $LastWriteTime)
    }

    # build again
    & $BuildScript -configuration $configuration @properties
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error running second build."
        exit 1
    }

    # gather file timestamps again
    $FinalFilesAndTimes = @{}
    foreach ($f in $FileList) {
        $LastWriteTime = (Get-Item $f).LastWriteTimeUtc
        $FinalFilesAndTimes.Add($f, $LastWriteTime)
    }

    # validate that file timestamps haven't changed
    $RebuiltFiles = @()
    foreach ($f in $FileList) {
        $InitialTime = $InitialFilesAndTimes[$f]
        $FinalTime = $FinalFilesAndTimes[$f]
        if ($InitialTime -ne $FinalTime) {
            $RebuiltFiles += $f
        }
    }

    $FileCount = $FileList.Length
    $RebuiltCount = $RebuiltFiles.Length
    Write-Host "$RebuiltCount of $FileCount files were re-built."
    $RebuiltFiles | ForEach-Object { Write-Host "    $_" }
    exit $RebuiltCount
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
