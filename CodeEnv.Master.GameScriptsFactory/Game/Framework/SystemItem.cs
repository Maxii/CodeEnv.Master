// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemItem.cs
// The data-holding class for all Systems in the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// The data-holding class for all Systems in the game.
/// </summary>
public class SystemItem : Item {
    // WARNING: Donot change this to "System", a protected word
    public new SystemData Data {
        get { return base.Data as SystemData; }
        set { base.Data = value; }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

