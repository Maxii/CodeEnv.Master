// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ProductionGuiElement.cs
// GuiElement handling the display and tooltip content for an Item's Production.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// GuiElement handling the display and tooltip content for an Item's Production.
/// </summary>
public class ProductionGuiElement : AGuiElement, IComparable<ProductionGuiElement> {

    public override GuiElementID ElementID { get { return GuiElementID.Production; } }

    private bool _isProductionSet = false;
    private string __producingName;
    public string __ProducingName {
        get { return __producingName; }
        set { SetProperty<string>(ref __producingName, value, "__ProducingName", __OnProducingNameChanged); }
    }

    private bool AreAllValuesSet { get { return _isProductionSet; } }

    private string _tooltipContent;
    protected override string TooltipContent { get { return _tooltipContent; } }

    private UILabel _imageNameLabel;
    private UISprite _imageSprite;
    //private UILabel _remainingProductionTimeLabel;
    //private UILabel _buyoutCostLabel;

    protected override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
    }

    private void InitializeValuesAndReferences() {
        _imageNameLabel = gameObject.GetSafeSingleMonoBehaviourInImmediateChildrenOnly<UILabel>();
        var sprites = gameObject.GetSafeMonoBehavioursInImmediateChildrenOnly<UISprite>();
        var imageFrameSprite = sprites.Single(s => s.spriteName == TempGameValues.ImageFrameSpriteName);
        _imageSprite = imageFrameSprite.gameObject.GetSafeFirstMonoBehaviourInChildrenOnly<UISprite>();
        // UNDONE find constructionTime and buyoutCost labels
    }

    private void __OnProducingNameChanged() {
        _isProductionSet = true;
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    private void PopulateElementWidgets() {
        if (__ProducingName.IsNullOrEmpty()) {
            OnValuesUnknown();
            return;
        }

        _imageSprite.atlas = AtlasID.MyGui.GetAtlas();
        _imageSprite.spriteName = __ProducingName;   // should result in a null (no image) sprite
        _imageNameLabel.text = __ProducingName;
        // UNDONE
    }

    private void OnValuesUnknown() {
        _imageSprite.atlas = AtlasID.MyGui.GetAtlas();
        _imageSprite.spriteName = TempGameValues.UnknownImageFilename;
        _imageNameLabel.text = Constants.QuestionMark;
        _tooltipContent = "Unknown";
    }

    public override void Reset() {
        // UNDONE
        __producingName = null;
        _isProductionSet = false;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IComparable<ProductionGuiElement> Members

    public int CompareTo(ProductionGuiElement other) {
        int result = __ProducingName.CompareTo(other.__ProducingName);
        // TODO
        return result;
    }

    #endregion

}

