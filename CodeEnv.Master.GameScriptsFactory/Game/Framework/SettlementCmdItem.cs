// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCommandItem.cs
// AUnitBaseCmdItems that are Settlements.
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
/// AUnitBaseCmdItems that are Settlements.
/// </summary>
public class SettlementCmdItem : AUnitBaseCmdItem, ISettlementCmd, ISettlementCmd_Ltd /*, ICameraFollowable  [not currently in motion]*/ {

    public new SettlementCmdData Data {
        get { return base.Data as SettlementCmdData; }
        set { base.Data = value; }
    }

    private SystemItem _parentSystem;
    public SystemItem ParentSystem {
        private get { return _parentSystem; }
        set {
            if (_parentSystem != null) {
                D.Error("Parent System {0} of {1} can only be set once.", _parentSystem.FullName, FullName);
            }
            SetProperty<SystemItem>(ref _parentSystem, value, "ParentSystem", ParentSystemPropSetHandler);
        }
    }

    public SettlementCmdReport UserReport { get { return Publisher.GetUserReport(); } }

    private IOrbitSimulator _celestialOrbitSimulator;
    public IOrbitSimulator CelestialOrbitSimulator {
        get {
            if (_celestialOrbitSimulator == null) {
                _celestialOrbitSimulator = UnitContainer.parent.GetComponent<IOrbitSimulator>();
            }
            return _celestialOrbitSimulator;
        }
    }

    private SettlementPublisher _publisher;
    private SettlementPublisher Publisher {
        get { return _publisher = _publisher ?? new SettlementPublisher(Data, this); }
    }

    #region Initialization

    protected override AFormationManager InitializeFormationMgr() {
        return new SettlementFormationManager(this);
    }

    protected override ItemHudManager InitializeHudManager() {
        return new ItemHudManager(Publisher);
    }

    #endregion

    public SettlementCmdReport GetReport(Player player) { return Publisher.GetReport(player); }

    public FacilityReport[] GetElementReports(Player player) {
        return Elements.Cast<FacilityItem>().Select(e => e.GetReport(player)).ToArray();
    }

    /// <summary>
    /// Removes the settlement and its orbiter from the system in preparation for a future settlement.
    /// </summary>
    private void RemoveSettlementFromSystem() {
        ParentSystem.Settlement = null;
    }

    protected override IconInfo MakeIconInfo() {
        return SettlementIconInfoFactory.Instance.MakeInstance(UserReport);
    }

    protected override void ShowSelectedItemHud() {
        SelectedItemHudWindow.Instance.Show(FormID.SelectedSettlement, UserReport);
    }

    protected override void HandleDeathBeforeBeginningDeathEffect() {
        base.HandleDeathBeforeBeginningDeathEffect();
        RemoveSettlementFromSystem();
    }

    protected override void DestroyApplicableParents(float delayInHours = Constants.ZeroF) {
        GameUtility.DestroyIfNotNullOrAlreadyDestroyed(_celestialOrbitSimulator, delayInHours);
    }

    protected override void ConnectHighOrbitRigidbodyToShipOrbitJoint(FixedJoint shipOrbitJoint) {
        shipOrbitJoint.connectedBody = CelestialOrbitSimulator.OrbitRigidbody;
    }

    #region Event and Property Change Handlers

    private void ParentSystemPropSetHandler() {
        Data.ParentSystemData = ParentSystem.Data;
    }

    #endregion

    #region Cleanup

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region INavigable Members

    public override bool IsMobile { get { return ParentSystem.SettlementOrbitData.ToOrbit; } }

    #endregion

}

