@echo off
powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0eng\build.ps1" -noDotnet %*
taskkill /F /IM dotnet.exe
rmdir /s /q ".\src\Microsoft.TryDotNet.IntegrationTests\bin"
rmdir /s /q ".\artifacts\bin\Microsoft.TryDotNet.IntegrationTests"
