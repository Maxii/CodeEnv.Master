// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DesignIconGuiElement.cs
// AMultiSizeIconGuiElement that represents Unit Cmd and Element Designs.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// AMultiSizeIconGuiElement that represents Unit Cmd and Element Designs.
/// </summary>
public class DesignIconGuiElement : AMultiSizeIconGuiElement {

    private const string DebugNameFormat = "{0}[{1}]";
    private const string TooltipFormat = "{0}{1}";
    private const string StatusTooltipFormat = "[{0}]";

    public override GuiElementID ElementID { get { return GuiElementID.DesignIcon; } }

    public override string DebugName {
        get {
            string designName = Design != null ? Design.DesignName : "No Design";
            return DebugNameFormat.Inject(GetType().Name, designName);
        }
    }

    protected override string TooltipContent {
        get {
            string statusText = _design.Status == AUnitMemberDesign.SourceAndStatus.PlayerCreation_Current ? string.Empty
                : StatusTooltipFormat.Inject(_design.Status.GetEnumAttributeText());
            return TooltipFormat.Inject(_design.DesignName, statusText);
        }
    }

    public override bool IsInitialized { get { return Size != default(IconSize) && Design != null; } }

    private AUnitMemberDesign _design;
    public AUnitMemberDesign Design {
        get { return _design; }
        set {
            D.AssertNull(_design);
            D.AssertNotNull(value);
            SetProperty<AUnitMemberDesign>(ref _design, value, "Design", DesignPropSetHandler);
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

    private UILabel _iconImageNameLabel;

    protected override UISprite AcquireIconImageSprite() {
        return _topLevelIconWidget.gameObject.GetSingleComponentInImmediateChildren<UISprite>();
    }

    protected override void AcquireAdditionalWidgets() {
        _iconImageNameLabel = _topLevelIconWidget.gameObject.GetSingleComponentInChildren<UILabel>();
    }

    protected override void PopulateMemberWidgetValues() {
        base.PopulateMemberWidgetValues();
        _iconImageSprite.atlas = Design.ImageAtlasID.GetAtlas();
        _iconImageSprite.spriteName = Design.ImageFilename;
        _iconImageNameLabel.text = Design.DesignName;
    }

    #region Event and Property Change Handlers

    private void DesignPropSetHandler() {
        D.AssertNotDefault((int)Size);
        if (IsInitialized) {
            PopulateMemberWidgetValues();
            Show();
        }
    }

    private void IsPickedPropChangedHandler() {
        HandleIsPickedChanged();
    }

    #endregion

    protected override void HandleGuiElementHovered(bool isOver) {
        if (isOver) {
            HoveredHudWindow.Instance.Show(FormID.Design, Design);
        }
        else {
            HoveredHudWindow.Instance.Hide();
        }
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

    protected override void HandleIsEnabledChanged() {
        base.HandleIsEnabledChanged();
        D.Assert(!IsPicked);    // Should never change while picked
    }

    public override void ResetForReuse() {
        base.ResetForReuse();
        _design = null;
        _isPicked = false;
        _iconImageNameLabel = null;
    }

    protected override void Cleanup() { }

}

