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

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for button scripts.
/// Note: There is no Button that inherits from AGuiMenuElement as buttons
/// aren't used to set state within a Menu.
/// </summary>
public abstract class AGuiButton : ATextTooltip {

    /// <summary>
    /// Keys that are valid to actuate a click event. Default is empty.
    /// For use with Ngui's UIKeyBinding script. The key(s) that UIKeyBinding binds
    /// to this button must be listed as ValidKeys for this button to generate a valid click event.
    /// </summary>
    protected virtual IList<KeyCode> ValidKeys { get { return new List<KeyCode>(Constants.Zero); } }

    /// <summary>
    /// Ngui Mouse Buttons that are valid to actuate a click event. Default is Left.
    /// </summary>
    protected virtual IList<NguiMouseButton> ValidMouseButtons { get { return new List<NguiMouseButton>() { NguiMouseButton.Left }; } }

    protected PlayerPrefsManager _playerPrefsMgr;
    protected GameManager _gameMgr;
    protected UIButton _button;
    private GameInputHelper _helper;

    protected override void Awake() {
        base.Awake();
        _playerPrefsMgr = PlayerPrefsManager.Instance;
        _gameMgr = GameManager.Instance;
        _helper = GameInputHelper.Instance;
        _button = gameObject.GetSafeComponent<UIButton>();
        ValidateKeys();
    }

    #region Event and Property Change Handlers

    protected abstract void HandleValidClick();

    private void ClickEventHandler() {
        if (_helper.IsCurrentMouseButtonAnyOf(ValidMouseButtons) || _helper.IsCurrentKeyAnyOf(ValidKeys)) {
            HandleValidClick();
        }
    }

    void OnClick() {
        ClickEventHandler();
    }

    #endregion

    private void ValidateKeys() {
        UIKeyBinding[] keyBinders = gameObject.GetComponents<UIKeyBinding>();
        if (!keyBinders.IsNullOrEmpty()) {
            var keyCodes = keyBinders.Select(kb => (int)kb.keyCode);
            var validKeys = ValidKeys.Cast<int>();
            if (!keyCodes.SequenceEquals(validKeys, ignoreOrder: true)) {
                D.WarnContext(gameObject, "{0} has one or more {1}s without corresponding list of ValidKeys.", GetType().Name, typeof(UIKeyBinding).Name);
            }
        }
    }

    // IDisposable Note: No reason to remove Ngui event currentListeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to this same GameObject that is being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.
}

