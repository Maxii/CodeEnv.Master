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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for button scripts.
/// Note: There is no Button that inherits from AGuiMenuElement as buttons generate events rather than store a state change.
/// </summary>
public abstract class AGuiButton : ATextTooltip {

    private static IList<NguiMouseButton> _validMouseButtons = new List<NguiMouseButton>() { NguiMouseButton.Left };

    /// <summary>
    /// Keys that are valid to actuate a click event. Default is none.
    /// For use with Ngui's UIKeyBinding script. The key(s) that UIKeyBinding binds
    /// to this button must be listed as ValidKeys for this button to generate a valid click event.
    /// </summary>
    protected virtual IEnumerable<KeyCode> ValidKeys { get { return Enumerable.Empty<KeyCode>(); } }

    /// <summary>
    /// Ngui Mouse Buttons that are valid to actuate a click event. Default is Left.
    /// </summary>
    protected virtual IEnumerable<NguiMouseButton> ValidMouseButtons { get { return _validMouseButtons; } }

    protected GameManager _gameMgr;
    protected UIButton _button;
    private GameInputHelper _inputHelper;

    protected override void Awake() {   // 6.5.18 Not sealed to allow UserActionButton to override as Singleton
        base.Awake();
        InitializeValuesAndReferences();
        __ValidateOnAwake();
    }

    protected virtual void InitializeValuesAndReferences() {
        _gameMgr = GameManager.Instance;
        _inputHelper = GameInputHelper.Instance;
        _button = gameObject.GetSafeComponent<UIButton>();
    }

    #region Event and Property Change Handlers

    private void ClickEventHandler() {
        if (_inputHelper.IsCurrentMouseButtonAnyOf(ValidMouseButtons) || _inputHelper.IsCurrentKeyAnyOf(ValidKeys)) {
            HandleValidClick();
        }
    }

    void OnClick() {
        ClickEventHandler();
    }

    #endregion

    protected abstract void HandleValidClick();

    #region Debug

    protected virtual void __ValidateOnAwake() {
        UIKeyBinding[] keyBinders = gameObject.GetComponents<UIKeyBinding>();
        if (!keyBinders.IsNullOrEmpty()) {
            var keyCodes = keyBinders.Select(kb => (int)kb.keyCode);
            var validKeys = ValidKeys.Cast<int>();
            if (!keyCodes.SequenceEquals(validKeys, ignoreOrder: true)) {
                D.WarnContext(gameObject, "{0} has one or more {1}s without corresponding list of ValidKeys.", DebugName, typeof(UIKeyBinding).Name);
            }
        }
    }

    #endregion

    // IDisposable Note: No reason to remove Ngui event currentListeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to this same GameObject that is being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.
}

