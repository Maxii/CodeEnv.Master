// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OrbitalPlane.cs
// Manages the interaction of the Orbital plane, aka the 'system', with the Player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// Manages the interaction of the Orbital plane, aka the 'system', with the Player.
/// </summary>
public class OrbitalPlane : StationaryItem, ISelectable, IZoomToFurthest {

    public new SystemData Data {
        get { return base.Data as SystemData; }
        set { base.Data = value; }
    }

    public float minPlaneZoomDistance = 1.0F;
    public float optimalPlaneFocusDistance = 400F;

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

    protected override void OnIsFocusChanged() {
        _systemGraphics.HighlightSystem(IsFocus, SystemGraphics.SystemHighlights.Focus);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraTargetable Members

    /// <summary>
    /// Overridden because the default implementation returns a value that
    /// is a factor of the collider bounds which doesn't work for the orbital
    /// plane collider.
    /// </summary>
    public override float MinimumCameraViewingDistance { get { return minPlaneZoomDistance; } }

    #endregion

    #region ICameraFocusable Members

    /// <summary>
    /// Overridden because the default implementation returns a value that
    /// is a factor of the collider bounds which doesn't work for the orbital
    /// plane collider.
    /// </summary>
    public override float OptimalCameraViewingDistance { get { return optimalPlaneFocusDistance; } }

    #endregion

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

