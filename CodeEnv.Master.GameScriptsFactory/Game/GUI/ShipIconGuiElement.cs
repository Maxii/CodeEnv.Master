// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipIcon.cs
// AMultiSizeIconGuiElement that holds a ShipItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.GameContent;

/// <summary>
/// AMultiSizeIconGuiElement that represents a ShipItem.
/// <remarks>This is an icon used by the Gui, not the in game icon that tracks a element in space.</remarks>
/// </summary>
public class ShipIconGuiElement : AUnitElementIconGuiElement {

    public new ShipItem Element {
        get { return base.Element as ShipItem; }
        set { base.Element = value; }
    }

    protected override void Cleanup() { }

}

