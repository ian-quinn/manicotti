# Manicotti :burrito:

![Revit API](https://img.shields.io/badge/Revit%20API-2022-red.svg)
![.NET](https://img.shields.io/badge/.NET-4.8-red.svg)

Revit add-in to build up model automatically based on DWG drawings. This is a toy project in progress and any test or joint development are welcome.  
```
manicotti
├ /Demo
│ ├ *.dwg     - DWG file for testing
│ └ *.gif     - Example movie
└ /Manicotti
  ├ /Properties  - Assembly info
  ├ /Resources
  │ ├ /ico    - Button icon files
  │ ├ /lib    - Teigha DLL files
  │ └ /rfa    - Default Revit family files
  ├ /Util
  │ └ *.cs    - Utility methods
  ├ /Views
  ├ ├ *.xaml  - View document
  │ └ *.cs
  ├ Manicotti.csproj  - Project configuration XML
  ├ Manicotti.sln     - VS solution file
  ├ App.cs    - Entry point
  ├ *.cs      - Class library
  └ Manicotti.addin   - Application manifest
```


## Test the Revit Add-in
Test the demo by Revit 2022 (en-US) with all default family libraries installed. The default folder for Revit Add-in is `C:\ProgramData\Autodesk\Revit\Addins\2022\` , or `C:\Users\$username$\AppData\Roaming\Autodesk\Revit\Addins\2022\`.  
The build events of the Visual Studio project will copy all necessary files to that directory after you build the source code. In case that you don't have VS, you need to create the plugin files manually under that directory. Like this:  
```
ProgramData\Autodesk\Revit\Addins\2022\
├ /Manicotti
│ ├ TD_*.dll        - Copy from /Resources/lib
│ └ Manicotti.dll   - Copy from /Demo
└ Manicotti.addin
```
Then start Revit and (video [ref](https://www.bilibili.com/video/BV17N4y1F7c1/?vd_source=9cd60edb139ebf7808403a2205ee49a1)):  
- Insert -> Link CAD -> `Demo\Link_floor.dwg`
- Manicotti -> Settings (check all .rfa files are loaded)
- Manicotti -> Build up model on all Levels
- Select the linked DWG in the floorplan view (process takes almost 1min)


## Compile the source code
The Manicotti add-in has been tested against Revit 2022. To apply it to other versions you need to rebuild it with correct .NET Framework. 
Revit 2022/2021 - .NET **4.8**   
Revit 2020/2019 - .NET 4.7  
Revit 2018 - .NET 4.6  
 
**REFERENCE** | The project hosts two external references, `RevitAPI.dll` and `RevitAPIUI.dll`. You can locate them under `...\Autodesk\Revit 2022\`  

**BUILD EVENTS** | Set additional macros in post-build event to copy the built files to the Revit add-in folder.
```
if exist "$(AppData)\Autodesk\REVIT\Addins\2022" copy "$(ProjectDir)*.addin" "$(AppData)\Autodesk\REVIT\Addins\2022"
if exist "$(AppData)\Autodesk\REVIT\Addins\2022" mkdir "$(AppData)\Autodesk\REVIT\Addins\2022\Manicotti"
copy "$(ProjectDir)$(OutputPath)*.dll" "$(AppData)\Autodesk\REVIT\Addins\2022\Manicotti"
copy "$(ProjectDir)Resources\rfa\*.rfa" "$(AppData)\Autodesk\REVIT\Addins\2022\Manicotti"
```

**DEBUG** | Within the project property, under DEBUG panel set external program as `...\Autodesk\Revit 2022\Revit.exe`


## What's new

This project uses Teigha for temporary development.  

To-do list moved to [TaskBoard](https://github.com/ian-quinn/manicotti/issues/1)  

A demo is online to build up the building model from CAD drawings. Only core components are covered (wall column window door room floor roof). For now the project still needs more cunning & robust algorithms to sort out layers/components and reshape the geometry, which will be the main theme in the next-phase coding.  

<img src="/Demo/Screenshot.jpg?raw=true">