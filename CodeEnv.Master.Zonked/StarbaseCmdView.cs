// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCmdView.cs
// A class for managing the elements of a Starbase's UI, those that are not already handled by the UI classes for Facilities. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A class for managing the elements of a Starbase's UI, those that are not already handled by the UI classes for Facilities. 
/// </summary>
public class StarbaseCmdView : AUnitCommandView {

    public bool enableTrackingLabel = false;

    public new StarbaseCmdPresenter Presenter {
        get { return base.Presenter as StarbaseCmdPresenter; }
        protected set { base.Presenter = value; }
    }

    private ITrackingWidget _trackingLabel;

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void InitializePresenter() {
        Presenter = new StarbaseCmdPresenter(this);
    }

    protected override void InitializeVisualMembers() {
        base.InitializeVisualMembers();
        // Revolvers control their own enabled state
    }

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        if (_trackingLabel != null) {
            _trackingLabel.Show(IsDiscernible);
        }
    }

    protected override void OnTrackingTargetChanged() {
        base.OnTrackingTargetChanged();
        if (enableTrackingLabel && _trackingLabel == null) {
            _trackingLabel = InitializeTrackingLabel();
        }
    }

    protected override void OnPlayerIntelCoverageChanged() {
        base.OnPlayerIntelCoverageChanged();
        Presenter.OnPlayerIntelCoverageChanged();
    }

    protected override void OnIsSelectedChanged() {
        base.OnIsSelectedChanged();
        Presenter.OnIsSelectedChanged();
    }

    private ITrackingWidget InitializeTrackingLabel() {
        float minShowDistance = TempGameValues.MinTrackingLabelShowDistance;
        string starbaseName = Presenter.Model.UnitName;
        var trackingLabel = TrackingWidgetFactory.Instance.CreateUITrackingLabel(TrackingTarget, WidgetPlacement.AboveRight, minShowDistance);
        trackingLabel.OptionalRootName = starbaseName + CommonTerms.Label;
        trackingLabel.Set(starbaseName);
        return trackingLabel;
    }

    protected override void Cleanup() {
        base.Cleanup();
        UnityUtility.DestroyIfNotNullOrAlreadyDestroyed(_trackingLabel, Destroy);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

