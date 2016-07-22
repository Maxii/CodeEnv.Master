// --------------------------------------------------------------------------------------------------------------------
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

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// GuiElement handling the display and tooltip content for the Composition of a Fleet.   
/// </summary>
public class FleetCompositionGuiElement : ACompositionGuiElement {

    private bool _isCategorySet = false;
    private FleetCategory _category;
    public FleetCategory Category {
        get { return _category; }
        set {
            D.Assert(!_isCategorySet);  // only happens once between Resets
            _category = value;  // value can be None if no element category is accessible
            CategoryPropSetHandler();  // SetProperty() only calls handler when changed
        }
    }

    protected override string TooltipContent { get { return base.TooltipContent; } }    //TODO

    protected override bool AreAllValuesSet { get { return IconInfo != default(IconInfo) && _isCategorySet; } }

    #region Event and Property Change Handlers

    private void CategoryPropSetHandler() {
        _isCategorySet = true;
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    #endregion

    protected override string GetTextForCategory() { return Category != FleetCategory.None ? Category.GetValueName() : _unknown; }

    public override void Reset() {
        base.Reset();
        _isCategorySet = false;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IComparable<ACompositionGuiElement> Members

    public override int CompareTo(ACompositionGuiElement other) {
        return Category.CompareTo((other as FleetCompositionGuiElement).Category);
    }

    #endregion

}

