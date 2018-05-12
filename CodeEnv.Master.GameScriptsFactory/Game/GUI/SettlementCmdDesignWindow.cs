// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCmdDesignWindow.cs
// GuiWindow used by the User to design SettlementCmdModules, managed by the DesignScreensManager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// GuiWindow used by the User to design SettlementCmdModules, managed by the DesignScreensManager.
/// </summary>
public class SettlementCmdDesignWindow : AUnitDesignWindow {

    protected override void AddToPlayerDesigns(AUnitMemberDesign newDesign) {
        UserDesigns.Add(newDesign as SettlementCmdModuleDesign);
    }

    protected override AUnitMemberDesign CopyDesignFrom(AUnitMemberDesign design) {
        return new SettlementCmdModuleDesign(design as SettlementCmdModuleDesign);
    }

    protected override bool TryGet3DModelFor(AUnitMemberDesign design, out Vector3 modelDimensions, out GameObject modelPrefab) {
        modelDimensions = default(Vector3);
        modelPrefab = null;
        return false;
    }

    protected override IEnumerable<AUnitMemberDesign> GetRegisteredUserDesigns(bool includeObsolete) {
        return UserDesigns.GetAllDeployableSettlementCmdModDesigns(includeObsolete, includeDefault: false).Cast<AUnitMemberDesign>();
    }

    protected override bool TryGetCreateDesignPopupContent(out string popupTitle, out List<string> popupContent) {
        popupTitle = null;
        popupContent = null;
        return false;
    }

    protected override AUnitMemberDesign GetEmptyTemplateDesign(string emptyTemplateHint) {
        D.AssertNull(emptyTemplateHint);
        return UserDesigns.GetCurrentSettlementCmdModTemplateDesign();
    }

    protected override void Obsolete(AUnitMemberDesign design) {
        UserDesigns.ObsoleteDesign(design as SettlementCmdModuleDesign);
    }

    protected override IEnumerable<AEquipmentStat> GetUserCurrentEquipmentStatsAvailableFor(AUnitMemberDesign pickedDesign) {
        return UserDesigns.GetCurrentCmdModuleOptEquipStats();
    }

}

