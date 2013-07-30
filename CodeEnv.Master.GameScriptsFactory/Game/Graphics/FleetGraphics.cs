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

    public Vector3 trackingLabelOffsetFromPivot = new Vector3(Constants.ZeroF, 0.02F, Constants.ZeroF);

    public int minTrackingLabelShowDistance = TempGameValues.MinFleetTrackingLabelShowDistance;
    public int maxTrackingLabelShowDistance = TempGameValues.MaxFleetTrackingLabelShowDistance;

    private GuiTrackingLabel _trackingLabel;


    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        Target = gameObject.GetSafeMonoBehaviourComponentInChildren<FleetAdmiral>().transform;
    }

    protected override void InitializeOnStart() {
        base.InitializeOnStart();
    }

    protected override void RegisterComponentsToDisable() {
        disableComponentOnInvisible = new Component[1] { 
            Target.collider 
        };
        disableGameObjectOnInvisible = new GameObject[1] { 
            gameObject.GetSafeMonoBehaviourComponentInChildren<Billboard>().gameObject
        };
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
            Logger.Log("FleetTrackingLabel.IsShowing = {0}.", toShowTrackingLabel);
            _trackingLabel.IsShowing = toShowTrackingLabel;
        }
        return distanceToCamera;
    }

    private GuiTrackingLabel InitializeTrackingLabel() {
        Vector3 pivotOffset = new Vector3(Constants.ZeroF, Target.collider.bounds.extents.y, Constants.ZeroF);
        GuiTrackingLabel trackingLabel = GuiTrackingLabelFactory.CreateGuiTrackingLabel(Target, pivotOffset, trackingLabelOffsetFromPivot);
        trackingLabel.IsShowing = true;
        return trackingLabel;
    }

    public void HighlightTrackingLabel(bool toHighlight) {
        if (_trackingLabel != null) {   // can be gap between checking enableTrackingLabel and instantiating it
            _trackingLabel.IsHighlighted = toHighlight;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

