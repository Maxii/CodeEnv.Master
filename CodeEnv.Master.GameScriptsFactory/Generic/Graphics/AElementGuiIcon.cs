// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AElementGuiIcon.cs
// Abstract base AMultiSizeGuiIcon that holds an AUnitElement.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base AMultiSizeGuiIcon that holds an AUnitElement.
/// <remarks>This is an icon used by the Gui, not the in game icon that tracks a element in space.</remarks>
/// </summary>
public abstract class AElementGuiIcon : AMultiSizeGuiIcon {

    private const string DebugNameFormat = "{0}[{1}]";
    private const string TooltipFormat = "{0}";

    public override string DebugName {
        get {
            if (Element == null) {
                return DebugNameFormat.Inject(GetType().Name, "No Element");
            }
            return DebugNameFormat.Inject(GetType().Name, Element.Name);
        }
    }

    /// <summary>
    /// Indicates whether this Icon has been initialized, aka its Element property has been set.
    /// <remarks>Handled this way as Element will return null if Element is destroyed.</remarks>
    /// </summary>
    public bool IsInitialized { get; private set; }

    private AUnitElementItem _element;
    public AUnitElementItem Element {
        get { return _element; }
        set {
            D.AssertNull(_element);
            D.AssertNotNull(value);
            _element = value;
            ElementPropSetHandler();
        }
    }

    private bool _isPicked;
    public bool IsPicked {
        get { return _isPicked; }
        set {
            if (_isPicked != value) {
                _isPicked = value;
                IsPickedPropChangedHandler();
            }
        }
    }

    protected override string TooltipContent { get { return TooltipFormat.Inject(_element.Name); } }

    private UISlider _healthBar;
    private AUnitElementDesign _design;
    private IList<IDisposable> _subscriptions;

    protected override UISprite AcquireImageSprite(GameObject topLevelIconGo) {
        return topLevelIconGo.GetComponentsInImmediateChildren<UISprite>().Single(s => s.GetComponent<UISlider>() == null);
    }

    protected override void AcquireAdditionalIconWidgets(GameObject topLevelIconGo) {
        _healthBar = topLevelIconGo.GetComponentInChildren<UISlider>();
    }

    #region Event and Property Change Handlers

    private void ElementPropSetHandler() {
        D.AssertNotDefault((int)Size);
        D.Assert(!IsInitialized);
        IsInitialized = true;
        AssignValuesToMembers();
        Subscribe();
        Show();
    }

    private void IsPickedPropChangedHandler() {
        HandleIsPickedChanged();
    }

    void OnHover(bool isOver) {
        ElementIconHoveredEventHandler(isOver);
    }

    private void ElementIconHoveredEventHandler(bool isOver) {
        Element.ShowHoveredHud(isOver);
    }

    private void ElementHealthPropChangedHandler() {
        D.Assert(IsShowing);
        _healthBar.value = Element.Data.Health;
    }

    #endregion

    private void Subscribe() {
        _subscriptions = _subscriptions ?? new List<IDisposable>();
        _subscriptions.Add(Element.Data.SubscribeToPropertyChanged<AUnitElementData, float>(eData => eData.Health, ElementHealthPropChangedHandler));
    }

    private void Show(GameColor color = GameColor.White) {
        _healthBar.value = Element.Data.Health;
        Show(_design.ImageAtlasID, _design.ImageFilename, Element.Name, color);
    }

    private void HandleIsPickedChanged() {
        D.Assert(IsShowing);
        if (IsPicked) {
            //D.Log("{0} has been picked.", DebugName);
            Show(TempGameValues.SelectedColor);
            SFXManager.Instance.PlaySFX(SfxClipID.Select);
        }
        else {
            //D.Log("{0} has been unpicked.", DebugName);
            Show();
            SFXManager.Instance.PlaySFX(SfxClipID.UnSelect);
        }
    }

    private void AssignValuesToMembers() {
        _design = InitializeDesign();
    }

    protected abstract AUnitElementDesign InitializeDesign();

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
        Unsubscribe();
        _element = null;
        _design = null;
        _isPicked = false;
        IsInitialized = false;
    }

    #region Cleanup

    private void Unsubscribe() {
        if (_subscriptions != null) {    // can be null if destroyed before Element is assigned
            _subscriptions.ForAll(d => d.Dispose());
            _subscriptions.Clear();
        }
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    #endregion

}

