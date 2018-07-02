// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectFacilityDesignDialogForm.cs
// ASelectDesignDialogForm that supports selecting a FacilityDesign for a refit or as the initial CentralHub of a new Base .
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
/// ASelectDesignDialogForm that supports selecting a FacilityDesign for a refit or as the initial CentralHub of a new Base .
/// </summary>
public class SelectFacilityDesignDialogForm : ASelectDesignDialogForm {

    public override FormID FormID { get { return FormID.SelectFacilityDesignDialog; } }

    protected override IEnumerable<AUnitMemberDesign> GetDesignChoices() {
        var playerDesigns = GameManager.Instance.GetAIManagerFor(Settings.Player).Designs;
        IEnumerable<FacilityDesign> designChoices;

        bool isRefitSelection = Settings.OptionalParameter != null;
        if (isRefitSelection) {
            FacilityDesign existingDesign = Settings.OptionalParameter as FacilityDesign;
            D.AssertNotNull(existingDesign);
            bool areDesignsFound = playerDesigns.TryGetUpgradeDesigns(existingDesign, out designChoices);
            D.Assert(areDesignsFound);  // existingDesign not included in choices
        }
        else {
            // the Design being selected is for initial deployment of a Base so its a CentralHub
            bool areDesignsFound = playerDesigns.TryGetDeployableDesigns(FacilityHullCategory.CentralHub, out designChoices);
            D.Assert(areDesignsFound);
        }
        return designChoices.Cast<AUnitMemberDesign>();
    }

}

