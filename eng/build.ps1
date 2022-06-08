[CmdletBinding(PositionalBinding = $false)]
param (
    [string]$configuration = "Debug",
    [switch]$build,
    [switch]$binaryLog,
    [switch]$noDotnet,
    [switch]$test,
    [switch]$integrationTest,
    [Parameter(ValueFromRemainingArguments = $true)][String[]]$arguments
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    $repoRoot = Resolve-Path "$PSScriptRoot\.."
    $npmDirs = @(
        "src\microsoft-trydotnet",
        "src\microsoft-trydotnet-editor",
        "src\microsoft-trydotnet-styles",
        "src\microsoft-learn-mock"
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
        if ($test) {
            $integrationTest = $true
        }

        # common paths
        $solutionFile = (Join-Path $repoRoot "TryDotNet.sln")

        # invoke regular build script
        $buildScript = (Join-Path $PSScriptRoot "common\build.ps1")
        Invoke-Expression "$buildScript -projects $solutionFile /p:Configuration=$configuration /p:BinaryLog=$binaryLog /p:Build=$build $arguments"
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }

        # playwright
        if ($integrationTest) {
            & $repoRoot\artifacts\bin\Microsoft.TryDotNet.IntegrationTests\$configuration\net6.0\playwright.ps1 install chromium
        }
    }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
