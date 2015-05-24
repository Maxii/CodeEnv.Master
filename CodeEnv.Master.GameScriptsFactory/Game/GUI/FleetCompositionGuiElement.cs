﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCompositionGuiElement.cs
// GuiElement handling the display and tooltip content for the Composition of a Fleet.   
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// GuiElement handling the display and tooltip content for the Composition of a Fleet.   
/// </summary>
public class FleetCompositionGuiElement : ACompositionGuiElement {

    private FleetCategory _category;
    public FleetCategory Category {
        get { return _category; }
        set { SetProperty<FleetCategory>(ref _category, value, "Category", OnCategoryChanged); }
        // Note: Once a cmd is detected, an estimation of its category is always available based on the elements that have been detected
    }

    protected override string TooltipContent { get { return base.TooltipContent; } }    // TODO

    protected override bool AreAllValuesSet { get { return IconInfo != default(IconInfo) && Category != default(FleetCategory); } }

    private void OnCategoryChanged() {
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    protected override string GetCategoryName() { return Category.GetName(); }

    public override void Reset() {
        base.Reset();
        _category = default(FleetCategory);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IComparable<ACompositionGuiElement> Members

    public override int CompareTo(ACompositionGuiElement other) {
        return Category.CompareTo((other as FleetCompositionGuiElement).Category);
    }

    #endregion

}

