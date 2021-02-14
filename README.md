# Manicotti :burrito:
Revit add-in to establish building information model automatically based on DWG drawings. A demo in progress.  
```
manicotti
├ /Demo
│ ├ Manicotti.addin  - Manifest file
│ ├ Manicotti.dll  - Add-in DLL
│ ├ *.dwg  - DWG file for testing
│ ├ *.rfa  - Revit family file for testing
│ ├ *.dyn  - Dynamo file for testing
│ └ *.gif  - Example movie
└ /Manicotti
  ├ /Properties  - Assembly info XML
  ├ /ico  - Button icon files
  ├ /lib  - Teigha DLL files
  ├ Manicotti.csproj  - Project configuration XML
  ├ Manicotti.sln  - VS solution file
  ├ App.cs  - Class library for ribbon button
  ├ *.cs  - Class library
  └ Manicotti.addin  - Application manifest XML
```

## Test the Revit Add-in
The default Revit Add-in folder is `C:\ProgramData\Autodesk\Revit\Addins\2020\` for all users or `C:\Users\$username$\AppData\Roaming\Autodesk\Revit\Addins\2020\` for individual user.
- Copy `Manicotti.addin` and `Manicotti.dll` to the Revit Add-in folder
- Insert -> Link CAD -> `Link_floor.dwg`
- Manicotti -> Build up model on all Levels
- Manual select the linked DWG in the view (process takes almost 7s)


## Test the Dynamo workflow
The Dynamo workflow has been abandoned due to its limitations.
- File -> New Project -> Select Template: `C:\ProgramData\Autodesk\RVT 2020\Templates\US Metric\DefaultMetric.rte`
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

*Further development...*  
To craft your add-in from scratch please use the template by Jeremy Tammik. ZIP the template files under `/cs` of [VisualStudioRevitAddinWizard 2020.0.0.0](https://github.com/jeremytammik/VisualStudioRevitAddinWizard/releases/tag/2020.0.0.0) 
as `Revit2020AddinWizard.zip`, and place it here:
`C:\Users\$username$\Documents\Visual Studio 2017\Templates\ProjectTemplates\`  
References: Jeremy's blog [1](https://thebuildingcoder.typepad.com/blog/2015/05/autodesk-university-q1-adn-labs-and-wizard-update.html#5) [2](https://thebuildingcoder.typepad.com/blog/2019/04/revit-2020-c-and-vb-visual-studio-add-in-wizards.html)


## Resources

**DOCUMENT** | [Revit API 2020](https://www.revitapidocs.com/2020/) | [Dynamo Dictionary **2.x**](https://dictionary.dynamobim.com/2/) | [Dynamo Primer](https://primer.dynamobim.org/en/index.html) | [Design Scripts Guide](https://dynamobim.org/wp-content/links/DesignScriptGuide.pdf)

**TUTORIAL** | [Hello World Demo](http://aectechy.com/stepbystep-guide-to-your-first-revit-plugin/) | [Group Copy Demo](https://knowledge.autodesk.com/support/revit-products/learn-explore/caas/simplecontent/content/lesson-1-the-basic-plug.html) | [Revit API Tutorials](https://www.youtube.com/channel/UCHqe6I5GKoUEH4XlPw2VGGw/videos)


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

Currently working on axis generation for sub-surface. Take the DOOR blocks for example, one of the bounding box edges (green polygon) will be selected as the axis (scarlet line). The axes of sub-surfaces will be merged into wall axes to create continuous walls.  
<img src="/Demo/DoorAxis.jpg?raw=true">