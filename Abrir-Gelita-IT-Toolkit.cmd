@echo off
setlocal
cd /d "%~dp0"

rem Pacote portatil: o executavel fica ao lado deste iniciador.
if exist "%~dp0Gelita-IT-Toolkit.exe" (
    start "" "%~dp0Gelita-IT-Toolkit.exe"
    exit /b 0
)

rem Somente desenvolvedores precisam compilar o codigo-fonte.
where dotnet >nul 2>&1
if errorlevel 1 (
    echo.
    echo O executavel portatil nao foi encontrado.
    echo.
    echo Esta pasta parece ser o codigo-fonte do programa, nao o pacote para usuarios.
    echo Baixe o arquivo ZIP na pagina Releases do GitHub, extraia todo o conteudo
    echo e execute Gelita-IT-Toolkit.exe.
    echo.
    pause
    exit /b 1
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

if exist "%~dp0bin\Debug\net8.0-windows\Gelita-IT-Toolkit.exe" (
    start "" "%~dp0bin\Debug\net8.0-windows\Gelita-IT-Toolkit.exe"
) else (
    echo O build terminou, mas o executavel nao foi encontrado.
    pause
    exit /b 1
)
endlocal
