// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AProgressBarGuiElement.cs
// Abstract AGuiElement that represents a Progress Bar.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract AGuiElement that represents a Progress Bar.
/// </summary>
public abstract class AProgressBarGuiElement : AGuiElement {

#pragma warning disable 0649

    [SerializeField]
    private UILabel _unknownLabel;    // contains "?"

    [SerializeField]
    private string _tooltipContent;

#pragma warning restore 0649

    protected sealed override string TooltipContent { get { return _tooltipContent; } }

    private UILabel _progressBarValueTextLabel;
    private UIProgressBar _progressBar;
    private UISprite _progressBarForeground;


    protected override void InitializeValuesAndReferences() {
        _progressBar = gameObject.GetSingleComponentInChildren<UIProgressBar>();
        _progressBarForeground = _progressBar.gameObject.GetSingleComponentInImmediateChildren<UISprite>();

        var otherLabels = gameObject.GetComponentsInChildren<UILabel>().Except(_unknownLabel);
        if (otherLabels.Any()) {
            _progressBarValueTextLabel = otherLabels.Single();
        }

        NGUITools.SetActive(_unknownLabel.gameObject, false);
    }

    protected void PopulateProgressBarValues(float barValue, GameColor barForegroundColor, string barValueText) {
        _progressBar.value = barValue;
        _progressBarForeground.color = barForegroundColor.ToUnityColor();
        if (_progressBarValueTextLabel != null) {
            _progressBarValueTextLabel.text = barValueText;
        }
    }

    protected void HandleValuesUnknown() {
        NGUITools.SetActive(_unknownLabel.gameObject, true);
        NGUITools.SetActive(_progressBar.gameObject, false);
        if (_progressBarValueTextLabel != null) {
            NGUITools.SetActive(_progressBarValueTextLabel.gameObject, false);
        }
    }

    #region Debug

    protected override void __ValidateOnAwake() {
        base.__ValidateOnAwake();
        D.AssertNotNull(_unknownLabel);
    }

    #endregion

}

