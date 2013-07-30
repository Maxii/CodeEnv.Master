// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Star.cs
// Manages a stationary Star.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Manages a stationary Star.
/// </summary>
public class Star : StationaryItem, ISelectable {

    /// <summary>
    /// The separation between the pivot point on the star and the system
    /// label as a Viewport vector. Viewport vector values vary from 0.0F to 1.0F.
    /// </summary>
    [SerializeField]
    private Vector3 _systemLabelOffsetFromPivot = new Vector3(Constants.ZeroF, 0.02F, Constants.ZeroF);

    /// <summary>
    /// The offset that determines the point on the star from which
    ///  the System label pivots, as a Worldspace vector.
    /// </summary>
    private Vector3 _systemLabelPivotOffset;
    private UILabel _systemLabel;

    private SystemGraphics _systemGraphics;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _systemGraphics = gameObject.GetSafeMonoBehaviourComponentInParents<SystemGraphics>();
    }

    protected override void InitializeOnStart() {
        base.InitializeOnStart();
        HumanPlayerIntelLevel = IntelLevel.Complete;
    }

    protected override void OnHover(bool isOver) {
        base.OnHover(isOver);
        _systemGraphics.HighlightTrackingLabel(isOver);
    }

    protected override void OnClick() {
        base.OnClick();
        if (NguiGameInput.IsLeftMouseButtonClick()) {
            OnLeftClick();
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ISelectable Members

    public void OnLeftClick() { // TODO
        //_systemMgr.HighlightSystem(true, SystemManager.SystemHighlights.Select);
    }

    private bool _isSelected;
    public bool IsSelected {
        get { return _isSelected; }
        set { SetProperty<bool>(ref _isSelected, value, "IsSelected"); }
    }

    #endregion

}

