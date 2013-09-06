// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetGraphics.cs
// Handles graphics optimization for Fleets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Handles graphics optimization for Fleets. Assumes location is with Fleet
/// game object, not FleetAdmiral.
/// </summary>
public class FleetGraphics : AGraphics {

    public bool enableTrackingLabel = false;

    public Vector3 trackingLabelOffsetFromPivot = new Vector3(Constants.ZeroF, 0.05F, Constants.ZeroF);

    public int minTrackingLabelShowDistance = TempGameValues.MinFleetTrackingLabelShowDistance;
    public int maxTrackingLabelShowDistance = TempGameValues.MaxFleetTrackingLabelShowDistance;

    private Color __originalFleetIconColor;
    private UISprite _fleetIcon;

    private GuiTrackingLabel _trackingLabel;
    private FleetCommand _fleetCmd;
    private FleetManager _fleetMgr;

    protected override void Awake() {
        base.Awake();
        _fleetMgr = gameObject.GetSafeMonoBehaviourComponent<FleetManager>();
        _fleetCmd = gameObject.GetSafeMonoBehaviourComponentInChildren<FleetCommand>();
        Target = _fleetCmd.transform;
        InitializeHighlighting();
    }

    protected override void RegisterComponentsToDisable() {
        disableComponentOnInvisible = new Component[1] { 
            Target.collider 
        };
        disableGameObjectOnInvisible = new GameObject[1] { 
            gameObject.GetSafeMonoBehaviourComponentInChildren<Billboard>().gameObject
        };
    }

    private void InitializeHighlighting() {
        _fleetIcon = gameObject.GetSafeMonoBehaviourComponentInChildren<UISprite>();
        __originalFleetIconColor = _fleetIcon.color;
    }


    protected override int EnableBasedOnDistanceToCamera() {
        int distanceToCamera = Constants.Zero;
        if (enableTrackingLabel) {  // allows tester to enable while editor is playing
            if (_trackingLabel == null) {
                _trackingLabel = InitializeTrackingLabel();
            }
            distanceToCamera = base.EnableBasedOnDistanceToCamera();
            bool toShowTrackingLabel = false;
            if (IsVisible) {
                if (distanceToCamera == Constants.Zero) {
                    distanceToCamera = Target.DistanceToCameraInt();
                }
                if (Utility.IsInRange(distanceToCamera, minTrackingLabelShowDistance, maxTrackingLabelShowDistance)) {
                    toShowTrackingLabel = true;
                }
            }
            //Logger.Log("FleetTrackingLabel.IsShowing = {0}.", toShowTrackingLabel);
            _trackingLabel.IsShowing = toShowTrackingLabel;
        }
        return distanceToCamera;
    }

    private GuiTrackingLabel InitializeTrackingLabel() {
        // use LeadShip collider for the offset rather than the Admiral collider as the Admiral collider changes scale dynamically. FIXME LeadShips die!!!
        Vector3 pivotOffset = new Vector3(Constants.ZeroF, _fleetMgr.LeadShipCaptain.collider.bounds.extents.y, Constants.ZeroF);
        GuiTrackingLabel trackingLabel = GuiTrackingLabelFactory.CreateGuiTrackingLabel(Target, pivotOffset, trackingLabelOffsetFromPivot);
        trackingLabel.IsShowing = true;
        return trackingLabel;
    }

    public void HighlightTrackingLabel(bool toHighlight) {
        if (_trackingLabel != null) {   // can be gap between checking enableTrackingLabel and instantiating it
            _trackingLabel.IsHighlighted = toHighlight;
        }
    }

    public void ChangeHighlighting() {
        if (!IsVisible) {
            Highlight(false);
            return;
        }
        if (_fleetCmd.IsFocus) {
            if (_fleetMgr.IsSelected) {
                Highlight(true, Highlights.Both);
                return;
            }
            Highlight(true, Highlights.Focused);
            return;
        }
        if (_fleetMgr.IsSelected) {
            Highlight(true, Highlights.Selected);
            return;
        }
        Highlight(true, Highlights.None);
    }

    private void Highlight(bool toShow, Highlights highlight = Highlights.None) {
        _fleetIcon.gameObject.SetActive(toShow);
        if (!toShow) {
            return;
        }
        switch (highlight) {
            case Highlights.Focused:
                _fleetIcon.color = UnityDebugConstants.IsFocusedColor;
                break;
            case Highlights.Selected:
                _fleetIcon.color = UnityDebugConstants.IsSelectedColor;
                break;
            case Highlights.Both:
                _fleetIcon.color = UnityDebugConstants.IsFocusAndSelectedColor;
                break;
            case Highlights.None:
                _fleetIcon.color = __originalFleetIconColor;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
    }

    protected override void OnIsVisibleChanged() {
        base.OnIsVisibleChanged();
        ChangeHighlighting();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

