// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnityConstants.cs
// Static class of common constants specific to the Unity Engine.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using Microsoft.Win32;
    using UnityEngine;

    public static class UnityConstants {

        public static string UnityPathName {
            get {
                using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Unity Technologies\Unity Editor 3.x\Location")) {
                    //string[] values = registryKey.GetValueNames();    // empty, no name, only default value
                    string path = (string)registryKey.GetValue(null);
                    System.Diagnostics.Debug.WriteLine("UnityPathName = " + @path);
                    return @path;
                }
            }
        }

        public static string UnityEntryProjectDir {
            get { return Environment.ExpandEnvironmentVariables(@"%UnityEnvDir%UnityEntry\"); }
        }

        public static string UnityPocProjectDir {
            get { return Environment.ExpandEnvironmentVariables(@"%UnityEnvDir%UnityPOC\"); }
        }

        public static string UnityTrialsProjectDir {
            get { return Environment.ExpandEnvironmentVariables(@"%UnityEnvDir%UnityTrials\"); }
        }

        public const string MouseAxisName_Horizontal = "Mouse X";
        public const string MouseAxisName_Vertical = "Mouse Y";
        public const string MouseAxisName_ScrollWheel = "Mouse ScrollWheel";

        public const string KeyboardAxisName_Horizontal = "Horizontal";
        public const string KeyboardAxisName_Vertical = "Vertical";

        public const string Key_Escape = "escape";

        public static readonly Vector3 UniverseOrigin = Vector3.zero;

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


