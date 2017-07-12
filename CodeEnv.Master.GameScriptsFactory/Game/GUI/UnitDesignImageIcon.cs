// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitDesignImageIcon.cs
// ImageIcon that holds an AUnitDesign.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// ImageIcon that holds an AUnitDesign.
/// </summary>
public class UnitDesignImageIcon : AImageIcon {

    private const string DebugNameFormat = "{0}[{1}]";
    private const string TooltipFormat = "{0}{1}";

    public override string DebugName { get { return DebugNameFormat.Inject(GetType().Name, Design.DesignName); } }

    protected override string TooltipContent {
        get {
            string obsoleteText = _design.Status == AUnitDesign.SourceAndStatus.Player_Obsolete ? "[Obsolete]" : string.Empty;
            return TooltipFormat.Inject(_design.DesignName, obsoleteText);
        }
    }

    private AUnitDesign _design;
    public AUnitDesign Design {
        get { return _design; }
        set {
            D.AssertNull(_design);
            SetProperty<AUnitDesign>(ref _design, value, "Design", DesignPropSetHandler);
        }
    }

    private bool _isSelected;
    public bool IsSelected {
        get { return _isSelected; }
        set {
            if (_isSelected != value) {
                _isSelected = value;
                IsSelectedPropertyChangedHandler();
            }
        }
    }

    #region Event and Property Change Handlers

    private void DesignPropSetHandler() {
        AssignValuesToMembers();
    }

    private void IsSelectedPropertyChangedHandler() {
        HandleIsSelectedChanged();
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

    private void HandleIsSelectedChanged() {
        if (!IsShowing) {
            D.Warn("{0} changed selection status when not showing?", DebugName);
        }
        if (IsSelected) {
            D.Log("{0} has been selected.", DebugName);
            Show(TempGameValues.SelectedColor);
            SFXManager.Instance.PlaySFX(SfxClipID.Select);
        }
        else {
            D.Log("{0} has been unselected.", DebugName);
            Show();
            SFXManager.Instance.PlaySFX(SfxClipID.UnSelect);
        }
    }

    private void AssignValuesToMembers() {
        Size = IconSize.Large;
    }

    protected override void HandleIconSizeSet() {
        base.HandleIconSizeSet();
        ResizeWidgetAndAnchorIcon();
        Show();
    }

    private void ResizeWidgetAndAnchorIcon() {
        IntVector2 iconSize = GetIconDimensions(Size);
        UIWidget widget = GetComponent<UIWidget>();
        widget.SetDimensions(iconSize.x, iconSize.y);
        AnchorTo(widget);
    }

    public void Reset() {
        _design = null;
        _isSelected = false;
    }

    protected override void Cleanup() { }
}

