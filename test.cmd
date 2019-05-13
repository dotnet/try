@echo off
powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0eng\common\build.ps1" -test -binaryLog %*
