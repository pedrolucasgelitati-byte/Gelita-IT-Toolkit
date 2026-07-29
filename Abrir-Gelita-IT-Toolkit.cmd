@echo off
setlocal
cd /d "%~dp0"

tasklist /FI "IMAGENAME eq Gelita-IT-Toolkit.exe" /NH | find /I "Gelita-IT-Toolkit.exe" >nul
if not errorlevel 1 (
    echo Fechando a instancia anterior do Gelita IT Toolkit...
    taskkill /IM "Gelita-IT-Toolkit.exe" >nul 2>&1
    timeout /t 1 /nobreak >nul
)

echo Compilando Gelita IT Toolkit...
dotnet build "Gelita-IT-Toolkit.sln" -v:q
if errorlevel 1 (
    echo.
    echo Nao foi possivel compilar o programa.
    echo Feche o Gelita IT Toolkit se ele ja estiver aberto e tente novamente.
    pause
    exit /b 1
)

start "" "%~dp0bin\Debug\net8.0-windows\Gelita-IT-Toolkit.exe"
endlocal
