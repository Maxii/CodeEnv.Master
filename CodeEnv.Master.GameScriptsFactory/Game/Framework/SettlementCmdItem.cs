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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
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
                D.Error("Parent System {0} of {1} can only be set once.", _parentSystem.DebugName, DebugName);
            }
            SetProperty<SystemItem>(ref _parentSystem, value, "ParentSystem", ParentSystemPropSetHandler);
        }
    }

    public SettlementCmdReport UserReport { get { return Data.Publisher.GetUserReport(); } }

    private IOrbitSimulator _celestialOrbitSimulator;
    public IOrbitSimulator CelestialOrbitSimulator {
        get {
            if (_celestialOrbitSimulator == null) {
                _celestialOrbitSimulator = UnitContainer.parent.GetComponent<IOrbitSimulator>();
            }
            return _celestialOrbitSimulator;
        }
    }

    private IList<Player> _playersWithInfoAccessToOwner;

    #region Initialization

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _playersWithInfoAccessToOwner = new List<Player>(TempGameValues.MaxPlayers);
    }

    protected override AFormationManager InitializeFormationMgr() {
        return new SettlementFormationManager(this);
    }

    protected override ItemHoveredHudManager InitializeHoveredHudManager() {
        return new ItemHoveredHudManager(Data.Publisher);
    }

    public override void FinalInitialize() {
        base.FinalInitialize();
        IsOperational = true;
    }

    #endregion

    public SettlementCmdReport GetReport(Player player) { return Data.Publisher.GetReport(player); }

    public FacilityReport[] GetElementReports(Player player) {
        return Elements.Cast<FacilityItem>().Select(e => e.GetReport(player)).ToArray();
    }

    /// <summary>
    /// Assesses whether to fire its infoAccessChanged event.
    /// <remarks>Implemented by some undetectable Items - System, SettlementCmd
    /// and Sector. All three allow a change in access to Owner while in IntelCoverage.Basic
    /// without requiring an increase in IntelCoverage. FleetCmd and StarbaseCmd are the other
    /// two undetectable Items, but they only change access to Owner when IntelCoverage
    /// exceeds Basic.</remarks>
    /// <remarks>3.22.17 This is the fix to a gnarly BUG that allowed changes in access to
    /// Owner without an event alerting subscribers that it had occurred. The subscribers
    /// relied on the event to keep their state correct, then found later that they had 
    /// access to Owner when they expected they didn't. Access to owner determines the
    /// response in a number of Interfaces like IFleetExplorable.IsExplorationAllowedBy(player).</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    internal void AssessWhetherToFireInfoAccessChangedEventFor(Player player) {
        if (!_playersWithInfoAccessToOwner.Contains(player)) {
            // A Settlement provides access to its Owner under 2 circumstances. First, if IntelCoverage >= Essential,
            // and second and more commonly, if its System provides access. A System provides access to its Owner
            // when its Star or any of its Planetoids provides access. They in turn provide access if their IntelCoverage
            // >= Essential. As IntelCoverage of Planetoids, Stars and Systems can't regress, once access is provided
            // it can't be lost which means access to a Settlement's Owner can't be lost either.
            if (InfoAccessCntlr.HasIntelCoverageReqdToAccess(player, ItemInfoID.Owner)) {
                _playersWithInfoAccessToOwner.Add(player);
                OnInfoAccessChanged(player);
            }
        }
    }

    /// <summary>
    /// Removes the settlement and its orbiter from the system in preparation for a future settlement.
    /// </summary>
    private void RemoveSettlementFromSystem() {
        ParentSystem.Settlement = null;
    }

    protected override TrackingIconInfo MakeIconInfo() {
        return SettlementIconInfoFactory.Instance.MakeInstance(UserReport);
    }

    protected override void ShowSelectedItemHud() {
        // 9.10.17 UnitHudWindow's SettlementForm will auto show InteractibleHudWindow's SettlementForm
        if (Owner.IsUser) {
            UnitHudWindow.Instance.Show(FormID.UserSettlement, this);
        }
        else {
            UnitHudWindow.Instance.Show(FormID.AiSettlement, this);
        }
    }

    protected override void PrepareForDeathEffect() {
        base.PrepareForDeathEffect();
        RemoveSettlementFromSystem();
    }

    protected override void DestroyApplicableParents(float delayInHours = Constants.ZeroF) {
        GameUtility.DestroyIfNotNullOrAlreadyDestroyed(_celestialOrbitSimulator, delayInHours);
    }

    protected override void ConnectHighOrbitRigidbodyToShipOrbitJoint(FixedJoint shipOrbitJoint) {
        shipOrbitJoint.connectedBody = CelestialOrbitSimulator.OrbitRigidbody;
    }

    protected override void AttemptHighOrbitRigidbodyDeactivation() {
        // Do nothing as Settlement HighOrbitRigidbody is on CelestialOrbitSimulator which needs to stay activated
    }

    protected override void HandleNameChanged() {
        base.HandleNameChanged();
        if (CelestialOrbitSimulator != null) {
            CelestialOrbitSimulator.transform.name = Name + GameConstants.OrbitSimulatorNameExtension;
        }
    }

    #region Event and Property Change Handlers

    private void ParentSystemPropSetHandler() {
        Data.ParentSystemData = ParentSystem.Data;
    }

    #endregion

    #region StateMachine Support Members

    protected override bool TryPickFacilityToRefitCmdModule(IEnumerable<FacilityItem> candidates, out FacilityItem facility) {
        D.Assert(!candidates.IsNullOrEmpty());
        facility = null;
        bool toRefitCmdModule = OwnerAiMgr.Designs.IsUpgradeDesignPresent(Data.CmdModuleDesign);
        if (toRefitCmdModule) {
            facility = candidates.SingleOrDefault(f => f.IsHQ);
            if (facility == null) {
                facility = candidates.First();
            }
        }
        return toRefitCmdModule;
    }

    #endregion


    #region Cleanup

    #endregion

    #region INavigableDestination Members

    public override bool IsMobile { get { return ParentSystem.SettlementOrbitData.ToOrbit; } }

    #endregion

}

