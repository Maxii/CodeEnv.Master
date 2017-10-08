// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCompositionGuiElement.cs
// AGuiElement that represent the composition of a Starbase.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// AGuiElement that represent the composition of a Starbase.
/// </summary>
public class StarbaseCompositionGuiElement : AUnitCompositionGuiElement {

    private bool _isCategorySet = false;  // reqd as value can be None if UnitCategory not accessible
    private StarbaseCategory _category;
    public StarbaseCategory Category {
        get { return _category; }
        set {
            D.Assert(!_isCategorySet);  // only happens once between Resets
            _category = value;
            CategoryPropSetHandler();
        }
    }

    protected override string TooltipContent { get { return "Composition of the Starbase"; } }    // IMPROVE

    public override bool IsInitialized { get { return IconInfo != default(TrackingIconInfo) && _isCategorySet; } }

    #region Event and Property Change Handlers

    private void CategoryPropSetHandler() {
        _isCategorySet = true;
        if (IsInitialized) {
            PopulateMemberWidgetValues();
        }
    }

    #endregion

    protected override string GetUnitCategoryName() { return Category != StarbaseCategory.None ? Category.GetValueName() : Unknown; }

    public override void ResetForReuse() {
        base.ResetForReuse();
        _isCategorySet = false;
    }

    protected override void Cleanup() { }

    #region IComparable<StarbaseCompositionGuiElement> Members

    public override int CompareTo(AUnitCompositionGuiElement other) {
        return Category.CompareTo((other as StarbaseCompositionGuiElement).Category);
    }

    #endregion

}

