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
        /// Gets the unity entry project directory. 
        /// Note:  The project folder can always be acquired by System.IO.Directory.GetCurrentDirectory as the editor requires that
        /// the current working directory be set to the project folder at all times.
        /// Warning: This only works on my systems which have UnityEnvDir defined as an environment variable.
        /// </summary>
        [Obsolete]
        public static string UnityEntryProjectDir {
            get { return Environment.ExpandEnvironmentVariables(@"%UnityEnvDir%UnityEntry\"); }
        }

        private static string _dataLibraryDir;
        /// <summary>
        /// Gets the directory path to the DataLibrary, aka where all the Xml values are stored.
        /// <remarks>When using relative vs absolute path notation, Unity current directory is always the Project root working directory.
        /// The project folder can always be acquired by System.IO.Directory.GetCurrentDirectory as the editor requires that
        /// the current working directory be set to the project folder at all times.
        /// Application.dataPath returns the absolute path to where the project data resides depending on the platform. In the UnityEditor,
        /// this path is the path to the Assets folder in the current project.</remarks>
        /// <remarks>Avoids calling Application.dataPath more than once. As of Unity 5.x it can only be called from
        /// a MonoBehaviour Awake() or Start() event. This way GameManager.Awake() can indirectly initialize the first class that uses
        /// it (an AXmlReader) and then it won't be called again.</remarks>
        /// </summary>
        public static string DataLibraryDir {
            get {
                if (_dataLibraryDir == null) {
                    _dataLibraryDir = Application.dataPath + @"\..\DataLibrary\";  // \..\ moves up one directory level
                }
                return _dataLibraryDir;
            }
        }
        //public static string DataLibraryDir {
        //    get { return Application.dataPath + @"\..\DataLibrary\"; }  // \..\ moves up one directory level
        //}

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

        // Common Texture names used by Unity's built-in shaders
        public const string MainDiffuseTexture = "_MainTex";
        public const string NormalMapTexture = "_BumpMap";
        public const string ReflectionCubeMapTexture = "_Cube";

        // Common Color names used by Unity's built-in shaders - use with material.SetColor(name, Color)
        public const string MaterialColor_Main = "_Color";   // the main color of a material, can also be accessed via color property
        public const string MaterialColor_Specular = "_SpecColor"; // the specular color of a material, used in specular/vertex-lit shaders
        public const string MaterialColor_Emissive = "_Emission"; // the emissive color of a material, used in vertex-lit shaders
        public const string MaterialColor_Reflection = "_ReflectColor";  // the reflection color of a material, used in reflective shaders
        public const string MaterialColor_Outline = "_OutlineColor"; // the outline color of a material, typically used in toon shaders?

        public const string AssetFolderName = "Assets";
        public const string AssetExtension = ".asset";

        public const string AssetsCreateMenuItem = "Assets/Create/";

        public const string InspectorWindowName = "Inspector";

        public const string NguiEmbeddedColorInitiatorFormat = "[{0:000000}]";
        public const string NguiEmbeddedColorTerminator = "[-]";
        // 6.16.18 Without these, the embedded color simply 'tints' the existing font base tint (UILabel.ColorTint setting)
        // rather than replacing it. This works fine if the base tine is white, but if not... Use these to fully replace the
        // color of the font with the embedded color no matter what the base tint value in UILabel is.
        // http://www.tasharen.com/forum/index.php?topic=12104.0
        public const string NguiBaseTintRemovalInitiator = "[c]";
        public const string NguiBaseTintRemovalTerminator = "[/c]";

        /// <summary>
        /// My default precision for Unity float equality comparisons.
        /// 1M times less precise than Unity's built in == comparison.
        /// WARNING: Do not use Mathf.Epsilon for any comparisons except with 0F.
        /// see http://docs.unity3d.com/ScriptReference/Mathf.Epsilon.html
        /// </summary>
        public const float FloatEqualityPrecision = .0001F;

        /// <summary>
        /// The finest precision allowed for use in determining angle equality. This is due to floating point 
        /// precision limitations within Unity which uses Quaternions for rotations, even when the rotations 
        /// are expressed using Vector3 directions as Unity uses Quaternions internally for all rotations.
        /// <remarks>2.15.17 At 0.04, getting Quaternion.IsSame() failures where actualDeviation won't go below 0.05595291
        /// rather than the 0.03956468 seen before and noted in the URL below. I can find no one else having this 
        /// experience with this new value. IMPROVE As before, the actual rotated degrees vs the desired rotated degrees are
        /// almost identical, within .0001 degrees so next step would be to convert to desired degrees and actual degrees
        /// and compare those. That's certainly more work for a high frequency comparison and it will still suffer
        /// from the same problem when desired and actual are very small values.</remarks>
        /// <remarks>5.15.17 Failed with actual deviation of 0.06852804 with identical 142.0884 rotations. Increased
        /// precision to 0.07F.</remarks>
        /// <remarks>http://answers.unity3d.com/questions/1036566/quaternionangle-is-inaccurate.html#answer-1162822 
        /// </remarks>
        /// </summary>
        public const float AngleEqualityPrecision = 0.07F;

    }
}

