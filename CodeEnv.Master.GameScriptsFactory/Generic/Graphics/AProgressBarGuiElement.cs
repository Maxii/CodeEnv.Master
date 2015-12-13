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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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

    //[FormerlySerializedAs("widgetsPresent")]
    [Tooltip("The widgets that are present to display the content of this GuiElement.")]
    [SerializeField]
    protected WidgetsPresent _widgetsPresent = WidgetsPresent.Both;

    protected string _tooltipContent;
    protected sealed override string TooltipContent { get { return _tooltipContent; } }

    protected string _detailValuesContent;
    protected UILabel _detailValuesLabel;

    private UISlider _slider;
    private UISprite _barForeground;
    private UILabel _unknownLabel;    // contains "?"

    protected sealed override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
    }

    private void InitializeValuesAndReferences() {
        var labels = gameObject.GetSafeComponentsInImmediateChildren<UILabel>();
        _unknownLabel = labels.Single(l => l.gameObject.name == TempGameValues.UnknownLabelName);
        switch (_widgetsPresent) {
            case WidgetsPresent.ProgressBar:
                _slider = gameObject.GetSingleComponentInChildren<UISlider>();
                _barForeground = _slider.gameObject.GetSingleComponentInImmediateChildren<UISprite>();
                NGUITools.AddWidgetCollider(gameObject);
                break;
            case WidgetsPresent.Label:
                _detailValuesLabel = labels.Except(_unknownLabel).Single();
                break;
            case WidgetsPresent.Both:
                _slider = gameObject.GetSingleComponentInChildren<UISlider>();
                _barForeground = _slider.gameObject.GetSingleComponentInImmediateChildren<UISprite>();
                _detailValuesLabel = labels.Except(_unknownLabel).Single();
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_widgetsPresent));
        }
        NGUITools.SetActive(_unknownLabel.gameObject, false);
    }

    protected abstract bool AreAllValuesSet { get; }

    protected abstract void PopulateElementWidgets();

    protected void PopulateProgressBarValues(float value, GameColor color) {
        _slider.value = value;
        _barForeground.color = color.ToUnityColor();
    }

    protected void HandleValuesUnknown() {
        NGUITools.SetActive(_unknownLabel.gameObject, true);
        switch (_widgetsPresent) {
            case WidgetsPresent.ProgressBar:
                NGUITools.SetActive(_slider.gameObject, false);
                break;
            case WidgetsPresent.Label:
                NGUITools.SetActive(_detailValuesLabel.gameObject, false);
                break;
            case WidgetsPresent.Both:
                NGUITools.SetActive(_slider.gameObject, false);
                NGUITools.SetActive(_detailValuesLabel.gameObject, false);
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_widgetsPresent));
        }
    }

    public sealed override string ToString() { return GetType().Name + Constants.Space + _detailValuesContent; }

    #region Nested Classes

    /// <summary>
    /// Enum that identifies the Widget's that are present in this GuiElement.
    /// </summary>
    public enum WidgetsPresent {

        /// <summary>
        /// A multi-widget ProgressBar for showing the content of this GuiElement.
        /// </summary>
        ProgressBar,

        /// <summary>
        /// A label for showing the content of this GuiElement in text form.
        /// </summary>
        Label,

        /// <summary>
        /// Both widgets are present.
        /// </summary>
        Both

    }

    #endregion

}

