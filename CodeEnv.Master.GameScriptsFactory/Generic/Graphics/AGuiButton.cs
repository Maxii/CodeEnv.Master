// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiButton.cs
// Abstract base class for button scripts.
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
/// Abstract base class for button scripts.
/// Note: There is no Button that inherits from AGuiMenuElement as buttons
/// aren't used to set state within a Menu.
/// </summary>
public abstract class AGuiButton : AGuiTooltip {

    protected PlayerPrefsManager _playerPrefsMgr;
    protected GameManager _gameMgr;
    protected UIButton _button;

    protected override void Awake() {
        base.Awake();
        _playerPrefsMgr = PlayerPrefsManager.Instance;
        _gameMgr = GameManager.Instance;
        _button = gameObject.GetSafeMonoBehaviourComponent<UIButton>();
    }

    void OnClick() {
        if (GameInputHelper.Instance.IsLeftMouseButton) {
            OnLeftClick();
        }
    }

    protected abstract void OnLeftClick();

    // IDisposable Note: No reason to remove Ngui event currentListeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to this same GameObject that is being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.
}

