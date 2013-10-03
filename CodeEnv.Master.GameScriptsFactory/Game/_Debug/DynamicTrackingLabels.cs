// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DynamicTrackingLabels.cs
// Easy access to the DynamicTrackingLabels folder in the Scene.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Easy access to the DynamicTrackingLabels folder in the Scene.
/// </summary>
public class DynamicTrackingLabels : AFolderAccess<DynamicTrackingLabels> {

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

