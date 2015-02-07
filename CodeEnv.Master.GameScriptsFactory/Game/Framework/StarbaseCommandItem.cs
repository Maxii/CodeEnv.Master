// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCommandItem.cs
// Item class for Unit Starbase Commands.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;

/// <summary>
///  Item class for Unit Starbase Commands. 
/// </summary>
public class StarbaseCommandItem : AUnitBaseCommandItem {

    public bool enableTrackingLabel = false;

    public new StarbaseCmdData2 Data {
        get { return base.Data as StarbaseCmdData2; }
        set { base.Data = value; }
    }
    //public new StarbaseCmdData Data {
    //    get { return base.Data as StarbaseCmdData; }
    //    set { base.Data = value; }
    //}

    private StarbasePublisher _publisher;
    public StarbasePublisher Publisher {
        get { return _publisher = _publisher ?? new StarbasePublisher(Data); }
    }

    public override bool IsHudShowing {
        get { return _hudManager != null && _hudManager.IsHudShowing; }
    }

    private CmdHudManager<StarbasePublisher> _hudManager;
    private ITrackingWidget _trackingLabel;

    #region Initialization

    //protected override IGuiHudPublisher InitializeHudPublisher() {
    //    var publisher = new GuiHudPublisher<StarbaseCmdData>(Data);
    //    publisher.SetOptionalUpdateKeys(GuiHudLineKeys.Health);
    //    return publisher;
    //}

    protected override void InitializeHudManager() {
        _hudManager = new CmdHudManager<StarbasePublisher>(Publisher);
    }

    private ITrackingWidget InitializeTrackingLabel() {
        float minShowDistance = TempGameValues.MinTrackingLabelShowDistance;
        var trackingLabel = TrackingWidgetFactory.Instance.CreateUITrackingLabel(HQElement, WidgetPlacement.AboveRight, minShowDistance);
        trackingLabel.Name = DisplayName + CommonTerms.Label;
        trackingLabel.Set(DisplayName);
        return trackingLabel;
    }

    #endregion

    #region Model Methods

    public StarbaseReport GetReport(Player player) { return Publisher.GetReport(player, GetElementReports(player)); }

    private FacilityReport[] GetElementReports(Player player) {
        return Elements.Cast<FacilityItem>().Select(e => e.GetReport(player)).ToArray();
    }

    protected override void OnHQElementChanged() {
        base.OnHQElementChanged();
        if (enableTrackingLabel) {
            _trackingLabel = _trackingLabel ?? InitializeTrackingLabel();
            _trackingLabel.Target = HQElement;
        }
    }

    protected override void OnDeath() {
        base.OnDeath();
        // unlike SettlementCmdItem, no parent orbiter object to disable or destroy
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

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        if (_trackingLabel != null) {
            _trackingLabel.Show(IsDiscernible);
        }
    }

    protected override IIcon MakeCmdIconInstance() {
        return StarbaseIconFactory.Instance.MakeInstance(Data);
    }

    #endregion

    #region Mouse Events

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        UnityUtility.DestroyIfNotNullOrAlreadyDestroyed(_trackingLabel);
        if (_hudManager != null) {
            _hudManager.Dispose();
        }
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region INavigableTarget Members

    public override bool IsMobile { get { return false; } }

    #endregion

}

