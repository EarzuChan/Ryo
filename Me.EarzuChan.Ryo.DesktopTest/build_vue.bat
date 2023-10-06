@echo off

echo Building Vue...
echo.
echo.

cd ..\Me.EarzuChan.Ryo.DesktopTest.VueTest
call npm install
call npm run build

echo.
echo.
set destination=..\Me.EarzuChan.Ryo.DesktopTest\WebResources
echo Copying Vue Folder To: %destination%
echo.
echo.

rmdir /s /q "%destination%"
xcopy /s /y /i "..\Me.EarzuChan.Ryo.DesktopTest.VueTest\Dist\*" "%destination%"