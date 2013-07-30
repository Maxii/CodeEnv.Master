// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetAdmiralIcon.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// The followable item of a fleet, this class handles fleet collider events by
/// relaying the calls to the fleet.
/// </summary>
public class FleetAdmiralIcon : FollowableItem, ISelectable {

    private FleetManager _fleetMgr;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _fleetMgr = _transform.parent.parent.gameObject.GetSafeMonoBehaviourComponent<FleetManager>();
    }

    //protected override void __InitializeData() {
    // do nothing, fleetManager will display Hud data
    //}

    protected override void OnHover(bool isOver) {
        if (isOver) {
            // highlight guiTrackingLabel
            _fleetMgr.DisplayCursorHUD();
        }
        else {
            _fleetMgr.ClearCursorHUD();
        }
        Logger.Log("{0}.OnHover({1}) called.", GetType().Name, isOver);
    }

    void OnDoubleClick() {
        _fleetMgr.ChangeFleetHeading(_transform.right);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}

