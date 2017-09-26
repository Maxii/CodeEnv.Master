// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AProgressBarGuiElement.cs
// Abstract GuiElement handling the display and tooltip content for Item attributes that use a Progress Bar.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Abstract GuiElement handling the display and tooltip content for Item attributes that use a Progress Bar.
/// </summary>
public abstract class AProgressBarGuiElement : AGuiElement {

    [SerializeField]
    private UILabel _unknownLabel;    // contains "?"
    [SerializeField]
    private string _tooltipContent;
    protected sealed override string TooltipContent { get { return _tooltipContent; } }

    private UILabel _barValueTextLabel;
    private UIProgressBar _progressBar;
    private UISprite _progressBarForeground;

    protected sealed override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
    }

    private void InitializeValuesAndReferences() {
        _progressBar = gameObject.GetSingleComponentInChildren<UIProgressBar>();
        _progressBarForeground = _progressBar.gameObject.GetSingleComponentInImmediateChildren<UISprite>();

        var otherLabels = gameObject.GetComponentsInChildren<UILabel>().Except(_unknownLabel);
        if (otherLabels.Any()) {
            _barValueTextLabel = otherLabels.Single();
        }

        NGUITools.SetActive(_unknownLabel.gameObject, false);
    }

    protected abstract bool AreAllValuesSet { get; }

    protected void PopulateValues(float barValue, GameColor barForegroundColor, string barValueText) {
        _progressBar.value = barValue;
        _progressBarForeground.color = barForegroundColor.ToUnityColor();
        if (_barValueTextLabel != null) {
            _barValueTextLabel.text = barValueText;
        }
    }

    protected void HandleValuesUnknown() {
        NGUITools.SetActive(_unknownLabel.gameObject, true);
        NGUITools.SetActive(_progressBar.gameObject, false);
        if (_barValueTextLabel != null) {
            NGUITools.SetActive(_barValueTextLabel.gameObject, false);
        }
    }

    #region Debug

    protected override void __Validate() {
        base.__Validate();
        D.AssertNotNull(_unknownLabel);
    }

    #endregion

}

