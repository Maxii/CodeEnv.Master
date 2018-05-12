// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitCmdIconGuiElement.cs
// Abstract AMultiSizeIconGuiElement that represents an AUnitCmdItem.
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

/// <summary>
/// Abstract AMultiSizeIconGuiElement that represents an AUnitCmdItem.
/// <remarks>This is an icon used by the Gui, not the in game icon that tracks a unit in space.</remarks>
/// </summary>
public abstract class AUnitCmdIconGuiElement : AMultiSizeIconGuiElement {

    private const string DebugNameFormat = "{0}[{1}]";
    private const string TooltipFormat = "{0}";
    private const string UnitCompositionFormat = "{0}/{1}";

    public override GuiElementID ElementID { get { return GuiElementID.UnitIcon; } }

    public override string DebugName {
        get {
            string unitName = Unit != null ? Unit.Name : "Unit either destroyed or not yet assigned";
            return DebugNameFormat.Inject(GetType().Name, unitName);
        }
    }

    public override bool IsInitialized { get { return Size != default(IconSize) && _isUnitPropSet; } }

    private bool _isUnitPropSet; // reqd as Unit can return null if destroyed
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

    protected sealed override string TooltipContent { get { return TooltipFormat.Inject(Unit.UnitName); } }

    protected IList<IDisposable> _subscriptions;
    private UISprite _unitCompositionIcon;
    private UILabel _unitCompositionLabel;
    private UIProgressBar _healthBar;
    private UILabel _iconImageNameLabel;

    protected sealed override UISprite AcquireIconImageSprite() {
        return _topLevelIconWidget.gameObject.GetComponentsInImmediateChildren<UISprite>().Single(s => s.GetComponent<UIProgressBar>() == null
            && s.GetComponentInChildren<GuiElement>() == null);
    }

    protected override void AcquireAdditionalWidgets() {
        _healthBar = _topLevelIconWidget.GetComponentInChildren<UIProgressBar>();
        var compositionContainerGo = _topLevelIconWidget.GetComponentsInChildren<GuiElement>().Single(ge => ge.ElementID == GuiElementID.Composition).gameObject;
        _unitCompositionIcon = compositionContainerGo.GetSingleComponentInChildren<UISprite>();
        _unitCompositionLabel = compositionContainerGo.GetSingleComponentInChildren<UILabel>();
        _iconImageNameLabel = _topLevelIconWidget.GetComponentsInChildren<GuiElement>().Single(ge => ge.ElementID == GuiElementID.Name).GetComponent<UILabel>();
    }

    protected virtual void Subscribe() {
        _subscriptions = _subscriptions ?? new List<IDisposable>();
        _subscriptions.Add(Unit.Data.SubscribeToPropertyChanged<AUnitCmdData, float>(data => data.UnitHealth, UnitHealthPropChangedHandler));
        _subscriptions.Add(Unit.DisplayMgr.SubscribeToPropertyChanged<UnitCmdDisplayManager, TrackingIconInfo>(unit => unit.IconInfo, UnitIconInfoPropChangedHandler));
        _subscriptions.Add(Unit.Data.SubscribeToPropertyChanged<AUnitCmdData, string>(data => data.UnitName, UnitNamePropChangedHandler));
    }

    protected override void PopulateMemberWidgetValues() {
        base.PopulateMemberWidgetValues();
        TrackingIconInfo unitIconInfo = Unit.DisplayMgr.IconInfo;
        _unitCompositionIcon.atlas = unitIconInfo.AtlasID.GetAtlas();
        _unitCompositionIcon.spriteName = unitIconInfo.Filename;
        _unitCompositionIcon.color = unitIconInfo.Color.ToUnityColor();

        _unitCompositionLabel.text = UnitCompositionFormat.Inject(Unit.ElementCount, MaxElementsPerUnit);
        _iconImageSprite.atlas = AtlasID.MyGui.GetAtlas();
        _iconImageSprite.spriteName = UnitImageFilename;
        _iconImageNameLabel.text = Unit.UnitName;

        PopulateHealthBarValues();
    }

    #region Event and Property Change Handlers

    private void UnitPropSetHandler() {
        D.AssertNotDefault((int)Size);
        _isUnitPropSet = true;
        if (IsInitialized) {
            PopulateMemberWidgetValues();
            Subscribe();
            Show();
        }
    }

    private void IsPickedPropChangedHandler() {
        HandleIsPickedChanged();
    }

    private void UnitHealthPropChangedHandler() {
        PopulateHealthBarValues();
    }

    private void UnitIconInfoPropChangedHandler() {
        _unitCompositionIcon.atlas = Unit.DisplayMgr.IconInfo.AtlasID.GetAtlas();
        _unitCompositionIcon.spriteName = Unit.DisplayMgr.IconInfo.Filename;
    }

    private void UnitNamePropChangedHandler() {
        if (IsPicked) {
            Show(TempGameValues.SelectedColor);
        }
        else {
            Show();
        }
    }

    #endregion

    protected override void HandleGuiElementHovered(bool isOver) {
        Unit.ShowHoveredHud(isOver);
    }

    private void HandleIsPickedChanged() {
        D.Assert(IsEnabled);
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
        _unitCompositionLabel.text = UnitCompositionFormat.Inject(Unit.ElementCount, MaxElementsPerUnit);
    }

    private void PopulateHealthBarValues() {
        float? health;
        if (TryGetHealth(out health)) {
            D.Assert(health.HasValue);
            _healthBar.value = health.Value;
        }
        else {
            _healthBar.value = Constants.OneHundredPercent;
            _healthBar.foregroundWidget.color = TempGameValues.UnknownHealthColor.ToUnityColor();
        }
    }

    private bool TryGetHealth(out float? health) {
        if (Unit.Data.InfoAccessCntlr.HasIntelCoverageReqdToAccess(GameManager.Instance.UserPlayer, ItemInfoID.UnitHealth)) {
            health = Unit.Data.UnitHealth;
            return true;
        }
        health = null;
        return false;
    }

    public override void ResetForReuse() {
        base.ResetForReuse();
        Unsubscribe();
        _unit = null;
        _isUnitPropSet = false;
        _isPicked = false;
        if (_healthBar != null) {
            _healthBar.value = Constants.ZeroPercent;
            _healthBar.foregroundWidget.color = GameColor.Green.ToUnityColor();
            _healthBar = null;
        }
        _iconImageNameLabel = null;
        _unitCompositionIcon = null;
        _unitCompositionLabel = null;
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


}

