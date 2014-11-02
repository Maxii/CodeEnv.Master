// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipCtxControl_AI.cs
// Context Menu Control for <see cref="ShipItem"/>s operated by the AI.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;

/// <summary>
/// Context Menu Control for <see cref="ShipItem"/>s operated by the AI.
/// Currently, this class does nothing as there is no context menu for an AI ship.
/// </summary>
public class ShipCtxControl_AI : ACtxControl {

    protected override int UniqueSubmenuCountReqd { get { return Constants.Zero; } }

    private ShipItem _shipMenuOperator;

    public ShipCtxControl_AI(ShipItem ship)
        : base(ship.gameObject) {
        _shipMenuOperator = ship;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

