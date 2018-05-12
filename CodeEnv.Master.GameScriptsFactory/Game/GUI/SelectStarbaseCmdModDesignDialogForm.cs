// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectStarbaseCmdModDesignDialogForm.cs
// ASelectDesignDialogForm that supports selecting a StarbaseCmdModuleDesign for a new or refitting starbase.
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
/// ASelectDesignDialogForm that supports selecting a StarbaseCmdModuleDesign for a new or refitting starbase.
/// </summary>
public class SelectStarbaseCmdModDesignDialogForm : ASelectDesignDialogForm {

    public override FormID FormID { get { return FormID.SelectStarbaseCmdModDesignDialog; } }

    protected override DesignIconGuiElement ChooseInitialIconToBePicked(IEnumerable<DesignIconGuiElement> allDisplayedIcons) {
        ////AUnitMemberDesign defaultCmdDesign = GameManager.Instance.GetAIManagerFor(Settings.Player).Designs.GetDefaultStarbaseCmdModDesign();
        ////return allDisplayedIcons.Single(icon => icon.Design == defaultCmdDesign);
        return allDisplayedIcons.Single(icon => icon.Design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Default);
    }

    protected override IEnumerable<AUnitMemberDesign> GetDesigns(bool includeObsolete) {
        var playerDesigns = GameManager.Instance.GetAIManagerFor(Settings.Player).Designs;

        var cmdDesigns = playerDesigns.GetAllDeployableStarbaseCmdModDesigns(includeObsolete, includeDefault: true).Cast<AUnitMemberDesign>();
        ////var cmdDefaultDesign = playerDesigns.GetDefaultStarbaseCmdModDesign();
        ////if (!cmdDesigns.Contains(cmdDefaultDesign)) {
        ////    cmdDesigns.Add(cmdDefaultDesign);
        ////}
        return cmdDesigns;
    }

}

