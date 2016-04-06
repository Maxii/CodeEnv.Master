// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCommandItem.cs
// Class for AUnitBaseCmdItems that are Starbases.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Class for AUnitBaseCmdItems that are Starbases.
/// </summary>
public class StarbaseCmdItem : AUnitBaseCmdItem, IStarbaseCmdItem {

    public new StarbaseCmdData Data {
        get { return base.Data as StarbaseCmdData; }
        set { base.Data = value; }
    }

    private StarbasePublisher _publisher;
    public StarbasePublisher Publisher {
        get { return _publisher = _publisher ?? new StarbasePublisher(Data, this); }
    }

    private Rigidbody _highOrbitRigidbody;

    #region Initialization

    protected override AFormationManager InitializeFormationMgr() {
        return new StarbaseFormationManager(this);
    }

    protected override ItemHudManager InitializeHudManager() {
        return new ItemHudManager(Publisher);
    }

    #endregion

    public StarbaseReport GetUserReport() { return GetReport(_gameMgr.UserPlayer); }

    public StarbaseReport GetReport(Player player) { return Publisher.GetReport(player); }

    public FacilityReport[] GetElementReports(Player player) {
        return Elements.Cast<FacilityItem>().Select(e => e.GetReport(player)).ToArray();
    }

    protected override IconInfo MakeIconInfo() {
        return StarbaseIconInfoFactory.Instance.MakeInstance(GetUserReport());
    }

    protected override void ShowSelectedItemHud() {
        SelectedItemHudWindow.Instance.Show(FormID.SelectedStarbase, GetUserReport());
    }

    protected override void HandleDeath() {
        base.HandleDeath();
        // unlike SettlementCmdItem, no parent orbiter object to disable or destroy
    }

    protected override void ConnectHighOrbitRigidbodyToShipOrbitJoint(FixedJoint shipOrbitJoint) {
        if (_highOrbitRigidbody == null) {
            _highOrbitRigidbody = gameObject.AddMissingComponent<Rigidbody>();
            _highOrbitRigidbody.useGravity = false;
            _highOrbitRigidbody.isKinematic = true;
        }
        shipOrbitJoint.connectedBody = _highOrbitRigidbody;
    }

    #region Cleanup

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

