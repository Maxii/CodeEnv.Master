// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UserFleetUnitHudForm.cs
// Form used by the UnitHudWindow to display info and allow changes when a user-owned Fleet is selected.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Form used by the UnitHudWindow to display info and allow changes when a user-owned Fleet is selected.
/// </summary>
public class UserFleetUnitHudForm : AFleetUnitHudForm {

    public override FormID FormID { get { return FormID.UserFleet; } }

    #region Pick Design Support

    protected override void ChooseCmdModuleDesignAndFormFleet() {
        if (!DebugControls.Instance.AiChoosesUserCmdModInitialDesigns) {
            HaveUserPickCmdModDesign();
        }
        else {
            base.ChooseCmdModuleDesignAndFormFleet();
        }
    }

    private void HaveUserPickCmdModDesign() {
        string dialogText = "Pick the CmdModDesign you wish to use to split off a new Fleet. \nCancel to not split off the Fleet.";
        EventDelegate cancelDelegate = new EventDelegate(() => {
            // ShipCreateFleetButton is ignored. Creation of fleet will not occur
            DialogWindow.Instance.Hide();
        });
        bool useUserActionButton = false;
        DialogWindow.Instance.HaveUserPickCmdModDesign(FormID.SelectFleetCmdModDesignDialog, dialogText, cancelDelegate,
            (chosenCmdModDesign) => FormFleetFrom(chosenCmdModDesign as FleetCmdModuleDesign), useUserActionButton);
    }

    #endregion

    protected override void AssessInteractibleHud() {
        if (_pickedShipIcons.Count == Constants.One) {
            InteractibleHudWindow.Instance.Show(FormID.UserShip, _pickedShipIcons.First().Element.Data);
        }
        else if (_pickedUnitIcons.Count == Constants.One) {
            InteractibleHudWindow.Instance.Show(FormID.UserFleet, _pickedUnitIcons.First().Unit.Data);
        }
        else {
            InteractibleHudWindow.Instance.Hide();
        }
    }


}

