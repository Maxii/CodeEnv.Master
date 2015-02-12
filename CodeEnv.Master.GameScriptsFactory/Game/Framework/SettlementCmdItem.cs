﻿// --------------------------------------------------------------------------------------------------------------------
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

/// <summary>
/// Class for AUnitBaseCmdItems that are Settlements.
/// </summary>
public class SettlementCmdItem : AUnitBaseCmdItem, ICmdPublisherClient<FacilityReport> /*, ICameraFollowable  [not currently in motion]*/ {

    public new SettlementCmdData Data {
        get { return base.Data as SettlementCmdData; }
        set { base.Data = value; }
    }

    /// <summary>
    /// Temporary flag set from SettlementCreator indicating whether
    /// this Settlement should move around it's star or stay in one location.
    /// IMPROVE no known way to switch the ICameraFollowable interface 
    /// on or off.
    /// </summary>
    public bool __OrbiterMoves { get; set; }

    private SettlementPublisher _publisher;
    public SettlementPublisher Publisher {
        get { return _publisher = _publisher ?? new SettlementPublisher(Data, this); }
    }

    #region Initialization

    protected override HudManager InitializeHudManager() {
        var hudManager = new HudManager(Publisher);
        hudManager.AddContentToUpdate(AHudManager.UpdatableLabelContentID.IntelState);
        return hudManager;
    }

    #endregion

    #region Model Methods

    public SettlementReport GetReport(Player player) { return Publisher.GetReport(player); }

    public FacilityReport[] GetElementReports(Player player) {
        return Elements.Cast<FacilityItem>().Select(e => e.GetReport(player)).ToArray();
    }

    protected override void OnDeath() {
        base.OnDeath();
        RemoveSettlementFromSystem();
    }

    /// <summary>
    /// Removes the settlement and its orbiter from the system in preparation for a future settlement.
    /// </summary>
    private void RemoveSettlementFromSystem() {
        var system = gameObject.GetSafeMonoBehaviourComponentInParents<SystemItem>();
        system.Settlement = null;
    }

    #endregion

    #region View Methods

    protected override IIcon MakeCmdIconInstance() {
        return SettlementIconFactory.Instance.MakeInstance(Data);
    }

    #endregion

    #region Mouse Events

    #endregion

    #region Cleanup

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region INavigableTarget Members

    public override bool IsMobile { get { return __OrbiterMoves; } }

    #endregion

    //#region ICameraFollowable Members

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

    //#endregion

}

