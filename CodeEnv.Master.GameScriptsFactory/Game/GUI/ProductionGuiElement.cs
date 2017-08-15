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
/// GuiElement handling the display and tooltip content for an Item's Production.
/// </summary>
public class ProductionGuiElement : AGuiElement, IComparable<ProductionGuiElement> {

    public override GuiElementID ElementID { get { return GuiElementID.Production; } }

    private bool __isProductionSet;
    private string __producingName;
    public string __ProducingName {
        get { return __producingName; }
        set {
            D.Assert(!__isProductionSet);   // occurs only once between Resets
            __producingName = value;
            __ProducingNamePropSetHandler();  // SetProperty() only calls handler when changed
        }
    }

    private bool AreAllValuesSet { get { return __isProductionSet; } }

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
        _imageNameLabel = gameObject.GetSingleComponentInImmediateChildren<UILabel>();
        var sprites = gameObject.GetSafeComponentsInImmediateChildren<UISprite>();
        var imageFrameSprite = sprites.Single(s => s.spriteName == TempGameValues.ImageFrameSpriteName);
        _imageSprite = imageFrameSprite.gameObject.GetSingleComponentInChildren<UISprite>(excludeSelf: true);
        // UNDONE find constructionTime and buyoutCost labels
    }

    #region Event and Property Change Handlers

    private void __ProducingNamePropSetHandler() {
        __isProductionSet = true;
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    #endregion

    private void PopulateElementWidgets() {
        if (__ProducingName.IsNullOrEmpty()) {
            HandleValuesUnknown();
            return;
        }

        _imageSprite.atlas = AtlasID.MyGui.GetAtlas();
        _imageSprite.spriteName = __ProducingName;   // should result in a null (no image) sprite
        _imageNameLabel.text = __ProducingName;
        // UNDONE
    }

    private void HandleValuesUnknown() {
        _imageSprite.atlas = AtlasID.MyGui.GetAtlas();
        _imageSprite.spriteName = TempGameValues.UnknownImageFilename;
        _imageNameLabel.text = Constants.QuestionMark;
        _tooltipContent = "Unknown";
    }

    public override void ResetForReuse() {
        // UNDONE
        __isProductionSet = false;
    }

    protected override void Cleanup() { }


    #region IComparable<ProductionGuiElement> Members

    public int CompareTo(ProductionGuiElement other) {
        int result = __ProducingName.CompareTo(other.__ProducingName);
        //TODO
        return result;
    }

    #endregion

}

