// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityIcon.cs
// AMultiSizeGuiIcon that holds a FacilityItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// AMultiSizeGuiIcon that holds a FacilityItem.
/// </summary>
public class FacilityIcon : AElementIcon {

    public new FacilityItem Element {
        get { return base.Element as FacilityItem; }
        set { base.Element = value; }
    }

    protected override AElementDesign InitializeDesign() {
        return GameManager.Instance.PlayersDesigns.GetUserFacilityDesign(Element.Data.DesignName);
    }

    protected override void Cleanup() { }

}

