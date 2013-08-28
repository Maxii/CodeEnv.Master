// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnityDebugConstants.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common.Unity {

    using System;
    using UnityEngine;

    public static class UnityDebugConstants {

        public static string UnityPocProjectDir {
            get { return Environment.ExpandEnvironmentVariables(@"%UnityEnvDir%UnityPOC\"); }
        }

        public static string UnityTrialsProjectDir {
            get { return Environment.ExpandEnvironmentVariables(@"%UnityEnvDir%UnityTrials\"); }
        }

        public static string CustomToolsDir {
            get { return Environment.ExpandEnvironmentVariables(@"%CustomToolsDir%"); }
        }

        public static Color IsSelectedColor {
            get { return GameColor.Green.ToUnityColor(); }
        }

        public static Color IsFocusedColor {
            get { return GameColor.Blue.ToUnityColor(); }
        }

        public static Color IsFocusAndSelectedColor {
            get { return GameColor.Yellow.ToUnityColor(); }
        }

    }
}

