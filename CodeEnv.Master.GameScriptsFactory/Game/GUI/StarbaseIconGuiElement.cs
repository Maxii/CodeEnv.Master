// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseIconGuiElement.cs
// AMultiSizeIconGuiElement that represents a StarbaseCmdItem.
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
/// AMultiSizeIconGuiElement that represents a StarbaseCmdItem.
/// <remarks>This is an icon used by the Gui, not the in game icon that tracks a unit in space.</remarks>
/// </summary>
[System.Obsolete("Not currently used")]
public class StarbaseIconGuiElement : AUnitCmdIconGuiElement {

    public new StarbaseCmdItem Unit {
        get { return base.Unit as StarbaseCmdItem; }
        set { base.Unit = value; }
    }

    protected override int MaxElementsPerUnit { get { return TempGameValues.MaxFacilitiesPerBase; } }

    protected override string UnitImageFilename { get { return TempGameValues.StarbaseImageFilename; } }

    protected override void Subscribe() {
        base.Subscribe();
        _subscriptions.Add(Unit.Data.SubscribeToPropertyChanged<StarbaseCmdData, BaseComposition>(data => data.UnitComposition, UnitCompositionPropChangedHandler));
    }

    #region Event and Property Change Handlers

    private void UnitCompositionPropChangedHandler() {
        HandleCompositionChanged();
    }

    #endregion


}

