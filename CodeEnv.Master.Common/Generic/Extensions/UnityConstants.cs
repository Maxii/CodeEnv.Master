// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnityConstants.cs
// Constant values specific to the Unity engine and environment.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using Microsoft.Win32;
    using UnityEngine;

    /// <summary>
    /// Constant values specific to the Unity engine and environment.
    /// </summary>
    public static class UnityConstants {        // the @ in @"string" means that the string between " is to be interpreted as a string literal requiring no escape characters

        public static string UnityInstallPath {
            get {
                using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Unity Technologies\Unity Editor 3.x\Location")) {
                    //string[] values = registryKey.GetValueNames();    // empty, no name, only default value
                    string path = (string)registryKey.GetValue(null);   // gets the default value
                    // System.Diagnostics.Debug.WriteLine("UnityInstallPath = " + @path);
                    return @path;
                }
            }
        }

        /// <summary>
        /// Gets the unity entry project dir. This only works on my systems which have UnityEnvDir defined as an environment variable
        /// </summary>
        [Obsolete]
        public static string UnityEntryProjectDir {
            get { return Environment.ExpandEnvironmentVariables(@"%UnityEnvDir%UnityEntry\"); }
        }

        // Note: when using relative vs absolute path notation, Unity current directory is always the Project root working directory
        // The project folder can always be acquired by System.IO.Directory.GetCurrentDirectory as the editor requires that
        // the current working directory be set to the project folder at all times.
        public static string DataLibraryDir {
            get { return Application.dataPath + @"\..\DataLibrary\"; }  // \..\ moves up one directory level
        }

        public const string MouseAxisName_Horizontal = "Mouse X";
        public const string MouseAxisName_Vertical = "Mouse Y";
        public const string MouseAxisName_ScrollWheel = "Mouse ScrollWheel";

        public const string KeyboardAxisName_Horizontal = "Horizontal";
        public const string KeyboardAxisName_Vertical = "Vertical";

        public const string Key_Escape = "escape";

        // Common Texture names used by Unity's builtin shaders
        public const string MainDiffuseTexture = "_MainTex";
        public const string NormalMapTexture = "_BumpMap";
        public const string ReflectionCubeMapTexture = "_Cube";

        // Common Color names used by Unity's builtin shaders - use with material.SetColor(name, Color)
        public const string MaterialColor_Main = "_Color";   // the main color of a material, can also be accessed via color property
        public const string MaterialColor_Specular = "_SpecColor"; // the specular color of a material, used in specular/vertexlit shaders
        public const string MaterialColor_Emissive = "_Emission"; // the emissive color of a material, used in vertexlit shaders
        public const string MaterialColor_Reflection = "_ReflectColor";  // the reflection color of a material, used in reflective shaders
        public const string MaterialColor_Outline = "_OutlineColor"; // the outline color of a material, typically used in toon shaders?

        public const string AssetFolderName = "Assets";
        public const string AssetExtension = ".asset";

        public const string AssetsCreateMenuItem = "Assets/Create/";

        public const string InspectorWindowName = "Inspector";

    }
}

