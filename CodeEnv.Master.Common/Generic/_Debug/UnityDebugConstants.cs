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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

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

        public static GameColor SelectedColor {
            get { return GameColor.Green; }
        }

        public static GameColor FocusedColor {
            get { return GameColor.Yellow; }
        }

        public static GameColor GeneralHighlightColor {
            get { return GameColor.White; }
        }

        public static GameColor SectorHighlightColor {
            get { return GameColor.Yellow; }
        }

    }
}

