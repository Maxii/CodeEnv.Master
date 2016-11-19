// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EffectsFolder.cs
// Easy access to Effects folder in Scene. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;

/// <summary>
/// Easy access to Effects folder in Scene. 
/// </summary>
public class EffectsFolder : AFolderAccess<EffectsFolder> {

    protected override bool IsRootGameObject { get { return true; } }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

