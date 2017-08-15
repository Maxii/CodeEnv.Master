// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipIcon.cs
// GuiIcon that holds a ShipItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.GameContent;

/// <summary>
/// GuiIcon that holds a ShipItem.
/// </summary>
public class ShipIcon : AElementIcon {

    public new ShipItem Element {
        get { return base.Element as ShipItem; }
        set { base.Element = value; }
    }

    protected override AElementDesign InitializeDesign() {
        return GameManager.Instance.PlayersDesigns.GetUserShipDesign(Element.Data.DesignName);
    }

    protected override void Cleanup() { }

}

