Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function TestUsingNPM([string] $testPath) {
    Push-Location $testPath
    Start-Process -PassThru -WindowStyle Hidden -Wait npm "i"
    $test = Start-Process -PassThru -WindowStyle Hidden -Wait npm "run ciTest"
    Pop-Location
    return $test.ExitCode
}

try {
    # invoke regular build/test script
    . (Join-Path $PSScriptRoot "common\build.ps1") @args

    # directly invoke npm tests
    if (($null -ne $args) -and ($args.Contains("-test") -or $args.Contains("-t"))) {
        TestUsingNPM "$PSScriptRoot\..\Microsoft.DotNet.Try.Client"
        TestUsingNPM "$PSScriptRoot\..\Microsoft.DotNet.Try.js"
    }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
