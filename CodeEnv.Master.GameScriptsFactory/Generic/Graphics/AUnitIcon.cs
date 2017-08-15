// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitIcon.cs
// Abstract base AGuiIcon that holds an AUnitCmdItem.
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
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base AGuiIcon that holds an AUnitCmdItem.
/// </summary>
public abstract class AUnitIcon : AGuiIcon {

    private const string DebugNameFormat = "{0}[{1}]";
    private const string TooltipFormat = "{0}";
    private const string UnitCompositionFormat = "{0}/{1}";

    public override string DebugName {
        get {
            if (Unit == null) {
                return DebugNameFormat.Inject(GetType().Name, "No Unit");
            }
            return DebugNameFormat.Inject(GetType().Name, Unit.Name);
        }
    }

    /// <summary>
    /// Indicates whether this Icon has been initialized, aka its Unit property has been set.
    /// <remarks>Warning: Unit will return null if Unit is destroyed.</remarks>
    /// </summary>
    public bool IsInitialized { get; private set; }

    private AUnitCmdItem _unit;
    public AUnitCmdItem Unit {
        get { return _unit; }
        set {
            D.AssertNull(_unit);
            D.AssertNotNull(value);
            _unit = value;
            UnitPropSetHandler();
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

    protected abstract int MaxElementsPerUnit { get; }

    /// <summary>
    /// Filename for the icon in MyGuiAtlas that represents this Unit Type, aka Fleet, Settlement or Starbase.
    /// </summary>
    protected abstract string UnitImageFilename { get; }

    protected sealed override string TooltipContent { get { return TooltipFormat.Inject(_unit.UnitName); } }

    protected IList<IDisposable> _subscriptions;
    private UISprite _unitCompositionIcon;
    private UILabel _unitCompositionLabel;
    private UISlider _healthBar;

    protected sealed override UISprite AcquireImageSprite(GameObject topLevelIconGo) {
        return topLevelIconGo.GetComponentsInImmediateChildren<UISprite>().Single(s => s.GetComponent<UISlider>() == null
            && s.GetComponentInChildren<GuiElement>() == null);
    }

    protected sealed override UILabel AcquireNameLabel(GameObject topLevelIconGo) {
        return topLevelIconGo.GetComponentsInChildren<GuiElement>().Single(ge => ge.ElementID == GuiElementID.NameLabel).GetComponent<UILabel>();
    }

    protected override void AcquireAdditionalIconWidgets(GameObject topLevelIconGo) {
        _healthBar = topLevelIconGo.GetComponentInChildren<UISlider>();
        var compositionContainerGo = topLevelIconGo.GetComponentsInChildren<GuiElement>().Single(ge => ge.ElementID == GuiElementID.Composition).gameObject;
        _unitCompositionIcon = compositionContainerGo.GetSingleComponentInChildren<UISprite>();
        _unitCompositionLabel = compositionContainerGo.GetSingleComponentInChildren<UILabel>();
    }

    #region Event and Property Change Handlers

    private void UnitPropSetHandler() {
        D.AssertNotDefault((int)Size);
        D.Assert(!IsInitialized);
        IsInitialized = true;
        AssignValuesToMembers();
        Subscribe();
        Show();
    }

    private void IsPickedPropChangedHandler() {
        D.Assert(IsShowing);
        HandleIsPickedChanged();
    }

    void OnHover(bool isOver) {
        Unit.ShowHoveredHud(isOver);
    }

    private void UnitHealthPropChangedHandler() {
        D.Assert(IsShowing);
        _healthBar.value = Unit.Data.UnitHealth;
    }

    private void UnitIconInfoPropChangedHandler() {
        D.Assert(IsShowing);
        _unitCompositionIcon.atlas = Unit.DisplayMgr.IconInfo.AtlasID.GetAtlas();
        _unitCompositionIcon.spriteName = Unit.DisplayMgr.IconInfo.Filename;
    }

    private void UnitNamePropChangedHandler() {
        D.Assert(IsShowing);
        if (IsPicked) {
            Show(TempGameValues.SelectedColor);
        }
        else {
            Show();
        }
    }

    #endregion

    protected virtual void Subscribe() {
        _subscriptions = _subscriptions ?? new List<IDisposable>();
        _subscriptions.Add(Unit.Data.SubscribeToPropertyChanged<AUnitCmdData, float>(data => data.UnitHealth, UnitHealthPropChangedHandler));
        _subscriptions.Add(Unit.DisplayMgr.SubscribeToPropertyChanged<UnitCmdDisplayManager, TrackingIconInfo>(unit => unit.IconInfo, UnitIconInfoPropChangedHandler));
        _subscriptions.Add(Unit.Data.SubscribeToPropertyChanged<AUnitCmdData, string>(data => data.UnitName, UnitNamePropChangedHandler));
    }

    protected virtual void Show(GameColor color = GameColor.White) {
        _healthBar.value = Unit.Data.UnitHealth;

        TrackingIconInfo unitIconInfo = Unit.DisplayMgr.IconInfo;
        _unitCompositionIcon.atlas = unitIconInfo.AtlasID.GetAtlas();
        _unitCompositionIcon.spriteName = unitIconInfo.Filename;
        _unitCompositionIcon.color = unitIconInfo.Color.ToUnityColor();

        _unitCompositionLabel.text = UnitCompositionFormat.Inject(Unit.Elements.Count, MaxElementsPerUnit);
        Show(AtlasID.MyGui, UnitImageFilename, Unit.UnitName, color);
    }

    private void HandleIsPickedChanged() {
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

    protected void HandleCompositionChanged() {
        _unitCompositionLabel.text = UnitCompositionFormat.Inject(Unit.Elements.Count, MaxElementsPerUnit);
    }

    private void AssignValuesToMembers() { }

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
        _unit = null;
        _isPicked = false;
        IsInitialized = false;
    }

    #region Cleanup

    private void Unsubscribe() {
        if (_subscriptions != null) {    // can be null if destroyed before Unit is assigned
            _subscriptions.ForAll(d => d.Dispose());
            _subscriptions.Clear();
        }
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    #endregion

    protected override void __Validate() {
        base.__Validate();
        UnityUtility.ValidateComponentPresence<BoxCollider>(gameObject);
    }

}

