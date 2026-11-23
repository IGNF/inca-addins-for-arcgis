using System.Resources;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("IGN.Inca.ArcGISPro.SDK")]
[assembly: AssemblyDescription("Assembly containing classes and tools that with get easier the interaction of ArcGIS Pro SDK")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("IGN")]
[assembly: AssemblyProduct("IGN.Inca.ArcGISPro.SDK")]
[assembly: AssemblyCopyright("Copyright © IGN 2021")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("27140026-d1e1-490d-bb2c-1d3e34b631ff")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("2.2")]
[assembly: AssemblyFileVersion("2.2")]
[assembly: NeutralResourcesLanguage("en")]

// Load "log4net" configuration
[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]