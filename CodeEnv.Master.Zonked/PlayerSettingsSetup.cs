// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlayerSettingsSetup.cs
// Editor class that initializes PlayerSettings as the Editor launches.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
//using CodeEnv.Master.Common;
using UnityEditor;

/// <summary>
/// Editor class that initializes PlayerSettings as the Editor launches.
/// <see cref="http://docs.unity3d.com/Documentation/Manual/RunningEditorCodeOnLaunch.html"/>
/// </summary>
//[InitializeOnLoad]
public class PlayerSettingsSetup {

    static PlayerSettingsSetup() {
        BuildTargetGroup[] platformTargets = new BuildTargetGroup[1] { BuildTargetGroup.Standalone };
        IList<string> definesToInclude = new List<string>() { "DEBUG_ERROR", "DEBUG_WARN" };
        if (EditorWindow.GetWindow<DebugSettingsWindow>()._isDebugLogEnabled) {
            definesToInclude.Add("DEBUG_LOG");
        }
        //if (DebugSettings.Instance.EnableVerboseDebug) {
        //    definesToInclude.Add("DEBUG_LOG");
        //}
        UnityEditorUtility.ResetConditionalCompilation(platformTargets, definesToInclude.ToArray<string>());
    }

}

