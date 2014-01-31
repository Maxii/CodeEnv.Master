﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementView.cs
// A class for managing the UI of a Settlement.
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
/// A class for managing the UI of a Settlement.
/// </summary>
public class SettlementView : ACommandView, ICameraFollowable {

    public new SettlementPresenter Presenter {
        get { return base.Presenter as SettlementPresenter; }
        protected set { base.Presenter = value; }
    }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void InitializePresenter() {
        Presenter = new SettlementPresenter(this);
    }

    protected override void InitializeTrackingTarget() {
        TrackingTarget = Presenter.GetTrackingTarget();
    }

    protected override void OnPlayerIntelCoverageChanged() {
        base.OnPlayerIntelCoverageChanged();
        Presenter.OnPlayerIntelCoverageChanged();
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

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraFollowable Members

    [SerializeField]
    private float cameraFollowDistanceDampener = 3.0F;
    public virtual float CameraFollowDistanceDampener {
        get { return cameraFollowDistanceDampener; }
    }

    [SerializeField]
    private float cameraFollowRotationDampener = 1.0F;
    public virtual float CameraFollowRotationDampener {
        get { return cameraFollowRotationDampener; }
    }

    #endregion

}

