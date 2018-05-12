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

using CodeEnv.Master.GameContent;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ASelectDesignDialogForm that supports selecting a FleetCmdModuleDesign for a new or refitting fleet.
/// </summary>
public class SelectFleetCmdModDesignDialogForm : ASelectDesignDialogForm {

    public override FormID FormID { get { return FormID.SelectFleetCmdModDesignDialog; } }

    protected override DesignIconGuiElement ChooseInitialIconToBePicked(IEnumerable<DesignIconGuiElement> allDisplayedIcons) {
        ////AUnitMemberDesign defaultCmdDesign = GameManager.Instance.GetAIManagerFor(Settings.Player).Designs.GetDefaultFleetCmdModDesign();
        ////return allDisplayedIcons.Single(icon => icon.Design == defaultCmdDesign);
        return allDisplayedIcons.Single(icon => icon.Design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Default);
    }

    protected override IEnumerable<AUnitMemberDesign> GetDesigns(bool includeObsolete) {
        var playerDesigns = GameManager.Instance.GetAIManagerFor(Settings.Player).Designs;

        var cmdDesigns = playerDesigns.GetAllDeployableFleetCmdModDesigns(includeObsolete, includeDefault: true).Cast<AUnitMemberDesign>();
        ////var cmdDefaultDesign = playerDesigns.GetDefaultFleetCmdModDesign();
        ////if (!cmdDesigns.Contains(cmdDefaultDesign)) {
        ////    cmdDesigns.Add(cmdDefaultDesign);
        ////}
        return cmdDesigns;
    }

}

