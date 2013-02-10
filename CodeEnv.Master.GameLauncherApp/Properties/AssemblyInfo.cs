using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;

// Note: Shared assembly information is specified in SharedAssemblyInfo.cs
// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("CodeEnv.Master.GameLauncherApp")]

// The AssemblyCultureAttribute is used by compilers to distinguish between a main assembly and a satellite assembly. 
// A main assembly contains code and the neutral culture's resources. A satellite assembly contains only resources for a 
// particular culture, as in [assembly:AssemblyCulture("de")]. Putting this attribute on an assembly and using something 
// other than the empty string ("") for the culture name will make this assembly look like a satellite assembly, rather than
// a main assembly that contains executable code. Labeling a traditional code library with this attribute will break it,
// because no other code will be able to find the library's keyValuePair points at runtime.
[assembly: AssemblyCulture("")]

// Instructs the resource fallback process to search for the default culture resource file in a satellite assembly rather than the Main assembly
// [assembly: NeutralResourcesLanguageAttribute("en", UltimateResourceFallbackLocation.Satellite)]
// Above won't reliably generate an 'en' Satellite assembly, resulting in a Manifest exception
[assembly: NeutralResourcesLanguageAttribute("en")]

//In order to begin building localizable applications, set 
//<UICulture>CultureYouAreCodingWith</UICulture> in your .csproj file
//inside a <PropertyGroup>.  For example, if you are using US english
//in your source files, set the <UICulture> to en-US.  

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
    //(used if a resource is not found in the page, 
    // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
    //(used if a resource is not found in the page, 
    // app, or any theme specific resource dictionaries)
)]

