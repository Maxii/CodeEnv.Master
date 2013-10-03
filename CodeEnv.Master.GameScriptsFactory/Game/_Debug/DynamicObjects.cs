// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DynamicObjects.cs
// Easy access to DynamicObjects folder in Scene.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Easy access to DynamicObjects folder in Scene.
/// </summary>
public class DynamicObjects : AFolderAccess<DynamicObjects> {

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}


