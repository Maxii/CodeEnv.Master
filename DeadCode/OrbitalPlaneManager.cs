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

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Manages a Systems Orbital plane.
/// </summary>
public class OrbitalPlaneManager : AMonoBase, ISelectable, ICameraTargetable, IZoomToFurthest {

    private GameEventManager _eventMgr;
    private Transform _transform;
    private SystemCreator _systemMgr;
    private MeshRenderer _renderer;

    void Awake() {
        UnityUtility.ValidateComponentPresence<BoxCollider>(gameObject);
        _transform = transform;
        _renderer = gameObject.GetComponentInChildren<MeshRenderer>();
        _eventMgr = GameEventManager.Instance;
        _systemMgr = gameObject.GetSafeFirstMonoBehaviourInParents<SystemCreator>();
    }

    void OnHover(bool isOver) {
        if (isOver) {
            _systemMgr.DisplayCursorHUD();
        }
        else {
            _systemMgr.ClearCursorHUD();
        }
        _systemMgr.TrackingLabel.IsHighlighted = isOver;
        Logger.Log("{0}.OnHover({1}) called.", GetType().Name, isOver);
    }

    public void OnClick() {
        Logger.Log("{0}.OnClick() called.", GetType().Name);
        if (GameInputHelper.IsLeftMouseButton()) {
            OnLeftClick();
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraTargetable Members

    public bool IsTargetable {
        get { return true; }
    }

    [SerializeField]
    private float minimumCameraViewingDistance = 10F;
    public float MinimumCameraViewingDistance {
        get {
            return minimumCameraViewingDistance;
        }
    }

    #endregion

    #region ISelectable Members

    public void OnLeftClick() { //TODO
        //_systemMgr.HighlightSystem(true, SystemManager.SystemHighlights.Select);
    }

    private bool _isSelected;
    public bool IsSelected {
        get { return _isSelected; }
        set { SetProperty<bool>(ref _isSelected, value, "IsSelected"); }
    }

    #endregion
}

