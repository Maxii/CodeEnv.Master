﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AElementGuiIcon.cs
// Abstract base AMultiSizeIconGuiElement that holds an AUnitElement.
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
/// Abstract AMultiSizeIconGuiElement that represents an AUnitElement.
/// <remarks>This is an icon used by the Gui, not the in game icon that tracks a UnitElement in space.</remarks>
/// </summary>
public abstract class AUnitElementIconGuiElement : AMultiSizeIconGuiElement {

    private const string DebugNameFormat = "{0}[{1}]";
    private const string TooltipFormat = "{0}";

    public override GuiElementID ElementID { get { return GuiElementID.ElementIcon; } }

    public override string DebugName {
        get {
            string elementName = Element != null ? Element.Name : "No Element";
            return DebugNameFormat.Inject(GetType().Name, elementName);
        }
    }

    public override bool IsInitialized { get { return Size != default(IconSize) && _isElementPropSet; } }

    private bool _isElementPropSet; // reqd as Element can return null if destroyed
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

    protected override string TooltipContent { get { return TooltipFormat.Inject(Element.Name); } }

    private UILabel _iconImageNameLabel;
    private UIProgressBar _healthBar;
    private IList<IDisposable> _subscriptions;

    protected override UISprite AcquireIconImageSprite() {
        return _topLevelIconWidget.gameObject.GetComponentsInImmediateChildren<UISprite>().Single(s => s.GetComponent<UIProgressBar>() == null);
    }

    protected override void AcquireAdditionalWidgets() {
        _healthBar = _topLevelIconWidget.GetComponentInChildren<UIProgressBar>();
        _iconImageNameLabel = _topLevelIconWidget.gameObject.GetSingleComponentInChildren<UILabel>();
    }

    #region Event and Property Change Handlers

    private void ElementPropSetHandler() {
        _isElementPropSet = true;
        if (IsInitialized) {
            PopulateMemberWidgetValues();
            Subscribe();
            Show();
        }
    }

    private void IsPickedPropChangedHandler() {
        HandleIsPickedChanged();
    }

    private void ElementHealthPropChangedHandler() {
        PopulateHealthBarValues();
    }

    #endregion

    protected override void HandleGuiElementHovered(bool isOver) {
        Element.ShowHoveredHud(isOver);
    }

    private void Subscribe() {
        _subscriptions = _subscriptions ?? new List<IDisposable>();
        _subscriptions.Add(Element.Data.SubscribeToPropertyChanged<AUnitElementData, float>(eData => eData.Health, ElementHealthPropChangedHandler));
    }

    protected override void PopulateMemberWidgetValues() {
        base.PopulateMemberWidgetValues();
        var design = Element.Data.Design;
        _iconImageSprite.atlas = design.ImageAtlasID.GetAtlas();
        _iconImageSprite.spriteName = design.ImageFilename;
        _iconImageNameLabel.text = Element.name;
        PopulateHealthBarValues();
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
        if (Element.Data.InfoAccessCntlr.HasIntelCoverageReqdToAccess(GameManager.Instance.UserPlayer, ItemInfoID.Health)) {
            health = Element.Data.Health;
            return true;
        }
        health = null;
        return false;
    }

    public override void ResetForReuse() {
        base.ResetForReuse();
        Unsubscribe();
        _element = null;
        _isElementPropSet = false;
        ////_design = null;
        _isPicked = false;
        if (_healthBar != null) {
            _healthBar.value = Constants.ZeroPercent;
            _healthBar.foregroundWidget.color = GameColor.Green.ToUnityColor();
            _healthBar = null;
        }
        _iconImageNameLabel = null;
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

