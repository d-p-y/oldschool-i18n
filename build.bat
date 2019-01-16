@echo off
echo "lib"

cd OldSchool.I18n.Lib
dotnet pack OldSchool.I18n.Lib.fsproj -c Release
cd ..

rem tools documentation:
rem https://stackoverflow.com/questions/52527004/how-to-fix-nu1212-for-dotnet-tool-install
rem docs: https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install
rem tutorial: http://www.tomdupont.net/2017/04/how-to-make-dotnet-cli-tool.html

echo "tool"
cd OldSchool.I18n.Tool
dotnet pack OldSchool.I18n.Tool.fsproj -c Release
cd ..

rem to use: dotnet tool install --global dotnet-oldschool-i18n
rem later to uninstall: dotnet tool uninstall --global dotnet-oldschool-i18n

timeout 5