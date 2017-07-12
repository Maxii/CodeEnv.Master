// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MyPlaySFX.cs
// Version of Ngui's UIPlaySound that utilizes my SfxManager rather than NguiTools.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Version of Ngui's UIPlaySound that utilizes my SfxManager rather than NguiTools.
/// </summary>
public class MyPlaySFX : AMonoBase {

    public SfxClipID sfxClipID;
    public Trigger trigger = Trigger.OnClick;

    public string DebugName { get { return GetType().Name; } }

    private bool IsAllowedToPlay {
        get {
            if (!enabled) {
                return false;
            }
            UIButton btn = GetComponent<UIButton>();
            if (btn == null || btn.isEnabled) {
                // Doesn't require a button, but if there is one it needs to be enabled
                return true;
            }
            return false;
        }
    }

    private bool _isOver = false;

    protected override void Awake() {
        base.Awake();
        if (sfxClipID == SfxClipID.None) {
            D.WarnContext(gameObject, "{0}: {1} is not set.", DebugName, typeof(SfxClipID).Name);
            enabled = false;
        }
    }

    #region Event and Property Change Handlers

    protected override void OnEnable() {
        base.OnEnable();
        if (trigger == Trigger.OnEnable) {
            SFXManager.Instance.PlaySFX(sfxClipID);
        }
    }

    protected override void OnDisable() {
        base.OnDisable();
        if (trigger == Trigger.OnDisable) {
            SFXManager.Instance.PlaySFX(sfxClipID);
        }
    }

    void OnHover(bool isOver) {
        if (trigger == Trigger.OnMouseOver) {
            if (_isOver == isOver) {
                return;
            }
            _isOver = isOver;
        }

        if (IsAllowedToPlay && ((isOver && trigger == Trigger.OnMouseOver) || (!isOver && trigger == Trigger.OnMouseOut))) {
            SFXManager.Instance.PlaySFX(sfxClipID);
        }
    }

    void OnPress(bool isPressed) {
        if (trigger == Trigger.OnPress) {
            if (_isOver == isPressed) {
                return;
            }
            _isOver = isPressed;
        }

        if (IsAllowedToPlay && ((isPressed && trigger == Trigger.OnPress) || (!isPressed && trigger == Trigger.OnRelease))) {
            SFXManager.Instance.PlaySFX(sfxClipID);
        }
    }

    void OnClick() {
        if (IsAllowedToPlay && trigger == Trigger.OnClick) {
            SFXManager.Instance.PlaySFX(sfxClipID);
        }
    }

    void OnSelect(bool isSelected) {
        if (IsAllowedToPlay && (!isSelected || UICamera.currentScheme == UICamera.ControlScheme.Controller)) {
            OnHover(isSelected);
        }
    }

    #endregion

    public void Play() {
        SFXManager.Instance.PlaySFX(sfxClipID);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return DebugName;
    }

    #region Nested Classes

    public enum Trigger {
        OnClick,
        OnMouseOver,
        OnMouseOut,
        OnPress,
        OnRelease,
        OnEnable,
        OnDisable,
    }

    #endregion

}

