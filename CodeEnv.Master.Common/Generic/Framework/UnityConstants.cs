// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnityConstants.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using Microsoft.Win32;

    public static class UnityConstants {

        public static string UnityPathName {
            get {
                using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Unity Technologies\Unity Editor 3.x\Location")) {
                    //string[] values = registryKey.GetValueNames();    // empty, no name, only default value
                    string path = (string)registryKey.GetValue(null);   // gets the default value
                    // System.Diagnostics.Debug.WriteLine("UnityPathName = " + @path);
                    return @path;
                }
            }
        }

        public static string UnityEntryProjectDir {
            get { return Environment.ExpandEnvironmentVariables(@"%UnityEnvDir%UnityEntry\"); }
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

        public const string AssetFolderName = "Assets";
        public const string AssetExtension = ".asset";

        public const string AssetsCreateMenuItem = "Assets/Create/";

        public const string InspectorWindowName = "Inspector";

    }
}

