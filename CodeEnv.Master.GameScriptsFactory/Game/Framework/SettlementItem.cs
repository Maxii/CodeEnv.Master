// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementItem.cs
//  The data-holding class for all Settlements in the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// The data-holding class for all Settlements in the game.
/// </summary>
public class SettlementItem : Item {

    public new SettlementData Data {
        get { return base.Data as SettlementData; }
        set { base.Data = value; }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

