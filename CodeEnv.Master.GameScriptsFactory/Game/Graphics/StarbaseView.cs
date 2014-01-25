// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseView.cs
// A class for managing the elements of a Starbase's UI, those that are not already handled by the UI classes for Facilities. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A class for managing the elements of a Starbase's UI, those that are not already handled by the UI classes for Facilities. 
/// </summary>
public class StarbaseView : ACommandView, IHighlightTrackingLabel {

    public new StarbasePresenter Presenter {
        get { return base.Presenter as StarbasePresenter; }
        protected set { base.Presenter = value; }
    }

    public bool enableTrackingLabel = false;
    private GuiTrackingLabel _trackingLabel;

    protected override void InitializePresenter() {
        Presenter = new StarbasePresenter(this);
    }

    protected override void InitializeTrackingTarget() {
        TrackingTarget = Presenter.GetHQElementTransform();
    }

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        if (_trackingLabel != null) {
            _trackingLabel.gameObject.SetActive(IsDiscernible);
        }
    }

    protected override void OnTrackingTargetChanged() {
        base.OnTrackingTargetChanged();
        InitializeTrackingLabel();
    }

    protected override void OnPlayerIntelContentChanged() {
        base.OnPlayerIntelContentChanged();
        Presenter.OnPlayerIntelChanged();
    }

    protected override void RequestContextMenu(bool isDown) {
        Presenter.RequestContextMenu(isDown);
    }

    protected override void OnAltLeftClick() {
        base.OnAltLeftClick();
        Presenter.__SimulateAllElementsAttacked();
    }

    protected override void OnIsSelectedChanged() {
        base.OnIsSelectedChanged();
        Presenter.OnIsSelectedChanged();
    }

    private void InitializeTrackingLabel() {
        if (enableTrackingLabel) {
            float minShowDistance = TempGameValues.MinTrackingLabelShowDistance;
            string fleetName = Presenter.Item.PieceName;
            _trackingLabel = GuiTrackingLabelFactory.Instance.CreateGuiTrackingLabel(TrackingTarget, GuiTrackingLabelFactory.LabelPlacement.AboveTarget, minShowDistance, Mathf.Infinity, fleetName);
        }
    }

    protected override void Cleanup() {
        base.Cleanup();
        if (_trackingLabel != null) {
            Destroy(_trackingLabel.gameObject);
            _trackingLabel = null;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IHighlightTrackingLabel Members

    public void HighlightTrackingLabel(bool toHighlight) {
        if (_trackingLabel != null) {   // can be gap between checking enableTrackingLabel and instantiating it
            _trackingLabel.IsHighlighted = toHighlight;
        }
    }

    #endregion
}

