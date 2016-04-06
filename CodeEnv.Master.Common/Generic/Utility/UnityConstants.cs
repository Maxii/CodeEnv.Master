﻿// --------------------------------------------------------------------------------------------------------------------
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

        [Obsolete]
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
        /// Gets the unity entry project dir. 
        /// Note:  The project folder can always be acquired by System.IO.Directory.GetCurrentDirectory as the editor requires that
        /// the current working directory be set to the project folder at all times.
        /// Warning: This only works on my systems which have UnityEnvDir defined as an environment variable.
        /// </summary>
        [Obsolete]
        public static string UnityEntryProjectDir {
            get { return Environment.ExpandEnvironmentVariables(@"%UnityEnvDir%UnityEntry\"); }
        }

        // Note: when using relative vs absolute path notation, Unity current directory is always the Project root working directory.
        // The project folder can always be acquired by System.IO.Directory.GetCurrentDirectory as the editor requires that
        // the current working directory be set to the project folder at all times.
        // Application.dataPath returns the absolute path to where the project data resides depending on the platform. In the UnityEditor,
        // this path is the path to the Assets folder in the current project.
        public static string DataLibraryDir {
            get { return Application.dataPath + @"\..\DataLibrary\"; }  // \..\ moves up one directory level
        }

        public const string MouseAxisName_Horizontal = "Mouse X";
        public const string MouseAxisName_Vertical = "Mouse Y";
        public const string MouseAxisName_ScrollWheel = "Mouse ScrollWheel";

        public const string KeyboardAxisName_Horizontal = "Horizontal";
        public const string KeyboardAxisName_Vertical = "Vertical";

        public const string Key_Escape = "escape";

        #region Standard Shader Feature Keywords

        // Usage: Material.EnableKeyword(keyword) to enable this variant of the Standard Shader during runtime
        // Note: One would expect a keyword for OpaqueRenderingMode too, but it is not needed as it is the default

        public const string StdShader_RenderModeKeyword_CutoutTransparency = "_ALPHATEST_ON";

        public const string StdShader_RenderModeKeyword_FadeTransparency = "_ALPHABLEND_ON";

        public const string StdShader_RenderModeKeyword_TransparentTransparency = "_ALPHAPREMULTIPLY_ON";

        public const string StdShader_MapKeyword_Normal = "_NORMALMAP";

        public const string StdShader_MapKeyword_Emission = "_EMISSION";

        public const string StdShader_MapKeyword_Height = "_PARALLAXMAP";

        public const string StdShader_MapKeyword_SecondaryDetail = "_DETAIL_MULX2";

        public const string StdShader_MapKeyword_Metallic = "_METALLICGLOSSMAP";

        public const string StdShader_MapKeyword_Specular = "_SPECCGLOSSMAP";

        #endregion

        #region Standard Shader Property Names

        // Usage: Material.SetXXX(PropertyName, value)  during runtime.
        // SetXXX: SetFloat(string, float), SetInt(string, int), SetColor(string, Color), SetTexture(string, Texture)

        public const string StdShader_Property_AlbedoTexture = "_MainTex";
        public const string StdShader_Property_AlbedoColor = "_Color";
        public const string StdShader_Property_AlphaCutoffFloat = "_Cutoff";

        public const string StdShader_Property_MetallicTexture = "_MetallicGlossMap";
        public const string StdShader_Property_MetallicFloat = "_Metallic";
        public const string StdShader_Property_SmoothnessFloat = "_Glossiness";

        public const string StdShader_Property_NormalTexture = "_BumpMap";
        public const string StdShader_Property_NormalScaleFloat = "_BumpScale";

        public const string StdShader_Property_HeightTexture = "_ParallaxMap";
        public const string StdShader_Property_HeightScaleFloat = "_Parallax";

        public const string StdShader_Property_OcclusionTexture = "_OcclusionMap";
        public const string StdShader_Property_OcclusionStrengthFloat = "_OcclusionStrength";

        public const string StdShader_Property_EmissionTexture = "_EmissionMap";
        public const string StdShader_Property_EmissionColor = "_EmissionColor";
        // StdShader_Property_EmissionBrightnessFloat = ??? UNCLEAR

        public const string StdShader_Property_DetailMaskTexture = "_DetailMask";

        public const string StdShader_Property_SecondaryDetailAlbedoTexture = "_DetailAlbedoMap";
        public const string StdShader_Property_SecondaryDetailNormalTexture = "_DetailNormalMap";
        public const string StdShader_Property_SecondaryDetailNormalScaleFloat = "_DetailNormalMapScale";

        #endregion

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

        public const string NguiEmbeddedColorFormat = "[{0:000000}]";
        public const string NguiEmbeddedColorTerminator = "[-]";

        /// <summary>
        /// My default precision for Unity float equality comparisons.
        /// 1M times less precise than Unity's built in == comparison
        /// </summary>
        public const float FloatEqualityPrecision = .0001F;

        /// <summary>
        /// The finest precision allowed for use in determining angle equality.
        /// This is due to floating point precision limitations within Unity which
        /// uses Quaternions for rotations, even when the rotations are expressed
        /// using Vector3 directions as Unity uses Quaternions internally for all
        /// rotations.
        /// <remarks>http://answers.unity3d.com/questions/1036566/quaternionangle-is-inaccurate.html#answer-1162822 
        /// </remarks>
        /// </summary>
        public const float AngleEqualityPrecision = 0.04F;

    }
}

