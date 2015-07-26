// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCommandItem.cs
// Class for AUnitBaseCmdItems that are Settlements.
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
/// Class for AUnitBaseCmdItems that are Settlements.
/// </summary>
public class SettlementCmdItem : AUnitBaseCmdItem, ISettlementCmdItem /*, ICameraFollowable  [not currently in motion]*/ {

    public new SettlementCmdData Data {
        get { return base.Data as SettlementCmdData; }
        set { base.Data = value; }
    }

    private SystemItem _system;
    public SystemItem System {
        get { return _system; }
        set { SetProperty<SystemItem>(ref _system, value, "System", OnSystemChanged, OnSystemChanging); }
    }

    /// <summary>
    /// Temporary flag set from SettlementCreator indicating whether
    /// this Settlement should move around it's star or stay in one location.
    /// IMPROVE no known way to switch the ICameraFollowable interface 
    /// on or off.
    /// </summary>
    public bool __OrbitSimulatorMoves { get; set; }

    private SettlementPublisher _publisher;
    public SettlementPublisher Publisher {
        get { return _publisher = _publisher ?? new SettlementPublisher(Data, this); }
    }

    #region Initialization

    protected override ItemHudManager InitializeHudManager() {
        return new ItemHudManager(Publisher);
    }

    #endregion

    #region Model Methods

    public SettlementReport GetUserReport() { return Publisher.GetUserReport(); }

    public SettlementReport GetReport(Player player) { return Publisher.GetReport(player); }

    public FacilityReport[] GetElementReports(Player player) {
        return Elements.Cast<FacilityItem>().Select(e => e.GetReport(player)).ToArray();
    }

    private void OnSystemChanging(SystemItem newSystem) {
        D.Assert(System == null); // should only happen once. No reason to remove on death
    }

    private void OnSystemChanged() {
        Data.SystemData = System.Data;
    }

    protected override void PrepareForOnDeathNotification() {
        base.PrepareForOnDeathNotification();
        RemoveSettlementFromSystem();
    }

    /// <summary>
    /// Removes the settlement and its orbiter from the system in preparation for a future settlement.
    /// </summary>
    private void RemoveSettlementFromSystem() {
        System.Settlement = null;
    }

    #endregion

    #region View Methods

    protected override IconInfo MakeIconInfo() {
        return SettlementIconInfoFactory.Instance.MakeInstance(GetUserReport());
    }

    protected override void ShowSelectedItemHud() {
        SelectedItemHudWindow.Instance.Show(FormID.SelectedSettlement, GetUserReport());
    }

    #endregion

    #region Events

    #endregion

    #region Cleanup

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region INavigableTarget Members

    public override bool IsMobile { get { return __OrbitSimulatorMoves; } }

    #endregion

    #region ICameraFollowable Members

    //[SerializeField]
    //private float cameraFollowDistanceDampener = 3.0F;
    //public virtual float CameraFollowDistanceDampener {
    //    get { return cameraFollowDistanceDampener; }
    //}

    //[SerializeField]
    //private float cameraFollowRotationDampener = 1.0F;
    //public virtual float CameraFollowRotationDampener {
    //    get { return cameraFollowRotationDampener; }
    //}

    #endregion

}

