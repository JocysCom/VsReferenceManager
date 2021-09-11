# Jocys.com VS Reference Manager (Microsoft Visual Studio extension)

Update \*.dll references with original Project references. Works with C# and VB.NET projects.

## Why this extension was created?

A long time ago CPUs and hard drives were much slower. Developers used to split projects into multiple solutions and used DLL references in order for Visual Studio to work faster. Today we have CPUs with more cores and blazing fast solid state drives (SSDs). Developers can load hundreds of projects into one Visual Studio solution, which also is much better for refactoring and debugging. This extension can save time if you want to find and load original projects back into one Solution.

## How it works

Extension scans specified disk locations for C# (\*.csproj) and VB.NET (\*.vbproj) project files. Then, it will use that data to update \*.dll references with original Project references. During update process, when necessary, original Projects will be automatically added under "References" solution folder. You can find extension settings and data here: C:\ProgramData\Jocys.com\VS Reference Manager\

## Update DLL references with Project references

Before: Projects reference \*.dll assemblies:

&nbsp;&nbsp;&nbsp;&nbsp;<img alt="Solution From" src="ReferenceManager/Documents/Images/Solution_From.png" width="340" height="340">

After: Projects reference original Projects, which are also included under "References" solution folder:

&nbsp;&nbsp;&nbsp;&nbsp;<img alt="Solution To" src="ReferenceManager/Documents/Images/Solution_To.png" width="340" height="370">

## Screenshots

&nbsp;&nbsp;&nbsp;&nbsp;<img alt="Solution From" src="ReferenceManager/Documents/Images/Extension_Menu.png" width="390" height="138">

&nbsp;&nbsp;&nbsp;&nbsp;<img alt="Solution From" src="ReferenceManager/Documents/Images/Extension_Step1.png" width="690" height="720">

&nbsp;&nbsp;&nbsp;&nbsp;<img alt="Solution From" src="ReferenceManager/Documents/Images/Extension_Step2.png" width="690" height="720">
