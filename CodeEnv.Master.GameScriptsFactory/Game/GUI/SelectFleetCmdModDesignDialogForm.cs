// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectFleetCmdModDesignDialogForm.cs
// ASelectDesignDialogForm that supports selecting a FleetCmdModuleDesign for a new or refitting fleet.
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
/// ASelectDesignDialogForm that supports selecting a FleetCmdModuleDesign for a new or refitting fleet.
/// </summary>
public class SelectFleetCmdModDesignDialogForm : ASelectDesignDialogForm {

    public override FormID FormID { get { return FormID.SelectFleetCmdModDesignDialog; } }

    protected override IEnumerable<AUnitMemberDesign> GetDesignChoices() {
        var playerDesigns = GameManager.Instance.GetAIManagerFor(Settings.Player).Designs;
        IEnumerable<FleetCmdModuleDesign> designChoices = null;

        bool isRefitSelection = Settings.OptionalParameter != null;
        if (isRefitSelection) {
            FleetCmdModuleDesign existingDesign = Settings.OptionalParameter as FleetCmdModuleDesign;
            D.AssertNotNull(existingDesign);
            bool areDesignsFound = playerDesigns.TryGetUpgradeDesigns(existingDesign, out designChoices);
            D.Assert(areDesignsFound);  // existingDesign not included in choices

            // this is a refit so no reason to include the default choice
        }
        else {
            // the Design being selected is for initial deployment of a Unit
            designChoices = playerDesigns.GetAllDeployableFleetCmdModDesigns(includeDefault: true);
        }
        return designChoices.Cast<AUnitMemberDesign>();
    }

}

