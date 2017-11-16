// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityIconGuiElement.cs
// AMultiSizeIconGuiElement that represents a FacilityItem.
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
/// AMultiSizeIconGuiElement that represents a FacilityItem.
/// <remarks>This is an icon used by the Gui, not the in game icon that tracks a element in space.</remarks>
/// </summary>
public class FacilityIconGuiElement : AUnitElementIconGuiElement {

    public new FacilityItem Element {
        get { return base.Element as FacilityItem; }
        set { base.Element = value; }
    }

}

