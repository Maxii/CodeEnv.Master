// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCompositionGuiElement.cs
// GuiElement handling the display and tooltip content for the Composition of a Settlement.    
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// GuiElement handling the display and tooltip content for the Composition of a Settlement.     
/// </summary>
public class SettlementCompositionGuiElement : ACompositionGuiElement {

    private SettlementCategory _category;
    public SettlementCategory Category {
        get { return _category; }
        set { SetProperty<SettlementCategory>(ref _category, value, "Category", OnCategoryChanged); }
        // Note: Once a cmd is detected, an estimation of its category is always available based on the elements that have been detected
    }

    protected override string TooltipContent { get { return base.TooltipContent; } }    // TODO

    protected override bool AreAllValuesSet { get { return IconInfo != default(IconInfo) && Category != default(SettlementCategory); } }

    private void OnCategoryChanged() {
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    protected override string GetCategoryName() { return Category.GetValueName(); }

    public override void Reset() {
        base.Reset();
        _category = default(SettlementCategory);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IComparable<ACompostionGuiElement> Members

    public override int CompareTo(ACompositionGuiElement other) {
        return Category.CompareTo((other as SettlementCompositionGuiElement).Category);
    }

    #endregion

}

