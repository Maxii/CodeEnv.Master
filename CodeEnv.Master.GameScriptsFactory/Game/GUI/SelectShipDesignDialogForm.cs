// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectShipDesignDialogForm.cs
// ASelectDesignDialogForm that supports selecting a ShipDesign for a refit.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ASelectDesignDialogForm that supports selecting a ShipDesign for a refit.
/// </summary>
public class SelectShipDesignDialogForm : ASelectDesignDialogForm {

    public override FormID FormID { get { return FormID.SelectShipDesignDialog; } }

    protected override IEnumerable<AUnitMemberDesign> GetDesignChoices() {
        var playerDesigns = GameManager.Instance.GetAIManagerFor(Settings.Player).Designs;
        IEnumerable<ShipDesign> designChoices = null;

        bool isRefitSelection = Settings.OptionalParameter != null;
        D.Assert(isRefitSelection); // new Fleets are only created from existing ships
        ShipDesign existingDesign = Settings.OptionalParameter as ShipDesign;
        D.AssertNotNull(existingDesign);
        bool areDesignsFound = playerDesigns.TryGetUpgradeDesigns(existingDesign, out designChoices);
        D.Assert(areDesignsFound);  // existingDesign not included in choices
        return designChoices.Cast<AUnitMemberDesign>();
    }

}

