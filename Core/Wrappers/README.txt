This folder contains wrappers and workarounds for .NET 3.5 and lower (so you can compile to this later versions of .NET Framework)
Dont worry about code, whole library is using auto-detected constants for preprocessor directives to detect exact version of .NET Framework
based on your targeted .NET (PROJECT->PROPERTIES->TARGET FRAMEWORK), this will automatically exlude any wrapper code!

Basically whole library will always be optimalized before compilation (by preprocessor), using code or workaround based on .NET targeted!

Project can be compiled to: .NET 3.5, 4.0, 4.5, 4.5.1 => TESTED, ENJOY ;-)

// This file is auto-detecting .NET Framework targeted by you, its included in .csproj file, dont touch it!
// See it youself for more informations:
VersionSpecificSymbols.Common.prop
// More: http://stackoverflow.com/questions/3436526/detect-target-framework-version-at-compile-time
