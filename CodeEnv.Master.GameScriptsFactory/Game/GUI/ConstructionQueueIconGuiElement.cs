// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ConstructionQueueIconGuiElement.cs
// AMultiSizeIconGuiElement that represents a Construction in a Base's ConstructionQueue.
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
/// AMultiSizeIconGuiElement that represents a Construction in a Base's ConstructionQueue. Also supports
/// buyout and drag and drop functionality.
/// <remarks>9.12.17 No need for facility or ship-specific versions as only names and images are needed from the design for the icon.</remarks>
/// </summary>
public class ConstructionQueueIconGuiElement : AMultiSizeIconGuiElement {

    private const string DebugNameFormat = "{0}[{1}]";
    private const string TooltipFormat = "Building {0}";
    private const string RemainingConstructionTimeFormat = "{0:0.0}";
    private const string BuyoutCostFormat = "{0:0.#}K";

    public event EventHandler constructionBuyoutInitiated;
    public event EventHandler dragDropEnded;

    public override GuiElementID ElementID { get { return GuiElementID.Construction; } }

    public sealed override string DebugName {
        get {
            string constructionName = Construction != null ? Construction.Name : "Construction not yet assigned";
            return DebugNameFormat.Inject(GetType().Name, constructionName);
        }
    }

    public override bool IsInitialized { get { return Size != default(IconSize) && Construction != null; } }

    private ConstructionTask _construction;
    public ConstructionTask Construction {
        get { return _construction; }
        set {
            D.AssertNotEqual(TempGameValues.NoConstruction, _construction);
            D.AssertNotEqual(TempGameValues.NoConstruction, value);
            D.AssertNotNull(value);
            _construction = value;
            ConstructionPropSetHandler();
        }
    }

    protected override string TooltipContent { get { return TooltipFormat.Inject(Construction.Name); } }

    private UILabel _iconImageNameLabel;
    private UIWidget _iconRemainingTimeContainer;
    private UILabel _iconBuyoutCostLabel;
    private UIButton _iconBuyoutButton;
    private UILabel _iconRemainingTimeLabel;
    private UIProgressBar _iconProgressBar;
    private MyDragDropItem _dragDropItem;

    protected override void Awake() {
        base.Awake();
        _dragDropItem = GetComponent<MyDragDropItem>();
        SubscribeToDragDrop();
    }

    private void SubscribeToDragDrop() {
        _dragDropItem.dragDropEnded += DragDropEndedEventHandler;
    }

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        SubscribeToIconWidgets();
    }

    protected override UISprite AcquireIconImageSprite() {
        return _topLevelIconWidget.gameObject.GetComponentsInImmediateChildren<UISprite>().Single(s => s.transform.childCount == Constants.Zero);
    }

    protected override void AcquireAdditionalWidgets() {
        _iconImageNameLabel = _topLevelIconWidget.gameObject.GetSingleComponentInImmediateChildren<UILabel>();
        _iconProgressBar = _topLevelIconWidget.GetComponentInChildren<UIProgressBar>();
        _iconBuyoutButton = _topLevelIconWidget.gameObject.GetSingleComponentInChildren<UIButton>();
        NGUITools.AddWidgetCollider(_iconBuyoutButton.gameObject);   // does nothing if already present
        _iconBuyoutCostLabel = _iconBuyoutButton.gameObject.GetSingleComponentInChildren<UILabel>();

        var immediateChildSprites = _topLevelIconWidget.gameObject.GetComponentsInImmediateChildren<UISprite>();
        _iconRemainingTimeContainer = immediateChildSprites.Single(s => s.transform.childCount > Constants.Zero
            && s.GetComponent<UIProgressBar>() == null && s.GetComponent<UIButton>() == null).GetComponent<UIWidget>();

        NGUITools.AddWidgetCollider(_iconRemainingTimeContainer.gameObject);   // does nothing if already present
        _iconRemainingTimeLabel = _iconRemainingTimeContainer.gameObject.GetSingleComponentInChildren<UILabel>();
    }

    private void SubscribeToIconWidgets() {
        UIEventListener.Get(_iconRemainingTimeContainer.gameObject).onTooltip += RemainingConstructionTimeTooltipEventHandler;
        EventDelegate.Add(_iconBuyoutButton.onClick, BuyoutButtonClickedEventHandler);
    }

    protected override void PopulateMemberWidgetValues() {
        base.PopulateMemberWidgetValues();
        _iconImageSprite.atlas = Construction.ImageAtlasID.GetAtlas();
        _iconImageSprite.spriteName = Construction.ImageFilename;
        _iconImageNameLabel.text = Construction.Name;
        _iconProgressBar.value = Construction.CompletionPercentage;
        _iconRemainingTimeLabel.text = RemainingConstructionTimeFormat.Inject(Construction.TimeToCompletion.TotalInHours);
        _iconBuyoutCostLabel.text = CreateBuyoutCostText();
        _iconBuyoutButton.isEnabled = Construction.CanBuyout;
    }

    #region Event and Property Change Handlers

    private void DragDropEndedEventHandler(object sender, EventArgs e) {
        D.AssertEqual(_dragDropItem, sender as MyDragDropItem);
        OnDragDropEnded();
    }

    private void ConstructionPropSetHandler() {
        PopulateMemberWidgetValues();
        Show();
    }

    private void RemainingConstructionTimeTooltipEventHandler(GameObject go, bool toShow) {
        HandleShowRemainingConstructionTimeTooltip(toShow);
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

    private void HandleShowRemainingConstructionTimeTooltip(bool toShow) {
        if (toShow) {
            TooltipHudWindow.Instance.Show("Hours to completion");
        }
        else {
            TooltipHudWindow.Instance.Hide();
        }
    }

    protected override void HandleGuiElementHovered(bool isOver) {
        if (isOver) {
            HoveredHudWindow.Instance.Show(FormID.Design, Construction.Design);
        }
        else {
            HoveredHudWindow.Instance.Hide();
        }
    }

    private string CreateBuyoutCostText() {
        decimal buyoutKCost = Construction.BuyoutCost / 1000;
        return BuyoutCostFormat.Inject(buyoutKCost);
    }

    public override void ResetForReuse() {
        base.ResetForReuse();
        UnsubscribeToIconWidgets();
        _construction = null;
        _iconImageNameLabel = null;
        _iconBuyoutButton = null;
        _iconBuyoutCostLabel = null;
        _iconProgressBar = null;
        _iconRemainingTimeContainer = null;
        _iconRemainingTimeLabel = null;
    }

    #region Cleanup

    private void UnsubscribeToIconWidgets() {
        // Icons can be destroyed before they are ever initialized when resident in form for debug purposes
        if (_iconRemainingTimeContainer != null) {
            UIEventListener.Get(_iconRemainingTimeContainer.gameObject).onTooltip -= RemainingConstructionTimeTooltipEventHandler;
        }
        if (_iconBuyoutButton != null) {
            EventDelegate.Remove(_iconBuyoutButton.onClick, BuyoutButtonClickedEventHandler);
        }
    }

    private void UnsubscribeToDragDrop() {
        _dragDropItem.dragDropEnded -= DragDropEndedEventHandler;
    }

    protected override void Cleanup() {
        UnsubscribeToIconWidgets();
        UnsubscribeToDragDrop();
    }

    #endregion

    #region Debug

    #endregion


}

