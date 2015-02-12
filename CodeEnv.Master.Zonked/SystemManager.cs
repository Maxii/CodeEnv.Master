﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemManager.cs
// The manager of a System whos primary purpose is to take and execute orders.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// The manager of a System whos primary purpose is to take and execute orders. SystemGraphics handles
/// most graphics operations, and each individual system object handles camera interaction and showing
/// Hud info.
/// </summary>
[System.Obsolete]
public class SystemManager : AMonoBase, ISelectable, IHasData {

    /// <summary>
    /// Used for convenience only. Actual SystemData repository is held by OrbitalPlane.
    /// </summary>
    public SystemData Data {
        get { return _orbitalPlane.Data; }
        set { _orbitalPlane.Data = value; }
    }

    private IntelLevel _playerIntelLevel;
    public IntelLevel PlayerIntelLevel {
        get { return _playerIntelLevel; }
        set { SetProperty<IntelLevel>(ref _playerIntelLevel, value, "PlayerIntelLevel", OnIntelLevelChanged); }
    }

    private SystemGraphics _systemGraphics;
    private OrbitalPlaneInputEventRouter _orbitalPlane;
    private Star _star;
    private FollowableItem[] _planetsAndMoons;
    private GameEventManager _eventMgr;
    private SelectionManager _selectionMgr;

    protected override void Awake() {
        base.Awake();
        _orbitalPlane = gameObject.GetSafeMonoBehaviourComponentInChildren<OrbitalPlaneInputEventRouter>();
        _systemGraphics = gameObject.GetSafeMonoBehaviourComponent<SystemGraphics>();
        _star = gameObject.GetSafeMonoBehaviourComponentInChildren<Star>();
        _planetsAndMoons = gameObject.GetSafeMonoBehaviourComponentsInChildren<FollowableItem>();
        _eventMgr = GameEventManager.Instance;
        _selectionMgr = SelectionManager.Instance;
    }

    private void OnIntelLevelChanged() {
        _orbitalPlane.PlayerIntelLevel = PlayerIntelLevel;
        _star.PlayerIntelLevel = PlayerIntelLevel;
        _planetsAndMoons.ForAll<FollowableItem>(pm => pm.PlayerIntelLevel = PlayerIntelLevel);
    }

    private void OnIsSelectedChanged() {
        _systemGraphics.AssessHighlighting();
        if (IsSelected) {
            _selectionMgr.CurrentSelection = this;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ISelectable Members

    private bool _isSelected;
    public bool IsSelected {
        get { return _isSelected; }
        set { SetProperty<bool>(ref _isSelected, value, "IsSelected", OnIsSelectedChanged); }
    }

    public void OnLeftClick() {
        IsSelected = true;
    }

    #endregion

    #region IHasData Members

    public AMortalItemData GetData() {
        return Data;
    }

    #endregion
}

