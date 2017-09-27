// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AElementConstructionGuiIcon.cs
// Abstract base AMultiSizeGuiIcon that holds a ElementConstructionTracker for an AUnitElementDesign under construction.
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
/// Abstract base AMultiSizeGuiIcon that holds a ElementConstructionTracker for an AUnitElementDesign under construction.
/// </summary>
public abstract class AElementConstructionGuiIcon : AMultiSizeGuiIcon {

    private const string DebugNameFormat = "{0}[{1}]";
    private const string TooltipFormat = "Building {0}";
    private const string RemainingConstructionTimeFormat = "{0:0.0}";
    private const string BuyoutCostFormat = "{0:0.#}K";

    public event EventHandler constructionBuyoutInitiated;
    public event EventHandler dragDropEnded;

    public sealed override string DebugName {
        get {
            if (ItemUnderConstruction == null) {
                return DebugNameFormat.Inject(GetType().Name, "No Construction");
            }
            return DebugNameFormat.Inject(GetType().Name, ItemUnderConstruction.Design.DesignName);
        }
    }

    private ElementConstructionTracker _itemUnderConstruction;
    public ElementConstructionTracker ItemUnderConstruction {
        get { return _itemUnderConstruction; }
        set {
            D.AssertNull(_itemUnderConstruction);
            D.AssertNotNull(value);
            _itemUnderConstruction = value;
            ItemUnderConstructionPropSetHandler();
        }
    }

    protected override string TooltipContent { get { return TooltipFormat.Inject(ItemUnderConstruction.Design.DesignName); } }

    private UIWidget _remainingTimeContainer;
    private UILabel _buyoutCostLabel;
    private UIButton _buyoutButton;
    private UILabel _remainingTimeLabel;
    private UIProgressBar _progressBar;
    private MyDragDropItem _dragDropItem;

    protected override UISprite AcquireImageSprite(GameObject topLevelIconGo) {
        return topLevelIconGo.GetComponentsInImmediateChildren<UISprite>().Single(s => s.transform.childCount == Constants.Zero);
    }

    protected override void AcquireAdditionalIconWidgets(GameObject topLevelIconGo) {
        _progressBar = topLevelIconGo.GetComponentInChildren<UIProgressBar>();
        _buyoutButton = topLevelIconGo.GetSingleComponentInChildren<UIButton>();
        NGUITools.AddWidgetCollider(_buyoutButton.gameObject);   // does nothing if already present
        D.Assert(_buyoutButton.isEnabled);
        _buyoutCostLabel = _buyoutButton.gameObject.GetSingleComponentInChildren<UILabel>();

        var immediateChildSprites = topLevelIconGo.GetComponentsInImmediateChildren<UISprite>();
        _remainingTimeContainer = immediateChildSprites.Single(s => s.transform.childCount > Constants.Zero
            && s.GetComponent<UIProgressBar>() == null && s.GetComponent<UIButton>() == null).GetComponent<UIWidget>();

        NGUITools.AddWidgetCollider(_remainingTimeContainer.gameObject);   // does nothing if already present
        _remainingTimeLabel = _remainingTimeContainer.gameObject.GetSingleComponentInChildren<UILabel>();
        _dragDropItem = GetComponent<MyDragDropItem>();
        Subscribe();
    }

    private void ShowRemainingConstructionTimeTooltip(bool toShow) {
        if (toShow) {
            TooltipHudWindow.Instance.Show("Hours to completion");
        }
        else {
            TooltipHudWindow.Instance.Hide();
        }
    }

    #region Event and Property Change Handlers

    private void DragDropEndedEventHandler(object sender, EventArgs e) {
        D.AssertEqual(_dragDropItem, sender as MyDragDropItem);
        OnDragDropEnded();
    }

    private void ItemUnderConstructionPropSetHandler() {
        Show();
    }

    private void RemainingBuildTimeTooltipEventHandler(GameObject go, bool toShow) {
        ShowRemainingConstructionTimeTooltip(toShow);
    }

    void OnHover(bool isOver) {
        IconHoveredEventHandler(isOver);
    }

    private void IconHoveredEventHandler(bool isOver) {
        HandleIconHovered(isOver);
    }

    private void BuyoutButtonClickedEventHandler() {
        OnConstructionBuyoutInitiated();
    }

    private void OnConstructionBuyoutInitiated() {
        if (constructionBuyoutInitiated != null) {
            constructionBuyoutInitiated(this, EventArgs.Empty);
        }
    }

    private void OnDragDropEnded() {
        if (dragDropEnded != null) {
            dragDropEnded(this, EventArgs.Empty);
        }
    }

    #endregion

    private void HandleIconHovered(bool isOver) {
        if (isOver) {
            HoveredHudWindow.Instance.Show(FormID.UnitDesign, ItemUnderConstruction.Design);
        }
        else {
            HoveredHudWindow.Instance.Hide();
        }
    }

    private void Subscribe() {
        UIEventListener.Get(_remainingTimeContainer.gameObject).onTooltip += RemainingBuildTimeTooltipEventHandler;
        EventDelegate.Add(_buyoutButton.onClick, BuyoutButtonClickedEventHandler);
        _dragDropItem.dragDropEnded += DragDropEndedEventHandler;
    }

    private void Show() {
        _progressBar.value = ItemUnderConstruction.CompletionPercentage;
        _remainingTimeLabel.text = RemainingConstructionTimeFormat.Inject(ItemUnderConstruction.TimeToCompletion.TotalInHours);
        _buyoutCostLabel.text = CreateBuyoutCostText();
        _buyoutButton.isEnabled = ItemUnderConstruction.CanBuyout;
        Show(ItemUnderConstruction.Design.ImageAtlasID, ItemUnderConstruction.Design.ImageFilename, ItemUnderConstruction.Design.DesignName);
    }

    private string CreateBuyoutCostText() {
        decimal buyoutKCost = ItemUnderConstruction.BuyoutCost / 1000;
        return BuyoutCostFormat.Inject(buyoutKCost);
    }

    protected sealed override void HandleIconSizeSet() {
        base.HandleIconSizeSet();
        ResizeWidgetAndAnchorIcon();
    }

    private void ResizeWidgetAndAnchorIcon() {
        IntVector2 iconDimensions = GetIconDimensions(Size);
        UIWidget topLevelWidget = GetComponent<UIWidget>();
        topLevelWidget.SetDimensions(iconDimensions.x, iconDimensions.y);
        AnchorTo(topLevelWidget);
    }

    public override void ResetForReuse() {
        base.ResetForReuse();
        if (_buyoutButton != null) {
            _buyoutButton.isEnabled = true;
        }
        _itemUnderConstruction = null;
    }

    #region Cleanup

    private void Unsubscribe() {
        // Icons can be destroyed before they are ever initialized when resident in form for debug purposes
        if (_remainingTimeContainer != null) {
            UIEventListener.Get(_remainingTimeContainer.gameObject).onTooltip -= RemainingBuildTimeTooltipEventHandler;
        }
        if (_buyoutButton != null) {
            EventDelegate.Remove(_buyoutButton.onClick, BuyoutButtonClickedEventHandler);
        }
        if (_dragDropItem != null) {
            _dragDropItem.dragDropEnded -= DragDropEndedEventHandler;
        }
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    #endregion

    #region Debug

    protected override void __Validate() {
        base.__Validate();
        UnityUtility.ValidateComponentPresence<BoxCollider>(gameObject);
    }

    #endregion

}

