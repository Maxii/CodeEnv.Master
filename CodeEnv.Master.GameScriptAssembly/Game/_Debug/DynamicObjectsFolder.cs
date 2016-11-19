// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DynamicObjectsFolder.cs
// Easy access to DynamicObjects folder in Scene.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Easy access to DynamicObjects folder in Scene.
/// </summary>
public class DynamicObjectsFolder : AFolderAccess<DynamicObjectsFolder>, IDynamicObjectsFolder {

    protected override bool IsRootGameObject { get { return true; } }

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        References.DynamicObjectsFolder = Instance;
    }

    protected override void Cleanup() {
        References.DynamicObjectsFolder = null;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}


