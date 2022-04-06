[CmdletBinding(PositionalBinding = $false)]
param (
    [switch]$ci,
    [switch]$noDotnet,
    [switch]$test,
    [Parameter(ValueFromRemainingArguments = $true)][String[]]$arguments
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    $repoRoot = Resolve-Path "$PSScriptRoot\.."
    $npmDirs = @(
        "src\microsoft-trydotnet",
        "src\microsoft-trydotnet-editor",
        "src\microsoft-trydotnet-styles"
    )
    foreach ($npmDir in $npmDirs) {
        Push-Location "$repoRoot\$npmDir"
        Write-Host "Building NPM in directory $npmDir"
        npm ci
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        npm run buildProd
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

        if ($test) {
            Write-Host "Testing NPM in directory $npmDir"
            npm run ciTest
            if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        }

        Pop-Location
    }

    if (-Not $noDotnet) {
        # promote switches to arguments
        if ($ci) {
            $arguments += "-ci"
        }
        if ($test) {
            $arguments += '-test'
        }

        # invoke regular build/test script
        $buildScript = (Join-Path $PSScriptRoot "common\build.ps1")
        Invoke-Expression "$buildScript -projects ""$PSScriptRoot\..\TryDotNet.sln"" $arguments"
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
