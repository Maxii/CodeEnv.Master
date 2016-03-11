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

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// AUnitBaseCmdItems that are Settlements.
/// </summary>
public class SettlementCmdItem : AUnitBaseCmdItem, ISettlementCmdItem /*, ICameraFollowable  [not currently in motion]*/ {

    public new SettlementCmdData Data {
        get { return base.Data as SettlementCmdData; }
        set { base.Data = value; }
    }

    private SystemItem _parentSystem;
    public SystemItem ParentSystem {
        get { return _parentSystem; }
        set {
            D.Assert(_parentSystem == null);  // only happens once
            _parentSystem = value;
            ParentSystemPropSetHandler();
        }
    }

    private SettlementPublisher _publisher;
    public SettlementPublisher Publisher {
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

    public SettlementReport GetUserReport() { return Publisher.GetUserReport(); }

    public SettlementReport GetReport(Player player) { return Publisher.GetReport(player); }

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
        return SettlementIconInfoFactory.Instance.MakeInstance(GetUserReport());
    }

    protected override void ShowSelectedItemHud() {
        SelectedItemHudWindow.Instance.Show(FormID.SelectedSettlement, GetUserReport());
    }

    protected override void HandleDeath() {
        base.HandleDeath();
        RemoveSettlementFromSystem();
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

    #region INavigableTarget Members

    public override bool IsMobile { get { return ParentSystem.SettlementOrbitSlot.ToOrbit; } }

    #endregion

}

