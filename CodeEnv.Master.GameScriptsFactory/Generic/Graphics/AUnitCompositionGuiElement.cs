// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitCompositionGuiElement.cs
// Abstract AGuiElement that represent the composition of a Unit.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract AGuiElement that represent the composition of a Unit.
/// </summary>
public abstract class AUnitCompositionGuiElement : AGuiElement, IComparable<AUnitCompositionGuiElement> {

    public override GuiElementID ElementID { get { return GuiElementID.Composition; } }

    private TrackingIconInfo _iconInfo;
    public TrackingIconInfo IconInfo {
        get { return _iconInfo; }
        set {
            D.AssertNull(_iconInfo);   // only occurs once between Resets
            SetProperty<TrackingIconInfo>(ref _iconInfo, value, "IconInfo", IconInfoPropSetHandler);
        }
    }

    protected override string TooltipContent { get { return "Composition custom tooltip placeholder"; } }

    private UILabel _unitCategoryNameLabel;
    private UISprite _unitIconSprite;

    protected override void InitializeValuesAndReferences() {
        _unitIconSprite = gameObject.GetSingleComponentInChildren<UISprite>();
        _unitCategoryNameLabel = gameObject.GetSingleComponentInChildren<UILabel>();
    }

    #region Event and Property Change Handlers

    private void IconInfoPropSetHandler() {
        if (IsInitialized) {
            PopulateMemberWidgetValues();
        }
    }

    #endregion

    protected override void PopulateMemberWidgetValues() {
        base.PopulateMemberWidgetValues();
        //D.Log("{0} populating Icon. SpriteName: {1}, Color: {2}.", GetType().Name, IconInfo.Filename, IconInfo.Color.GetValueName());
        _unitIconSprite.atlas = IconInfo.AtlasID.GetAtlas();
        _unitIconSprite.spriteName = IconInfo.Filename;
        _unitIconSprite.color = IconInfo.Color.ToUnityColor();
        //sprite size and placement are preset
        _unitCategoryNameLabel.text = GetUnitCategoryName();
    }

    protected abstract string GetUnitCategoryName();

    public override void ResetForReuse() {
        _iconInfo = default(TrackingIconInfo);
    }

    #region IComparable<AUnitCompositionGuiElement> Members

    public abstract int CompareTo(AUnitCompositionGuiElement other);

    #endregion

}

