﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AiFleetUnitHudForm.cs
// Form used by the UnitHudWindow to display info and allow changes when a AI-owned Fleet is selected.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Form used by the UnitHudWindow to display info and allow changes when a AI-owned Fleet is selected.
/// </summary>
public class AiFleetUnitHudForm : AFleetUnitHudForm {

    public override FormID FormID { get { return FormID.AiFleet; } }

    protected override void HandleShipCreateFleetButtonClicked() {
        D.Assert(DebugControls.Instance.AreAiUnitHudButtonsFunctional);
        // Handled this way to allow user to manually pick AI's CmdModDesign

        var acceptDelegate = new EventDelegate(this, "HandlePlayerPickedCmdModDesign");
        var cancelDelegate = new EventDelegate(() => {
            DialogWindow.Instance.Hide();
            // Button is ignored. Creation of fleet will not occur
        });

        var dialogSettings = new APopupDialogForm.DialogSettings(SelectedUnit.Owner, acceptDelegate, cancelDelegate);
        DialogWindow.Instance.Show(FormID.SelectFleetCmdModDesignDialog, dialogSettings);
    }

    private void HandlePlayerPickedCmdModDesign(AUnitMemberDesign pickedDesign) {
        FleetCmdModuleDesign pickedCmdModuleDesign = pickedDesign as FleetCmdModuleDesign;
        D.AssertNotNull(pickedCmdModuleDesign);

        DialogWindow.Instance.Hide();

        ApplyToFleetBeingCreated(pickedCmdModuleDesign);
    }

    protected override void AssessUnitButtons() {
        if (DebugControls.Instance.AreAiUnitHudButtonsFunctional) {
            base.AssessUnitButtons();
        }
        else {
            DisableUnitButtons();
            AssessUnitFocusButton();
        }
    }

    protected override void AssessElementButtons() {
        if (DebugControls.Instance.AreAiUnitHudButtonsFunctional) {
            base.AssessElementButtons();
        }
        else {
            DisableShipButtons();
        }
    }

    protected override void AssessInteractibleHud() {
        if (_pickedShipIcons.Count == Constants.One) {
            InteractibleHudWindow.Instance.Show(FormID.AiShip, _pickedShipIcons.First().Element.UserReport);
        }
        else if (_pickedUnitIcons.Count == Constants.One) {
            InteractibleHudWindow.Instance.Show(FormID.AiFleet, _pickedUnitIcons.First().Unit.UserReport);
        }
        else {
            InteractibleHudWindow.Instance.Hide();
        }
    }

    protected override void BuildPickedUnitsCompositionIcons() {
        bool isPickedUnitsCompositionKnownToUser = _pickedUnitIcons.Select(icon => icon.Unit).All(unit => unit.UserReport.UnitComposition != null);
        if (isPickedUnitsCompositionKnownToUser) {
            base.BuildPickedUnitsCompositionIcons();
        }
        else {
            RemoveAllUnitCompositionIcons();
        }
    }

    #region Debug

    #endregion


}

