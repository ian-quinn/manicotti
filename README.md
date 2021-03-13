# Manicotti :burrito:

![Revit API](https://img.shields.io/badge/Revit%20API-2020-red.svg)
![.NET](https://img.shields.io/badge/.NET-4.7-red.svg)

Revit add-in to build up model automatically based on DWG drawings. This is a toy project in progress and any test or joint development are welcome.  
```
manicotti
├ /Demo
│ ├ *.dwg  - DWG file for testing
│ ├ *.dyn  - Dynamo file for testing
│ └ *.gif  - Example movie
└ /Manicotti
  ├ /Properties  - Assembly info
  ├ /Resources
  │ ├ /ico  - Button icon files
  │ ├ /lib  - Teigha DLL files
  │ └ /rfa  - Default Revit family files
  ├ Manicotti.csproj  - Project configuration XML
  ├ Manicotti.sln  - VS solution file
  ├ App.cs  - Class library for ribbon button
  ├ *.cs  - Class library
  └ Manicotti.addin  - Application manifest XML
```

## Test the Revit Add-in
It's better to test the demo by starting Revit under English environment and with all default family libraries installed. The default Revit Add-in folder is `C:\ProgramData\Autodesk\Revit\Addins\2020\` for all users or `C:\Users\$username$\AppData\Roaming\Autodesk\Revit\Addins\2020\` for individual user.
- Copy `Manicotti.addin` and `Manicotti.dll` to the Revit Add-in folder
- Insert -> Link CAD -> `Link_floor.dwg`
- Manicotti -> Build up model on all Levels
- Manual select the linked DWG in the floorplan view (process takes almost 1min)


## Do not test this Dynamo workflow
The Dynamo workflow is only a hint to the wall extrusion problem. Due to the limitation of speed it has been aborted.
- Insert -> Import CAD -> `Demo.dwg`
- Insert -> Load Family -> `Column_demo.rfa`
- Modify -> Explode -> Full Explode
- Manage -> Dynamo -> Open -> `Demo.dyn`
- Within Dynamo Packages -> Manage Packages -> Make sure custom nodes `Chynamo` & `Data-Shapes` are installed
- Within Dynamo click 'RUN' (takes almost 5s)


## Compile the source code
The Manicotti add-in has only been tested against Revit 2020. To apply it to other versions you need to rebuild it under the correspoinding .NET Framework.
Revit 2021 - .NET 4.8 | Revit 2020/2019 - .NET **4.7** | Revit 2018 - .NET 4.6  
Revit 2017/2016/2015 - .NET 4.5 | Revit 2014 - .NET 4.0
 
**REFERENCE** | The project hosts two external references, `RevitAPI.dll` and `RevitAPIUI.dll`. You can locate them under `...\Autodesk\Revit 2020\`  

**BUILD EVENTS** | Set additional macros in post-build event to copy the built files to the Revit add-in folder. `if exist "$(AppData)\Autodesk\REVIT\Addins\2020" copy "$(ProjectDir)*.addin" "$(AppData)\Autodesk\REVIT\Addins\2020"` & `if exist "$(AppData)\Autodesk\REVIT\Addins\2020" copy "$(ProjectDir)$(OutputPath)*.dll" "$(AppData)\Autodesk\REVIT\Addins\2020"`  

**DEBUG** | Within the project property, under DEBUG panel set external program as `...\Autodesk\Revit 2020\Revit.exe`

This project uses [VisualStudioRevitAddinWizard 2020.0.0.0](https://github.com/jeremytammik/VisualStudioRevitAddinWizard/releases/tag/2020.0.0.0).  
This project uses Teigha for temperary development.  

## What's new

To-do list moved to [TaskBoard](https://github.com/ian-quinn/manicotti/issues/1)  

Demos  
<div align=right>
<table>
  <tr>
    <td><img src="/Demo/DetectRegion.gif?raw=true" alt="DetectRegion" width="400"/></a></td>
    <td><img src="/Demo/AllocateInfo.gif?raw=true" alt="AllocateInfo" width="400"/></a></td>
  </tr>
</table>
</div>

A comprehensive demo is online to build up the building model from CAD drawings. All necessary components are covered (wall column window door room floor roof). For now the project still needs more cunning & robust algorithms to sort out layers/components and reshape the geometry, which will be the main theme in the next-phase coding.  

<img src="/Demo/Screenshot.jpg?raw=true">