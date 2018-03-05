// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AiSettlementUnitHudForm.cs
// Form used by the UnitHudWindow to display info and allow changes when a AI-owned Settlement is selected.
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
/// Form used by the UnitHudWindow to display info and allow changes when a AI-owned Settlement is selected.
/// </summary>
public class AiSettlementUnitHudForm : ABaseUnitHudForm {

    public override FormID FormID { get { return FormID.AiSettlement; } }

    public new SettlementCmdItem SelectedUnit { get { return base.SelectedUnit as SettlementCmdItem; } }

    protected override void InitializeConstructionModule() {
        bool isCurrentConstructionKnownToUser = SelectedUnit.Data.InfoAccessCntlr.HasIntelCoverageReqdToAccess(_gameMgr.UserPlayer, ItemInfoID.CurrentConstruction);
        if (isCurrentConstructionKnownToUser) {
            base.InitializeConstructionModule();
        }
        else {
            DisableConstructionModuleButtons();
        }

        if (!DebugControls.Instance.AreAiUnitHudButtonsFunctional) {
            DisableConstructionModuleButtons();
        }
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

    protected override void AssessUnitCompositionButtons() {
        if (DebugControls.Instance.AreAiUnitHudButtonsFunctional) {
            base.AssessUnitCompositionButtons();
        }
        else {
            DisableUnitCompositionButtons();
        }
    }

    protected override void AssessHangerButtons() {
        if (DebugControls.Instance.AreAiUnitHudButtonsFunctional) {
            base.AssessHangerButtons();
        }
        else {
            DisableHangerButtons();
        }
    }

    protected override void BuildUnitCompositionIcons() {
        bool isCompositionKnownToUser = SelectedUnit.UserReport.UnitComposition != null;
        if (isCompositionKnownToUser) {
            base.BuildUnitCompositionIcons();
        }
    }

    protected override void BuildHangerShipIcons() {
        bool isCompositionKnownToUser = SelectedUnit.UserReport.UnitComposition != null;
        if (isCompositionKnownToUser) {
            base.BuildHangerShipIcons();
        }
    }

    protected override void AssessInteractibleHud() {
        if (_pickedFacilityIcons.Count == Constants.One) {
            InteractibleHudWindow.Instance.Show(FormID.AiFacility, _pickedFacilityIcons.First().Element.UserReport);
        }
        else if (SelectedUnit != null) {    // 9.14.17 if SelectedUnit has been destroyed, reference will test as null
            InteractibleHudWindow.Instance.Show(FormID.AiSettlement, SelectedUnit.UserReport);
        }
        else {
            InteractibleHudWindow.Instance.Hide();
        }
    }


}

