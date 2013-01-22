// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ScriptableObjectTrial.cs
// COMMENT - one line to give a brief idea of what this file does.
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

[Serializable]
/// <summary>
/// Demo class in support of creating ScriptableObject asset files.
/// </summary>
public class ScriptableObjectTrial : ScriptableObject {

    public string trialName = "MyTrialName";
    public int trialValue = 1;


    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

