// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MyNguiToggleButton.cs
// Button that toggles between 'in' and 'out' when clicked.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using MoreLinq;
using UnityEngine;

/// <summary>
/// Button that toggles between 'in' and 'out' when clicked.
/// </summary>
public class MyNguiToggleButton : ATextTooltip {

    private const string DebugNameFormat = "{0}[{1}]";

    public event EventHandler toggleStateChanged;

    [SerializeField]
    private string _tooltipContent = null;

    public override string DebugName {
        get {
            if (_tooltipContent.IsNullOrEmpty()) {
                return base.DebugName;
            }
            return DebugNameFormat.Inject(base.DebugName, _tooltipContent);
        }
    }

    public bool IsEnabled {
        get { return _button.isEnabled; }
        set { _button.isEnabled = value; }
    }

    public bool IsToggledIn { get { return _toggle.value; } }

    protected override string TooltipContent { get { return _tooltipContent; } }

    private UIButton _button;
    private UIToggle _toggle;
    private UISprite _icon;

    public void Initialize(string tooltipContent = null) {
        InitializeValuesAndReferences();
        _toggle.Start();    // before subscribing so toggleStateChanged doesn't fire
        Subscribe();
        __Validate();
        if (!_tooltipContent.IsNullOrEmpty()) {
            if (tooltipContent != null) {
                // attempting to overwrite tooltip entered via the editor
                D.Warn("{0} not allowed to overwrite EditorTooltip {1} with CodeTooltip {2}.", DebugName, _tooltipContent, tooltipContent);
            }
            return;
        }
        _tooltipContent = tooltipContent;
    }

    private void InitializeValuesAndReferences() {
        _button = GetComponent<UIButton>();
        _toggle = GetComponent<UIToggle>();
        _toggle.startsActive = false;
        _toggle.group = Constants.Zero;
        _icon = gameObject.GetComponentsInImmediateChildren<UISprite>().MaxBy(s => s.depth);
    }

    private void Subscribe() {
        EventDelegate.Add(_toggle.onChange, ToggleStateChangedEventHandler);
    }

    public void SetToggledState(bool toToggleIn, GameColor iconColor = GameColor.White, bool toNotify = false) {
        _toggle.Set(toToggleIn, toNotify);
        SetIconColor(iconColor);
    }

    public void SetIconColor(GameColor color) {
        _icon.color = color.ToUnityColor();
    }

    #region Event and Property Change Handlers

    private void ToggleStateChangedEventHandler() {
        OnToggleStateChanged();
    }

    private void OnToggleStateChanged() {
        if (toggleStateChanged != null) {
            toggleStateChanged(this, EventArgs.Empty);
        }
    }

    #endregion

    private void Unsubscribe() {
        EventDelegate.Remove(_toggle.onChange, ToggleStateChangedEventHandler);
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    #region Debug

    private void __Validate() {
        int normalButtonSpriteDepth = _button.GetComponent<UISprite>().depth;
        var toggleButtonSprite = gameObject.GetComponentsInImmediateChildren<UISprite>().Single(s => s != _icon);
        int toggledButtonSpriteDepth = toggleButtonSprite.depth;
        D.Assert(toggledButtonSpriteDepth > normalButtonSpriteDepth);
        D.Assert(_icon.depth > toggledButtonSpriteDepth);
        D.AssertEqual(toggleButtonSprite, _toggle.activeSprite);
    }

    #endregion

}

