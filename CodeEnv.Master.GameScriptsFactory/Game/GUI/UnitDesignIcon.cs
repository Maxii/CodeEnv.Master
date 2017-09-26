// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitDesignIcon.cs
// AMultiSizeGuiIcon that holds a AUnitDesign for Unit Cmd and Element Designs.
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
/// AMultiSizeGuiIcon that holds a AUnitDesign for Unit Cmd and Element Designs.
/// </summary>
public class UnitDesignIcon : AMultiSizeGuiIcon {

    private const string DebugNameFormat = "{0}[{1}]";
    private const string TooltipFormat = "{0}{1}";

    public override string DebugName {
        get {
            if (Design == null) {
                return DebugNameFormat.Inject(GetType().Name, "No Design");
            }
            return DebugNameFormat.Inject(GetType().Name, Design.DesignName);
        }
    }

    protected override string TooltipContent {
        get {
            string obsoleteText = _design.Status == AUnitDesign.SourceAndStatus.Player_Obsolete ? "[Obsolete]" : string.Empty;
            return TooltipFormat.Inject(_design.DesignName, obsoleteText);
        }
    }

    /// <summary>
    /// Indicates whether this Icon has been initialized, aka its Design property has been set.
    /// </summary>
    public bool IsInitialized { get; private set; }

    private AUnitDesign _design;
    public AUnitDesign Design {
        get { return _design; }
        set {
            D.AssertNull(_design);
            D.AssertNotNull(value);
            SetProperty<AUnitDesign>(ref _design, value, "Design", DesignPropSetHandler);
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

    protected override void AcquireAdditionalIconWidgets(GameObject topLevelIconGo) { }

    #region Event and Property Change Handlers

    private void DesignPropSetHandler() {
        D.AssertNotDefault((int)Size);
        D.Assert(!IsInitialized);
        IsInitialized = true;
        Show();
    }

    private void IsPickedPropChangedHandler() {
        D.Assert(IsShowing);
        HandleIsPickedChanged();
    }

    void OnHover(bool isOver) {
        if (isOver) {
            HoveredHudWindow.Instance.Show(FormID.UnitDesign, Design);
        }
        else {
            HoveredHudWindow.Instance.Hide();
        }
    }

    #endregion

    private void Show(GameColor color = GameColor.White) {
        Show(Design.ImageAtlasID, Design.ImageFilename, Design.DesignName, color);
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

    protected override void HandleIconSizeSet() {
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
        _design = null;
        _isPicked = false;
        IsInitialized = false;
    }

    protected override void Cleanup() { }
}

