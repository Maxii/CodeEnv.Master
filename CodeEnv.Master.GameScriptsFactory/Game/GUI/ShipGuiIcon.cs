// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipIcon.cs
// AMultiSizeGuiIcon that holds a ShipItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.GameContent;

/// <summary>
/// AMultiSizeGuiIcon that holds a ShipItem.
/// <remarks>This is an icon used by the Gui, not the in game icon that tracks a element in space.</remarks>
/// </summary>
public class ShipGuiIcon : AElementGuiIcon {

    public new ShipItem Element {
        get { return base.Element as ShipItem; }
        set { base.Element = value; }
    }

    protected override AUnitElementDesign InitializeDesign() {
        return GameManager.Instance.PlayersDesigns.GetUserShipDesign(Element.Data.DesignName);
    }

    protected override void Cleanup() { }

}

