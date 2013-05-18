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
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

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

        // Unity current directory is always the Project root working directory
        public static readonly string DebugSettingsPath = @".\Assets\DataLibrary\DebugSettings.xml";

    }
}

