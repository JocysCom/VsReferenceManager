@echo off
::-------------------------------------------------------------
:: Check permissions and run as Administrator.
::-------------------------------------------------------------
ATTRIB %windir%\system32 -h | FINDSTR /I "denied" >nul
IF NOT ERRORLEVEL 1 GOTO:ADM
GOTO:EXE
::-------------------------------------------------------------
:ADM
::-------------------------------------------------------------
:: Create temp batch.
SET tb="%TEMP%\%~n0.tmp.bat"
SET tj="%TEMP%\%~n0.tmp.js"
echo @echo off> %tb%
echo %~d0>> %tb%
echo cd "%~p0">> %tb%
echo call "%~nx0" %1 %2 %3 %4 %5 %6 %7 %8 %9>> %tb%
echo del %tj%>> %tb%
:: Delete itself without generating any error message.
echo (goto) 2^>nul ^& del %tb%>> %tb%
:: Create temp script.
echo var arg = WScript.Arguments;> %tj%
echo var wsh = WScript.CreateObject("WScript.Shell");>> %tj%
echo var sha = WScript.CreateObject("Shell.Application");>> %tj%
echo sha.ShellExecute(arg(0), "", wsh.CurrentDirectory, "runas", 1);>> %tj%
:: Execute as Administrator.
cscript /B /NoLogo %tj% %tb%
GOTO:EOF
::-------------------------------------------------------------
:EXE
::-------------------------------------------------------------

::-------------------------------------------------------------
:: Main
::-------------------------------------------------------------
:: List   symbolic links: dir /A:L
:: Remote symbolic links: rmdir Skype
SET upr=C:\Projects\Jocys.com\Class Library
IF EXIST "D:\Projects\Jocys.com\Class Library" SET upr=D:\Projects\Jocys.com\Class Library
CALL:MKJ ComponentModel
CALL:MKJ Configuration
CALL:MKJ Controls
CALL:MKJ Files
CALL:MKJ IO
CALL:MKJ Runtime
pause
GOTO:EOF

::=============================================================
:MKL
::-------------------------------------------------------------

IF NOT EXIST "%~pd1" mkdir "%~pd1"
IF EXIST "%~1" (
  echo Already exists: %~1
) ELSE (
  echo Map: %~1
  fsutil hardlink create "%~1" "%upr%\%~1" > nul
)
GOTO:EOF

::=============================================================
:MKJ
::-------------------------------------------------------------
DIR /B /A:L | findstr "^%~1$" > nul && (
  ECHO Link already exists: %~1
  GOTO:EOF
) 
DIR /B /A:D | findstr "^%~1$" > nul && (
	ECHO Remove directory: %~1
	RMDIR "%~1"
)
ECHO Map: %~1
MKLINK /J "%~1" "%upr%\%~1" > nul
