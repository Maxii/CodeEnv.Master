// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCompositionGuiElement.cs
// GuiElement handling the display and tooltip content for the Composition of a Starbase.    
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// GuiElement handling the display and tooltip content for the Composition of a Starbase.    
/// </summary>
public class StarbaseCompositionGuiElement : ACompositionGuiElement {

    private bool _isCategorySet = false;
    private StarbaseCategory _category;
    public StarbaseCategory Category {
        get { return _category; }
        set {
            D.Assert(!_isCategorySet);  // only happens once between Resets
            _category = value;  // value can be None if no element category is accessible
            CategoryPropSetHandler();  // SetProperty() only calls handler when changed
        }
    }

    protected override string TooltipContent { get { return base.TooltipContent; } }    //TODO

    protected override bool AreAllValuesSet { get { return IconInfo != default(TrackingIconInfo) && _isCategorySet; } }

    #region Event and Property Change Handlers

    private void CategoryPropSetHandler() {
        _isCategorySet = true;
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    #endregion

    protected override string GetTextForCategory() { return Category != StarbaseCategory.None ? Category.GetValueName() : Unknown; }

    public override void ResetForReuse() {
        base.ResetForReuse();
        _isCategorySet = false;
    }

    protected override void Cleanup() { }

    #region IComparable<ACompostionGuiElement> Members

    public override int CompareTo(ACompositionGuiElement other) {
        return Category.CompareTo((other as StarbaseCompositionGuiElement).Category);
    }

    #endregion

}

