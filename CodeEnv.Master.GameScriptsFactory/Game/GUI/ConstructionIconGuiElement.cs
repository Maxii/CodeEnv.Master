// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ConstructionIconGuiElement.cs
// AIconGuiElement that represents a Base's CurrentConstruction, including NoConstructionInfo and Unknown.
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
using UnityEngine;

/// <summary>
/// AIconGuiElement that represents a Base's CurrentConstruction, including NoConstructionInfo and Unknown.
/// </summary>
public class ConstructionIconGuiElement : AIconGuiElement, IComparable<ConstructionIconGuiElement> {

    private const string DebugNameFormat = "{0}[{1}]";
    private const string TooltipFormat = "Building {0}";
    private const string RemainingConstructionTimeFormat = "{0:0.0}";
    private const string BuyoutCostFormat = "{0:0.#}K";

    public override GuiElementID ElementID { get { return GuiElementID.Construction; } }

    public sealed override string DebugName {
        get {
            string constructionName = Construction != null ? Construction.Name : Unknown;
            return DebugNameFormat.Inject(GetType().Name, constructionName);
        }
    }

    public override bool IsInitialized { get { return _isConstructionPropSet; } }

    private bool _isConstructionPropSet;  // reqd as Construction can be Construction, NoConstruction or null (unknown)
    private Construction _construction;
    public Construction Construction {
        get { return _construction; }
        set {
            D.Assert(!_isConstructionPropSet);   // occurs only once between Resets
            _construction = value;
            ConstructionPropSetHandler();
        }
    }

    private string _tooltipContent;
    protected override string TooltipContent { get { return _tooltipContent; } }

    private UIWidget _remainingTimeContainer;
    private UILabel _buyoutCostLabel;
    private UIButton _buyoutButton;
    private UILabel _remainingTimeLabel;
    private UIProgressBar _progressBar;
    private UILabel _iconImageNameLabel;

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override UISprite AcquireIconImageSprite() {
        return gameObject.GetComponentsInImmediateChildren<UISprite>().Single(s => s.transform.childCount == Constants.Zero); ;
    }

    protected override void AcquireAdditionalWidgets() {
        _iconImageNameLabel = gameObject.GetSingleComponentInImmediateChildren<UILabel>();

        _progressBar = gameObject.GetSingleComponentInChildren<UIProgressBar>();
        _buyoutButton = gameObject.GetSingleComponentInChildren<UIButton>();
        _buyoutButton.isEnabled = false;
        //NGUITools.AddWidgetCollider(_buyoutButton.gameObject);   // 10.3.17 Not currently implemented
        _buyoutCostLabel = _buyoutButton.gameObject.GetSingleComponentInChildren<UILabel>();

        var immediateChildSprites = gameObject.GetComponentsInImmediateChildren<UISprite>();
        _remainingTimeContainer = immediateChildSprites.Single(s => s.transform.childCount > Constants.Zero
            && s.GetComponent<UIProgressBar>() == null && s.GetComponent<UIButton>() == null).GetComponent<UIWidget>();

        NGUITools.AddWidgetCollider(_remainingTimeContainer.gameObject);   // does nothing if already present
        _remainingTimeLabel = _remainingTimeContainer.gameObject.GetSingleComponentInChildren<UILabel>();
    }

    private void Subscribe() {
        UIEventListener.Get(_remainingTimeContainer.gameObject).onTooltip += RemainingConstructionTimeTooltipEventHandler;
    }

    #region Event and Property Change Handlers

    private void ConstructionPropSetHandler() {
        _isConstructionPropSet = true;
        if (IsInitialized) {
            PopulateMemberWidgetValues();
            Show();
        }
    }

    private void RemainingConstructionTimeTooltipEventHandler(GameObject go, bool toShow) {
        ShowRemainingConstructionTimeTooltip(toShow);
    }

    #endregion

    protected override void PopulateMemberWidgetValues() {
        base.PopulateMemberWidgetValues();
        if (Construction == null) {
            HandleValuesUnknown();
            return;
        }

        _iconImageSprite.atlas = Construction.ImageAtlasID.GetAtlas();
        _iconImageSprite.spriteName = Construction.ImageFilename;
        _iconImageNameLabel.text = Construction.Name;
        _remainingTimeLabel.text = RemainingConstructionTimeFormat.Inject(Construction.TimeToCompletion.TotalInHours);
        _buyoutCostLabel.text = CreateBuyoutCostText();
        _progressBar.value = Construction.CompletionPercentage;
        _tooltipContent = TooltipFormat.Inject(Construction.Name);
    }

    protected override void HandleValuesUnknown() {
        base.HandleValuesUnknown();
        _iconImageNameLabel.text = Unknown;
        _remainingTimeLabel.text = Unknown;
        _buyoutCostLabel.text = Unknown;
        _progressBar.value = Constants.ZeroPercent;
        _tooltipContent = TooltipFormat.Inject(Unknown);
    }

    protected override void HandleGuiElementHovered(bool isOver) {
        if (isOver) {
            if (Construction != null && Construction != TempGameValues.NoConstruction) {
                HoveredHudWindow.Instance.Show(FormID.Design, Construction.Design);
            }
        }
        else {
            HoveredHudWindow.Instance.Hide();
        }
    }

    private void ShowRemainingConstructionTimeTooltip(bool toShow) {
        if (toShow) {
            TooltipHudWindow.Instance.Show("Hours to completion");
        }
        else {
            TooltipHudWindow.Instance.Hide();
        }
    }

    private string CreateBuyoutCostText() {
        decimal buyoutKCost = Construction.BuyoutCost / 1000;
        return BuyoutCostFormat.Inject(buyoutKCost);
    }

    public override void ResetForReuse() {
        _construction = null;
        _isConstructionPropSet = false;
    }

    #region Cleanup

    private void Unsubscribe() {
        // Icons can be destroyed before they are ever initialized when resident in form for debug purposes
        if (_remainingTimeContainer != null) {
            UIEventListener.Get(_remainingTimeContainer.gameObject).onTooltip -= RemainingConstructionTimeTooltipEventHandler;
        }
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    #endregion

    #region Debug

    #endregion

    #region IComparable<ConstructionIconGuiElement> Members

    public int CompareTo(ConstructionIconGuiElement other) {
        int result;
        var noConstruction = TempGameValues.NoConstruction;
        if (Construction == noConstruction) {
            result = other.Construction == noConstruction ? Constants.Zero : Constants.MinusOne;
        }
        else if (Construction == null) {
            // unknown Construction (Construction == null) sorts higher than NoConstructionInfo
            result = other.Construction == null ? Constants.Zero : (other.Construction == noConstruction) ? Constants.One : Constants.MinusOne;
        }
        else {
            result = (other.Construction == noConstruction || other.Construction == null) ? Constants.One :
                Construction.ExpectedCompletionDate.CompareTo(other.Construction.ExpectedCompletionDate);
        }
        return result;
    }

    #endregion

}

