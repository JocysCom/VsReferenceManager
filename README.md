# Jocys.com VS Reference Manager

Scan and update DLL references with Project references. Works with C# and VB.NET projects.

## Why extension was created?

A long time ago CPUs and hard drivers were much slower. Developers used to split projects into multiple solutions and used DLL references in order for Visual Studio to work faster. Today we have CPUs with more cores and blazing fast solid state drives (SSDs). Developers can load hundreds of projects into one Visual Studio solution, which also is much better for refactoring and debugging. This extension can scan your disk, find original projects of DLLs, add them to Solution and replace DLLs references with original Project references.

## How it works

Extension scans specified locations with projects. Then it will use that date to replace project DLL references with Project references. Projects will be added to solution under "References" solution folder.

## Update DLL references with Project references

Before: Solution's Project4 references two DLL assemblies:

&nbsp;&nbsp;&nbsp;&nbsp;<img alt="Solution From" src="ReferenceManager/Documents/Images/Solution_From.png" width="340" height="340">

After: Solution includes two projects and Project4 references them:

&nbsp;&nbsp;&nbsp;&nbsp;<img alt="Solution To" src="ReferenceManager/Documents/Images/Solution_To.png" width="340" height="370">

## Screenshots

&nbsp;&nbsp;&nbsp;&nbsp;<img alt="Solution From" src="ReferenceManager/Documents/Images/Extension_Menu.png" width="390" height="138">

&nbsp;&nbsp;&nbsp;&nbsp;<img alt="Solution From" src="ReferenceManager/Documents/Images/Extension_Step1.png" width="690" height="720">

&nbsp;&nbsp;&nbsp;&nbsp;<img alt="Solution From" src="ReferenceManager/Documents/Images/Extension_Step2.png" width="690" height="720">
