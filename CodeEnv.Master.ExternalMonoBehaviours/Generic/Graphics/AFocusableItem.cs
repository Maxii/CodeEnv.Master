// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AFocusableItem.cs
// Abstract base class for any item in the universe that is focusable. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Abstract base class for any item in the universe that is focusable. Also provides
/// CursorHud support if there is Data for the item.
/// </summary>
public abstract class AFocusableItem : AItem, ICameraFocusable {

    protected GameEventManager _eventMgr;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _eventMgr = GameEventManager.Instance;
    }

    protected virtual void OnClick() {
        D.Log("{0}.OnClick() called.", gameObject.name);
        if (NguiGameInput.IsMiddleMouseButtonClick()) {
            OnMiddleClick();
        }
    }

    protected virtual void OnMiddleClick() {
        _eventMgr.Raise<FocusSelectedEvent>(new FocusSelectedEvent(this, _transform));
    }

    protected virtual void OnIsFocusChanged() { }

    #region ICameraFocusable Members

    [SerializeField]
    private float optimalCameraViewingDistanceMultiplier = 10.0F;

    private float _optimalCameraViewingDistance;
    public virtual float OptimalCameraViewingDistance {
        get {
            if (_optimalCameraViewingDistance == Constants.ZeroF) {
                _optimalCameraViewingDistance = _collider.bounds.extents.magnitude * optimalCameraViewingDistanceMultiplier;
            }
            return _optimalCameraViewingDistance;
        }
    }

    private bool _isFocus;
    public virtual bool IsFocus {
        get { return _isFocus; }
        set { SetProperty<bool>(ref _isFocus, value, "IsFocus", OnIsFocusChanged); }
    }

    #endregion

}

