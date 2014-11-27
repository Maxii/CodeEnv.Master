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

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;

/// <summary>
///  Item class for Unit Starbase Commands. 
/// </summary>
public class StarbaseCommandItem : AUnitBaseCommandItem {

    public new StarbaseCmdData Data {
        get { return base.Data as StarbaseCmdData; }
        set { base.Data = value; }
    }

    public bool enableTrackingLabel = false;

    private ITrackingWidget _trackingLabel;

    #region Initialization

    protected override IGuiHudPublisher InitializeHudPublisher() {
        var publisher = new GuiHudPublisher<StarbaseCmdData>(Data);
        publisher.SetOptionalUpdateKeys(GuiHudLineKeys.Health);
        return publisher;
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

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        if (_trackingLabel != null) {
            _trackingLabel.Show(IsDiscernible);
        }
    }

    protected override IIcon MakeCmdIconInstance() {
        return StarbaseIconFactory.Instance.MakeInstance(Data, PlayerIntel);
    }

    #endregion

    #region Mouse Events
    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        UnityUtility.DestroyIfNotNullOrAlreadyDestroyed(_trackingLabel);
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region INavigableTarget Members

    public override bool IsMobile { get { return false; } }

    #endregion

}

