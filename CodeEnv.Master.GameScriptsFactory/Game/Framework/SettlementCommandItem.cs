// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCommandItem.cs
// Item class for Unit Settlement Commands.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
///  Item class for Unit Settlement Commands. Settlements currently don't move.
/// </summary>
public class SettlementCommandItem : AUnitBaseCommandItem /*, ICameraFollowable  [not currently in motion]*/ {

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

    #region Initialization

    protected override IGuiHudPublisher InitializeHudPublisher() {
        var publisher = new GuiHudPublisher<SettlementCmdData>(Data);
        publisher.SetOptionalUpdateKeys(GuiHudLineKeys.Health);
        return publisher;
    }

    #endregion

    #region Model Methods

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
        return SettlementIconFactory.Instance.MakeInstance(Data, PlayerIntel);
    }

    #endregion

    #region Mouse Events
    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDestinationTarget Members

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

