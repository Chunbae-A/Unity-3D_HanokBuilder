@echo off
cd /d "%~dp0"
set PYTHON=C:\Users\User\anaconda3\python.exe
set SCRIPT=%~dp0culture_asset_downloader.py

powershell -Command "Start-Process -FilePath '%PYTHON%' -ArgumentList 'culture_asset_downloader.py' -WorkingDirectory '%~dp0' -WindowStyle Hidden"

echo 백그라운드에서 실행 중입니다.
echo 로그 확인: %~dp0downloader.log
timeout /t 3 >nul
