// --------------------------------------------------------------------------------------------------------------------
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

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// The manager of a System whos primary purpose is to take and execute orders. SystemGraphics handles
/// most graphics operations, and each individual system object handles camera interaction and showing
/// Hud info.
/// </summary>
public class SystemManager : AMonoBehaviourBase, ISelectable {

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
    private OrbitalPlane _orbitalPlane;
    private Star _star;
    private FollowableItem[] _planetsAndMoons;
    private GameEventManager _eventMgr;

    protected override void Awake() {
        base.Awake();
        _orbitalPlane = gameObject.GetSafeMonoBehaviourComponentInChildren<OrbitalPlane>();
        _systemGraphics = gameObject.GetSafeMonoBehaviourComponent<SystemGraphics>();
        _star = gameObject.GetSafeMonoBehaviourComponentInChildren<Star>();
        _planetsAndMoons = gameObject.GetSafeMonoBehaviourComponentsInChildren<FollowableItem>();
        _eventMgr = GameEventManager.Instance;
    }

    private void OnIntelLevelChanged() {
        _orbitalPlane.PlayerIntelLevel = PlayerIntelLevel;
        _star.PlayerIntelLevel = PlayerIntelLevel;
        _planetsAndMoons.ForAll<FollowableItem>(pm => pm.PlayerIntelLevel = PlayerIntelLevel);
    }

    private void OnIsSelectedChanged() {
        _systemGraphics.ChangeHighlighting();
        if (IsSelected) {
            _eventMgr.Raise<SelectionEvent>(new SelectionEvent(this));
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

    public Data GetData() {
        return _orbitalPlane.Data;
    }

    public void OnLeftClick() {
        IsSelected = true;
    }

    #endregion
}

