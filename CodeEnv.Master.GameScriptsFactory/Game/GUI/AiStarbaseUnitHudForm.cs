// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AiStarbaseUnitHudForm.cs
// Form used by the UnitHudWindow to display info and allow changes when a AI-owned Starbase is selected.
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
/// Form used by the UnitHudWindow to display info and allow changes when a AI-owned Starbase is selected.
/// </summary>
public class AiStarbaseUnitHudForm : ABaseUnitHudForm {

    public override FormID FormID { get { return FormID.AiStarbase; } }

    public new StarbaseCmdItem SelectedUnit { get { return base.SelectedUnit as StarbaseCmdItem; } }

    protected override void InitializeConstructionModule() {
        bool isCurrentConstructionKnownToUser = SelectedUnit.Data.InfoAccessCntlr.HasIntelCoverageReqdToAccess(_gameMgr.UserPlayer, ItemInfoID.CurrentConstruction);
        if (isCurrentConstructionKnownToUser) {
            base.InitializeConstructionModule();
            if (!DebugControls.Instance.AreAiUnitHudButtonsFunctional) {
                DisableConstructionModuleButtons();
            }
        }
        else {
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

    // 6.5.18 AI HudForm no longer uses SelectCmdModuleDesign DialogWindow

    protected override void AssessInteractibleHud() {
        if (_pickedFacilityIcons.Count == Constants.One) {
            InteractibleHudWindow.Instance.Show(FormID.AiFacility, _pickedFacilityIcons.First().Element.UserReport);
        }
        else if (SelectedUnit != null) {    // 9.14.17 if SelectedUnit has been destroyed, reference will test as null
            InteractibleHudWindow.Instance.Show(FormID.AiStarbase, SelectedUnit.UserReport);
        }
        else {
            InteractibleHudWindow.Instance.Hide();
        }
    }

}

