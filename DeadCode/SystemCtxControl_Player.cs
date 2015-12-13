// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemCtxControl_Player.cs
// Context Menu Control for <see cref="SystemItem"/>s operated by the Human Player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Context Menu Control for <see cref="SystemItem"/>s operated by the Human Player.
/// Simply a shortcut to the context menu of the System's player-owned Settlement Base.
/// </summary>
public class SystemCtxControl_Player : BaseCtxControl_User {

    private SystemItem _system;

    public SystemCtxControl_User(SystemItem system)
        : base(system.Settlement, system.gameObject) {
        D.Assert(system.Settlement != null);
        _system = system;
    }

    //protected override CtxObject ValidateAndAcquireCtxObject() {
    //    // the system's CtxObject, not the system's base's CtxObject    - provides proper menu positioning
    //    return UnityUtility.ValidateMonoBehaviourPresence<CtxObject>(_system.gameObject);
    //}

    //public override void OnRightPressRelease() {    // must override to test for the system being selected
    //    if (_system.IsSelected) {
    //        D.Assert(_system.Owner.IsPlayer);
    //        _accessSource = AccessSource.Local;
    //        _playerRemoteAccessItem = null;
    //        Show(true);
    //        return;
    //    }

    //    var selected = SelectionManager.Instance.CurrentSelection;
    //    var selectedFleet = selected as FleetCommandItem;
    //    if (selectedFleet != null && selectedFleet.Owner.IsPlayer) {
    //        // a remote player owned fleet is selected
    //        _accessSource = AccessSource.RemoteFleet;
    //        _playerRemoteAccessItem = selectedFleet;
    //        Show(true);
    //        return;
    //    }

    //    var selectedShip = selected as ShipItem;
    //    if (selectedShip != null && selectedShip.Owner.IsPlayer) {
    //        // a remote player owned ship is selected
    //        _accessSource = AccessSource.RemoteShip;
    //        _playerRemoteAccessItem = selectedShip;
    //        Show(true);
    //        return;
    //    }

    //    _accessSource = AccessSource.None;
    //    _playerRemoteAccessItem = null;
    //}

    protected override bool TryIsSelectedItemAccessAttempted(ISelectable selected) {
        if (_system.IsSelected) {
            D.Assert(_system == selected as SystemItem);
            D.Assert(_system.Owner.IsPlayer);
            return true;
        }
        return false;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

