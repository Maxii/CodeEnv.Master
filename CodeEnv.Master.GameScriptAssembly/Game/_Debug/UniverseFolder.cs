// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseFolder.cs
//  Easy access to Universe folder in Scene.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;

/// <summary>
///  Easy access to Universe folder in Scene.
/// </summary>
public class UniverseFolder : AFolderAccess<UniverseFolder> {

    protected override bool IsRootGameObject { get { return true; } }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

