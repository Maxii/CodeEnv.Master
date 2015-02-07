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

using System.Linq;
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

    private SettlementPublisher _publisher;
    public SettlementPublisher Publisher {
        get { return _publisher = _publisher ?? new SettlementPublisher(Data); }
    }

    public override bool IsHudShowing {
        get { return _hudManager != null && _hudManager.IsHudShowing; }
    }

    private CmdHudManager<SettlementPublisher> _hudManager;

    #region Initialization

    //protected override IGuiHudPublisher InitializeHudPublisher() {
    //    var publisher = new GuiHudPublisher<SettlementCmdData>(Data);
    //    publisher.SetOptionalUpdateKeys(GuiHudLineKeys.Health);
    //    return publisher;
    //}

    protected override void InitializeHudManager() {
        _hudManager = new CmdHudManager<SettlementPublisher>(Publisher);
    }

    #endregion

    #region Model Methods

    public SettlementReport GetReport(Player player) { return Publisher.GetReport(player, GetElementReports(player)); }

    private FacilityReport[] GetElementReports(Player player) {
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

    public override void ShowHud(bool toShow) {
        if (_hudManager != null) {
            if (toShow) {
                _hudManager.Show(Position, GetElementReports(_gameMgr.HumanPlayer));
            }
            else {
                _hudManager.Hide();
            }
        }
    }

    protected override IIcon MakeCmdIconInstance() {
        return SettlementIconFactory.Instance.MakeInstance(Data);
    }

    #endregion

    #region Mouse Events

    //private SettlementReportGenerator _reportGenerator;
    //public SettlementReportGenerator ReportGenerator {
    //    get {
    //        return _reportGenerator = _reportGenerator ?? new SettlementReportGenerator(Data);
    //    }
    //}

    //protected override void OnHover(bool isOver) {
    //    if (isOver) {
    //        FacilityReport[] elementReports = Elements.Cast<FacilityItem>().Select(f => f.GetReport(_gameMgr.HumanPlayer)).ToArray();
    //        string hudText = ReportGenerator.GetCursorHudText(Data.GetHumanPlayerIntel(), elementReports);
    //        GuiCursorHud.Instance.Set(hudText, Position);
    //    }
    //    else {
    //        GuiCursorHud.Instance.Clear();
    //    }
    //}

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        if (_hudManager != null) {
            _hudManager.Dispose();
        }
    }

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

