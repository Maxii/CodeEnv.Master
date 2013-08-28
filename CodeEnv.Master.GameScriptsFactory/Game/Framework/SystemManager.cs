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

    private SystemData _data;
    public SystemData Data {
        get { return _data; }
        set { SetProperty<SystemData>(ref _data, value, "Data", OnSystemDataChanged); }
    }

    private IntelLevel _playerIntelLevel;
    public IntelLevel PlayerIntelLevel {
        get { return _playerIntelLevel; }
        set { SetProperty<IntelLevel>(ref _playerIntelLevel, value, "PlayerIntelLevel", OnIntelLevelChanged); }
    }

    private SystemGraphics _systemGraphics;
    private OrbitalPlane _orbitalPlane;
    private GameEventManager _eventMgr;

    protected override void Awake() {
        base.Awake();
        _orbitalPlane = gameObject.GetSafeMonoBehaviourComponentInChildren<OrbitalPlane>();
        _systemGraphics = gameObject.GetSafeMonoBehaviourComponent<SystemGraphics>();
        _eventMgr = GameEventManager.Instance;
    }

    protected override void Start() {
        base.Start();
        PlayerIntelLevel = IntelLevel.Complete;
    }


    private void OnSystemDataChanged() {
        _orbitalPlane.Data = Data;
    }

    private void OnIntelLevelChanged() {
        _orbitalPlane.PlayerIntelLevel = PlayerIntelLevel;
    }

    private void OnIsSelectedChanged() {
        _systemGraphics.ChangeHighlighting();
        if (IsSelected) {
            _eventMgr.Raise<SelectionEvent>(new SelectionEvent(this, gameObject));
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


}

