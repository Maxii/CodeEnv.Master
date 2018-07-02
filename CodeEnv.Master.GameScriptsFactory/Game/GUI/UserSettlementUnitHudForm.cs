// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UserSettlementUnitHudForm.cs
// Form used by the UnitHudWindow to display info and allow changes when a user-owned Settlement is selected.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Form used by the UnitHudWindow to display info and allow changes when a user-owned Settlement is selected.
/// </summary>
public class UserSettlementUnitHudForm : ABaseUnitHudForm {

    public override FormID FormID { get { return FormID.UserSettlement; } }

    public new SettlementCmdItem SelectedUnit { get { return base.SelectedUnit as SettlementCmdItem; } }

    #region Pick Design Support

    protected override void ChooseCmdModuleDesignAndFormFleet() {
        if (!DebugControls.Instance.AiChoosesUserCmdModInitialDesigns) {
            HaveUserPickCmdModDesignAndFormFleet();
        }
        else {
            base.ChooseCmdModuleDesignAndFormFleet();
        }
    }

    private void HaveUserPickCmdModDesignAndFormFleet() {
        string dialogText = "Pick the CmdModDesign you wish to use to form a new Hanger Fleet. \nCancel to not form the Fleet.";
        EventDelegate cancelDelegate = new EventDelegate(() => {
            // ShipCreateFleetButton is ignored. Creation of fleet will not occur
            DialogWindow.Instance.Hide();
        });
        DialogWindow.Instance.HaveUserPickCmdModDesign(FormID.SelectFleetCmdModDesignDialog, dialogText, cancelDelegate,
            (chosenCmdModDesign) => FormFleetFrom(chosenCmdModDesign as FleetCmdModuleDesign), useUserActionButton: false);
    }

    protected override void ChooseDesignAndIssueRefitOrderFor(FacilityItem facility) {
        if (!DebugControls.Instance.AiChoosesUserElementRefitDesigns) {
            HaveUserPickDesignAndIssueRefitOrder(facility);
        }
        else {
            base.ChooseDesignAndIssueRefitOrderFor(facility);
        }
    }

    private void HaveUserPickDesignAndIssueRefitOrder(FacilityItem facilityToRefit) {
        string dialogText = "Pick the Design you wish to use to refit {0}. \nCancel to not refit.".Inject(facilityToRefit.Name);
        EventDelegate cancelDelegate = new EventDelegate(() => {
            // FacilityRefitButton is ignored. Facility will not be refit
            DialogWindow.Instance.Hide();
        });
        var existingDesign = facilityToRefit.Data.Design;
        DialogWindow.Instance.HaveUserPickElementRefitDesign(FormID.SelectFacilityDesignDialog, dialogText, cancelDelegate, existingDesign,
            (chosenDesign) => IssueRefitOrderTo(facilityToRefit, chosenDesign as FacilityDesign), useUserActionButton: false);
    }

    protected override void ChooseDesignAndIssueRefitOrderFor(ShipItem ship) {
        if (!DebugControls.Instance.AiChoosesUserElementRefitDesigns) {
            HaveUserPickDesignAndIssueRefitOrder(ship);
        }
        else {
            base.ChooseDesignAndIssueRefitOrderFor(ship);
        }
    }

    private void HaveUserPickDesignAndIssueRefitOrder(ShipItem shipToRefit) {
        string dialogText = "Pick the Design you wish to use to refit {0}. \nCancel to not refit.".Inject(shipToRefit.Name);
        EventDelegate cancelDelegate = new EventDelegate(() => {
            // ShipRefitButton is ignored. Ship will not be refit
            DialogWindow.Instance.Hide();
        });
        var existingDesign = shipToRefit.Data.Design;
        DialogWindow.Instance.HaveUserPickElementRefitDesign(FormID.SelectShipDesignDialog, dialogText, cancelDelegate, existingDesign,
            (chosenDesign) => IssueRefitOrderTo(shipToRefit, chosenDesign as ShipDesign), useUserActionButton: false);
    }

    #endregion

    protected override void AssessInteractibleHud() {
        if (_pickedFacilityIcons.Count == Constants.One) {
            InteractibleHudWindow.Instance.Show(FormID.UserFacility, _pickedFacilityIcons.First().Element.Data);
        }
        else if (SelectedUnit != null) {    // 9.14.17 if SelectedUnit has been destroyed, reference will test as null
            InteractibleHudWindow.Instance.Show(FormID.UserSettlement, SelectedUnit.Data);
        }
        else {
            InteractibleHudWindow.Instance.Hide();
        }
    }

}

