// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ScriptableObjectTrialAsset.cs
// Demo class that enables creation of ScriptableObject asset files.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Resources;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// Demo class that establishes an Assets/Create/&ltYourScriptableObjectAssetClassName&gt 
/// MenuItem that enables creation of YourScriptableObjectAssetClassName asset files.
/// </summary>
public class ScriptableObjectTrialAsset {

    [MenuItem(UnityConstants.AssetsCreateMenuItem + "ScriptableObjectTrial")]
    public static void CreateAsset() {
        UnityUtility.CreateScriptableObjectAsset<ScriptableObjectTrial>();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

