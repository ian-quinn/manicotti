# Manicotti :burrito:
Revit add-in to extrude walls from CAD drawings. A demo in progress.


## Test the Dynamo workflow
The files you need are cached within $/Demo
- File -> New Project -> Select Template: `C:\ProgramData\Autodesk\RVT 2020\Templates\US Metric\DefaultMetric.rte`
- Insert -> Import CAD -> `Demo.dwg`
- Insert -> Load Family -> `Column_demo.rfa`
- Modify -> Explode -> Full Explode
- Manage -> Dynamo -> Open -> `Demo.dyn`
- Within Dynamo Packages -> Manage Packages -> Make sure custom nodes `Chynamo` & `Data-Shapes` are installed
- Within Dynamo click 'RUN' (takes almost 5s)


## Test the Revit Add-in
The files you need are cached within $/Demo. The default Revit Add-in folder is `C:\ProgramData\Autodesk\Revit\Addins\2020\` for all users or `C:\Users\$username$\AppData\Roaming\Autodesk\Revit\Addins\2020\` for individual user.
- Copy `Manicotti.addin` and `Manicotti.dll` to the Revit Add-in folder
- Insert -> Import CAD -> `Demo.dwg`
- Insert -> Load Family -> `Column_demo.rfa`
- Modify -> Explode -> Full Explode
- Manicotti -> Extrude walls (takes almost 2s)


<img src="/Demo/Demo.gif?raw=true">


## About the Dynamo Packages
Default installation folder: `C:\Users\$username$\AppData\Roaming\Dynamo\Dynamo Revit\2.1\packages\` (Dynamo Revit) `C:\Users\$username$\AppData\Roaming\Dynamo\Dynamo Core\2.8\packages\` (Dynamo Sandbox)  
├ /bin	- houses .dll files created with C# or Zero-Touch libraries  
├ /dyf	- custom nodes  
├ /extra	- any additional files such as .svg / .xls / .dyn  
└ pkg.json	- text file, defining the package settings  


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

Template structure  
├ /Properties  -  
├ /References  - External dependencies RevitAPI.dll & RevitAPIUI.dll  
├ App.cs	- Class library  
├ Command.cs	- Class library  
└ $projectname$.addin	- Application manifest file  


## Resources

**DOCUMENT** | [Revit API 2020](https://www.revitapidocs.com/2020/) | [Dynamo Dictionary **2.x**](https://dictionary.dynamobim.com/2/) | [Dynamo Primer](https://primer.dynamobim.org/en/index.html) | [Design Scripts Guide](https://dynamobim.org/wp-content/links/DesignScriptGuide.pdf)

**TUTORIAL** | [Hello World Demo](http://aectechy.com/stepbystep-guide-to-your-first-revit-plugin/) | [Group Copy Demo](https://knowledge.autodesk.com/support/revit-products/learn-explore/caas/simplecontent/content/lesson-1-the-basic-plug.html) | [Revit API Tutorials](https://www.youtube.com/channel/UCHqe6I5GKoUEH4XlPw2VGGw/videos)


## To-do

- [x] <DetectRegion> Create closed CurveArray based on intersected strays
- [ ] Adhere windows to walls
- [ ] Create families for multi-thickness walls

<img src="/Demo/DetectRegion.gif?raw=true">