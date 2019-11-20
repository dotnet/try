Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

$thisDir = Split-Path -Parent $PSCommandPath
$toolLocation = ""
$toolVersion = ""
if (Test-Path 'env:DisableArcade') {
    dotnet pack "$thisDir\MLS.Agent.csproj" /p:Version=0.0.0
    $script:toolLocation = "$thisDir\bin\debug"
    $script:toolVersion = "0.0.0"
} else {
    & "$thisDir\..\build.cmd" -pack
    $script:toolLocation = "$thisDir\..\artifacts\packages\Debug\Shipping"
    $script:toolVersion = "1.0.44142.42"
}

dotnet tool uninstall -g dotnet-try
dotnet tool install -g --add-source "$toolLocation" --version $toolVersion dotnet-try
