// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectSettlementCmdModDesignDialogForm.cs
// ASelectDesignDialogForm that supports selecting a SettlementCmdModuleDesign for a new or refitting settlement.
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
/// ASelectDesignDialogForm that supports selecting a SettlementCmdModuleDesign for a new or refitting settlement.
/// </summary>
public class SelectSettlementCmdModDesignDialogForm : ASelectDesignDialogForm {

    public override FormID FormID { get { return FormID.SelectSettlementCmdModDesignDialog; } }

    protected override IEnumerable<AUnitMemberDesign> GetDesignChoices() {
        var playerDesigns = GameManager.Instance.GetAIManagerFor(Settings.Player).Designs;
        IEnumerable<SettlementCmdModuleDesign> designChoices = null;

        bool isRefitSelection = Settings.OptionalParameter != null;
        if (isRefitSelection) {
            SettlementCmdModuleDesign existingDesign = Settings.OptionalParameter as SettlementCmdModuleDesign;
            D.AssertNotNull(existingDesign);
            bool areDesignsFound = playerDesigns.TryGetUpgradeDesigns(existingDesign, out designChoices);
            D.Assert(areDesignsFound);  // existingDesign not included in choices

            // this is a refit so no reason to include the default choice
        }
        else {
            // the Design being selected is for initial deployment of a Unit
            designChoices = playerDesigns.GetAllDeployableSettlementCmdModDesigns(includeDefault: true);
        }
        return designChoices.Cast<AUnitMemberDesign>();
    }

}

