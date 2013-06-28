// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OrbitalPlaneManager.cs
// Manages a Systems Orbital plane.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Manages a Systems Orbital plane.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class OrbitalPlaneManager : AMonoBehaviourBase, ICameraFocusable, IZoomToFurthest {

    private GameEventManager _eventMgr;
    private Transform _transform;
    private SystemManager _systemMgr;

    void Awake() {
        _eventMgr = GameEventManager.Instance;
        _transform = transform;
        _systemMgr = _transform.parent.gameObject.GetSafeMonoBehaviourComponent<SystemManager>();
    }

    void OnHover(bool isOver) {
        if (isOver) {
            _systemMgr.DisplayCursorHUD();
        }
        else {
            _systemMgr.ClearCursorHUD();
        }
        _systemMgr.GuiTrackingLabel.IsHighlighted = isOver;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraTargetable Members

    [SerializeField]
    private float minimumCameraViewingDistance = 10F;
    public float MinimumCameraViewingDistance {
        get {
            return minimumCameraViewingDistance;
        }
    }

    #endregion

    #region ICameraFocusable Members

    [SerializeField]
    private float _optimalCameraViewingDistance = 500F;
    public float OptimalCameraViewingDistance {
        get { return _optimalCameraViewingDistance; }
    }

    public void OnClick() {
        //Debug.Log("{0}.OnClick() called.".Inject(GetType().Name));
        if (NguiGameInput.IsMiddleMouseButtonClick()) {
            _eventMgr.Raise<FocusSelectedEvent>(new FocusSelectedEvent(this, _transform));
        }
    }

    #endregion
}

