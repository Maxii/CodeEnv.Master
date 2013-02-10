// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnityUtility.cs
// Holds Unity-specific static utility methods.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using UnityEngine;
    using UnityEditor;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.Common.Unity;
    using System.IO;

    /// <summary>
    /// Holds Uniity-specific static utility methods.
    /// </summary>
    public static class UnityUtility {

        /// <summary>
        //	Utiliity method that makes it easy to create, name and place unique new ScriptableObject asset files.
        /// </summary>
        public static void CreateScriptableObjectAsset<T>() where T : ScriptableObject {
            T asset = ScriptableObject.CreateInstance<T>();

            // the path to whatever is selected in the project pane
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path == string.Empty) {
                // if nothing is selected, put the new asset in the Assets folder
                path = UnityConstants.AssetFolderName;
            }
            else if (Path.GetExtension(path) != string.Empty) {
                // the item selected has an extension so it is a file. Get rid of the filename so the path is to the containing folder
                path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), string.Empty);
            }

            // generates a unique path and name for the asset. A sequential number is added if not a unique path/name combo
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New " + typeof(T).ToString() + UnityConstants.AssetExtension);

            AssetDatabase.CreateAsset(asset, assetPathAndName);

            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }

    }
}

