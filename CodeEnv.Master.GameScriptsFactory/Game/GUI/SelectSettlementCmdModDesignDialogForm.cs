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

using CodeEnv.Master.GameContent;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ASelectDesignDialogForm that supports selecting a SettlementCmdModuleDesign for a new or refitting settlement.
/// </summary>
public class SelectSettlementCmdModDesignDialogForm : ASelectDesignDialogForm {

    public override FormID FormID { get { return FormID.SelectSettlementCmdModDesignDialog; } }

    protected override DesignIconGuiElement ChooseInitialIconToBePicked(IEnumerable<DesignIconGuiElement> allDisplayedIcons) {
        ////AUnitMemberDesign defaultCmdDesign = GameManager.Instance.GetAIManagerFor(Settings.Player).Designs.GetDefaultSettlementCmdModDesign();
        ////return allDisplayedIcons.Single(icon => icon.Design == defaultCmdDesign);
        return allDisplayedIcons.Single(icon => icon.Design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Default);
    }

    protected override IEnumerable<AUnitMemberDesign> GetDesigns(bool includeObsolete) {
        var playerDesigns = GameManager.Instance.GetAIManagerFor(Settings.Player).Designs;

        var cmdDesigns = playerDesigns.GetAllDeployableSettlementCmdModDesigns(includeObsolete, includeDefault: true).Cast<AUnitMemberDesign>();
        ////var cmdDefaultDesign = playerDesigns.GetDefaultSettlementCmdModDesign();
        ////if (!cmdDesigns.Contains(cmdDefaultDesign)) {
        ////    cmdDesigns.Add(cmdDefaultDesign);
        ////}
        return cmdDesigns;
    }

}

