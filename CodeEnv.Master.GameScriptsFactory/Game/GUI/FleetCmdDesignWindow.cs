// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCmdDesignWindow.cs
// GuiWindow used by the User to design FleetCmdModules, managed by the DesignScreensManager.
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
/// GuiWindow used by the User to design FleetCmdModules, managed by the DesignScreensManager.
/// </summary>
public class FleetCmdDesignWindow : AUnitDesignWindow {

    protected override void AddToPlayerDesigns(AUnitMemberDesign newDesign) {
        UserDesigns.Add(newDesign as FleetCmdModuleDesign);
    }

    protected override AUnitMemberDesign CopyDesignFrom(AUnitMemberDesign design) {
        return new FleetCmdModuleDesign(design as FleetCmdModuleDesign);
    }

    protected override bool TryGet3DModelFor(AUnitMemberDesign design, out Vector3 modelDimensions, out GameObject modelPrefab) {
        modelDimensions = default(Vector3);
        modelPrefab = null;
        return false;
    }

    protected override IEnumerable<AUnitMemberDesign> GetRegisteredUserDesigns(bool includeObsolete) {
        return UserDesigns.GetAllDeployableFleetCmdModDesigns(includeObsolete, includeDefault: false).Cast<AUnitMemberDesign>();
    }

    protected override bool TryGetCreateDesignPopupContent(out string popupTitle, out List<string> popupContent) {
        popupTitle = null;
        popupContent = null;
        return false;
    }

    protected override AUnitMemberDesign GetEmptyTemplateDesign(string emptyTemplateHint) {
        D.AssertNull(emptyTemplateHint);
        return UserDesigns.GetCurrentFleetCmdModTemplateDesign();
    }

    protected override void Obsolete(AUnitMemberDesign design) {
        UserDesigns.ObsoleteDesign(design as FleetCmdModuleDesign);
    }

    protected override IEnumerable<AEquipmentStat> GetUserCurrentEquipmentStatsAvailableFor(AUnitMemberDesign pickedDesign) {
        return UserDesigns.GetCurrentCmdModuleOptEquipStats();
    }

}

