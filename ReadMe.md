# IncaProSDK

As part of the INCA project, the **[IncaProSDK.sln](https://github.com/IGNF/inca-addins-for-arcgis/tree/main/releases)** solution was designed to develop a collection of applications (Console Applications, Add-ins) that interact with ArcGIS Pro through the .NET SDK.

```text
Language:              C#
Subject:               ArcGIS Pro SDK APIs
Project:               INCA
Contributor:           INCA Project Team
Organization:          IGN France
ArcGIS Pro:             3.1.3 (3.6.5 since inca-sdk-pro-2.4)
Visual Studio:          2022 (v17.6.4)
.NET Target Framework:  6.0 (8.0 since inca-sdk-pro-2.4)
```

### Prerequisites

- ArcGIS Pro 3.1.2 or later must be installed.
- Visual Studio 2022 (Community or Enterprise) and .NET 6.0 must be installed.
- [*OPTIONAL*] The `ArcGIS Pro SDK for .NET` and `ArcGIS Pro SDK for .NET (Utilities)` extensions for Visual Studio provide templates and other tools that make it easier to create ArcGIS Pro SDK Add-ins or ArcGIS Pro CoreHost Applications from scratch, among other things.

----------

## 1. **IncaProSDK (Library)**

This is the [library](.../main/rsc/code/inca-sdk-pro-[version]-compiled-for-pro-[verson_arcgis].zip/lib/IncaProSDK%20%28Library%29) in which the methods that interact directly with the Pro SDK are defined. It is mainly divided into three parts, or layers:

- **BLL**: Folder/namespace containing the classes that define the business logic and behavior of the INCA project for ArcGIS Pro SDK applications, such as Console Applications or Add-ins.
- **DAL**: Folder/namespace containing all the objects that define the data model of the INCA project for ArcGIS Pro SDK. Some objects may also provide methods that support specific ArcGIS Pro SDK reading, configuration, or behavior.
- **Commons**: Folder/namespace containing all the common and generic tools used by the INCA project for ArcGIS Pro SDK.

### Dependencies

- **Newtonsoft.Json** library (version *13.0.3*)
- **EPPlus** library (version *6.2.8*)
- **Microsoft.IO.RecyclableMemoryStream** library (version *2.2.1*)
- **System.ComponentModel.Annotations** library (version *5.0.0*)
- **log4net** library (version *3.1.0*)

> **NOTE**: Pay **VERY CLOSE ATTENTION** to the versions of each dependency listed in this document. We cannot guarantee that the `sdk-inca-pro` tools will work correctly if you choose to use other versions. Test thoroughly whenever you upgrade a version.

----------

## 2. Core Host APIs

Projects of the **"ArcGIS Pro CoreHost Application"** type make it possible to communicate with the ArcGIS Pro .NET SDK's **ArcGIS.Core.dll** assembly from Python code (or another language, if needed).

## 2.1 **ConcatAnnoProCoreHostAPI**

The [ConcatAnnoProCoreHostAPI](.../main/rsc/code/inca-sdk-pro-[version]-compiled-for-pro-[verson_arcgis].zip/corehost/ConcatAnnoProCoreHostAPI) executable concatenates place names character by character into a single annotation and inserts it into an Annotation Feature Class.

### Dependencies

- [IncaProSDK (Library)](.../main/rsc/code/inca-sdk-pro-[version]-compiled-for-pro-[verson_arcgis].zip/lib/IncaProSDK%20%28Library%29)

## 2.2 **ExportAnnoProCoreHostAPI**

The [ExportAnnoProCoreHostAPI](.../main/rsc/code/inca-sdk-pro-[version]-compiled-for-pro-[verson_arcgis].zip/corehost/ExportAnnoProCoreHostAPI) executable exports annotations generated in ArcGIS Pro to two Feature Classes (one for STRAIGHT annotations and one for CURVED annotations). These Feature Classes have the same structure as the annotations but are always of type POINT. The aim is to create simple rather than complex annotations: either straight or character-by-character.

### Dependencies

- [IncaProSDK (Library)](.../main/rsc/code/inca-sdk-pro-[version]-compiled-for-pro-[verson_arcgis].zip/lib/IncaProSDK%20%28Library%29)

----------

## 3. Add-ins

## 3.1 **IgnMaster**

An Add-in project that allows all the following Add-ins to display resources in a shared tab on the ArcGIS Pro ribbon. This Add-in must always be installed alongside the others.

## 3.2 **IncaDisplay**

An Add-in project that allows users to interact with the selected map (and its various layers) through several tools designed to improve productivity.

- <u>AttributeSets</u>: a tool for editing several fields of the same feature on the fly, with all settings defined in an XML file.
- <u>DisplayContents</u>: tools for changing the display or editing modes of the display pane on the fly:
  - A button to show all layers / a button to hide all layers.
  - A button to enable editing for all layers / a button to disable editing for all layers.
  - A button to enable selection for all layers / a button to disable selection for all layers.
- <u>DisplayManager</u>: a tool for saving or applying display, editing, and selection settings to a map's layers. Configurable through an XLSX file.
- <u>DisplayTools</u>: various tools that make ArcGIS easier to use:
  - A tool to resume or pause drawing on all open maps.
  - A tool to open a Google Street View or Satellite page in a web browser.
  - A tool to clear the selection on all maps.
  - A pane containing the nine most frequently used tools.
- <u>EditQueue</u>: a tool for browsing a preconfigured table of features from different classes.
- <u>QueryManager</u>: a tool for managing selections through preconfigured tables. Selections can be saved to tables and then browsed in the editing queue.

### Dependencies

- [IncaProSDK (Library)](.../main/rsc/code/inca-sdk-pro-[version]-compiled-for-pro-[verson_arcgis].zip/lib/IncaProSDK%20%28Library%29)
- **Microsoft.Xaml.Behaviors.Wpf** library (version *1.1.39*)

## 3.3 **IncaEditing**

An Add-in project providing additional editing tools, including:

- <u>CastFeatures</u>: a type-casting tool that transfers features from one class to another while complying with rules defined in an Excel configuration file.
- <u>ExportSymbols</u>: a tool for exporting symbols contained in feature layers to a style file (`.stylx`).
- <u>FeatureEdit</u>: various editing tools that make ArcGIS easier to use:
  - A tool for creating bridge parapets.
  - A tool for populating the visualization attribute with a single click.
  - A tool for creating the center circle and halfway line of football pitches.
  - A tool for creating side points.
- <u>OffsetSide</u>: a tool for changing the side on which an offset is applied.
- <u>TouristEdit</u>: two tools for creating callout boxes and underlines for place names on the map.

### Dependencies

- [IncaProSDK (Library)](.../main/rsc/code/inca-sdk-pro-[version]-compiled-for-pro-[verson_arcgis].zip/lib/IncaProSDK%20%28Library%29)
- **Microsoft.Xaml.Behaviors.Wpf** library (version *1.1.39*)
- **Newtonsoft.Json** library (version *13.0.3*)

## 3.1 **IncaLabelling**

An Add-in project designed to automatically configure Maplex labeling or replace text symbols in an annotation feature class.

- <u>MaplexEngineConfig</u>: the Maplex configuration tool automates the creation of Maplex label classes in a map using settings supplied in an Excel file.

  It takes a correctly configured Excel file as input, processes it, and configures the various options of the Maplex engine according to the values specified.
- <u>UpdateSymbols</u>: a tool for replacing the text symbols in an annotation feature class using text symbols stored in a style file (`.stylx`).

  It works around an ArcGIS Pro limitation that prevents users from modifying the existing symbol collection of an annotation feature class.

### Dependencies

- [IncaProSDK (Library)](.../main/rsc/code/inca-sdk-pro-[version]-compiled-for-pro-[verson_arcgis].zip/lib/IncaProSDK%20%28Library%29)
- **Microsoft.Xaml.Behaviors.Wpf** library (version *1.1.39*)

## 3.4 **IncaReintegration**

An Add-in project that reintegrates data from an extracted file geodatabase into the shared geodatabase.

It can also launch the conflict resolution console when conflicts are detected.

- <u>ConflictManager</u>: a tool for resolving any conflicts that may arise between the extracted geodatabase and the shared geodatabase during an update.

  An interface allows users to view and resolve conflicts.
- <u>Reintegration</u>: a tool for reintegrating data from a replica file geodatabase into the shared geodatabase from which it originated.
- <u>Update</u>: a tool for synchronizing an extracted replica (file geodatabase) with the shared geodatabase from which it originated.

  All changes made to the shared geodatabase since the replica was created are applied to the extracted file geodatabase.

### Dependencies

- [IncaProSDK (Library)](.../main/rsc/code/inca-sdk-pro-[version]-compiled-for-pro-[verson_arcgis].zip/lib/IncaProSDK%20%28Library%29)
- **Microsoft.Xaml.Behaviors.Wpf** library (version *1.1.39*)

> **NOTE**: Pay **VERY CLOSE ATTENTION** to the versions of each dependency listed in this document. We cannot guarantee that the `sdk-inca-pro` tools will work correctly if you choose to use other versions. Test thoroughly whenever you upgrade a version.

----------

# User Guide

### User Installation

1. Download the version (`.zip`) you need from the official release URL: https://github.com/IGNF/inca-addins-for-arcgis/tree/main/releases
2. Extract the downloaded file to the location of your choice.
3. Run `user_install.bat` as an administrator. It will automatically copy all files from the `\bin_inca_sdk` directory to the ArcGIS Pro `\bin` folder: `ARCGIS_PRO_INSTALL_DIRECTORY\bin\bin_inca\sdk` (**Example**: *C:\Program Files\ArcGIS\Pro\bin\bin_inca\sdk*) and install all available Add-ins from the `\add-ins` directory.

**Note**: If the `.dll` files and the `ConsoleArcGISProSDKIncaAPI.exe` file are not detected when the script runs, check that the path to the ArcGIS Pro `\bin` folder is included in the system `PATH` environment variable.

### Developer Installation

1. Retrieve the source code from the `repository`: [https://github.com/IGNF/inca-addins-for-arcgis/tree/main/rsc/codes](ssh://git@gitlab.ign.fr:10022/inca/inca-sdk-pro.git_ne_fonctionne_plus_???_à_vérifier)

**Note**: Always use the `develop` branch.

2. The first time, all `ArcGIS Pro and external library references` will be broken.
   - To repair the ArcGIS library references: right-click each project, select `Pro Fix References`, click `OK`, then click `Reload All`.
   - To repair external library references, use the NuGet Package Manager: right-click the solution, select `Manage NuGet Packages for solution...`, and download the packages.

   ***Tip***: The easiest approach is to uninstall the packages first and then reinstall them.

3. Run `Clean Solution` and then `Build Solution`. The build should complete successfully.

***Tip***: To avoid issues during a new installation, delete all files copied during the previous installation from the ArcGIS Pro `\bin\bin_inca\sdk` directory. When ArcGIS Pro starts, it initializes everything it finds in the `\bin` directory where ArcGIS Pro is installed.

### Automatically Creating a Distribution (Developer)

1. Run the `build_package.bat` file located in the root directory of `inca-sdk-pro`.
2. Indicate the release's version number in the standardized x.x.x format
3. Find the zipped installation file in the `releases` directory.

### Manually Creating a Distribution (Developer)

1. Build the entire solution in *Release* mode.
2. Publish the `ConcatAnnoProCoreHostAPI` and `ExportAnnoProCoreHostAPI` projects to generate self-contained `.exe` files: right-click each project and select `Publish`.
3. Copy the installation script, compiled Add-ins and DLLs, the executable files published in the previous step, and the DLLs required by the tools into a directory, following the structure of the most recent releases.
4. Zip the directory as-is for distribution.
