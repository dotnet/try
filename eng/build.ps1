Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function TestUsingNPM([string] $testPath) {
    Push-Location $testPath
    npm i
    npm run ciTest
    Pop-Location
}

try {
    # invoke regular build/test script
    . (Join-Path $PSScriptRoot "common\build.ps1") @args

    # directly invoke npm tests
    if (($null -ne $args) -and ($args.Contains("-test") -or $args.Contains("-t"))) {
        TestUsingNPM "$PSScriptRoot\..\MLS.Client"
        TestUsingNPM "$PSScriptRoot\..\MLS.Trydotnet.js"
    }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    ExitWithExitCode 1
}
