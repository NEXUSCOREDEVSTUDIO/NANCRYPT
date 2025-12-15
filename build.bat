@echo off
if not exist "..\bin\Debug" mkdir "..\bin\Debug"
call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat" x64
cl /LD /EHsc /D "NANCRYPTCORE_EXPORTS" /Fe:..\bin\Debug\NanCrypt.Core.dll NativeLib.cpp pch.cpp dllmain.cpp
