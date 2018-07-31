// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UserAIManager.cs
// The AI Manager for the user player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// The AI Manager for the user player.
    /// </summary>
    public class UserAIManager : PlayerAIManager {

        public new UserPlayerKnowledge Knowledge { get { return base.Knowledge as UserPlayerKnowledge; } }

        public new UserResearchManager ResearchMgr { get { return base.ResearchMgr as UserResearchManager; } }

        public UserAIManager(UserPlayerKnowledge knowledge)
            : base(GameReferences.GameManager.UserPlayer, knowledge) { }    // Warning: _gameMgr in base not yet initialized

        #region Research Support

        protected override PlayerResearchManager InitializeResearchMgr() {
            return new UserResearchManager(this, Designs);
        }

        public override void PickFirstResearchTask() {
            if (__debugControls.UserSelectsTechs) {
                return;
            }
            base.PickFirstResearchTask();
        }

        public override bool TryPickNextResearchTask(ResearchTask justCompletedRsch, out ResearchTask nextRschTask, out bool isFutureTechRuntimeCreation) {
            if (__debugControls.UserSelectsTechs) {
                bool isNextTaskSelected = base.TryPickNextResearchTask(justCompletedRsch, out nextRschTask, out isFutureTechRuntimeCreation);
                D.Assert(isNextTaskSelected);
                return false;   // aka don't assign next task yet as User must pick it in ResearchScreen
            }
            return base.TryPickNextResearchTask(justCompletedRsch, out nextRschTask, out isFutureTechRuntimeCreation);
        }

        #endregion

        #region Choose Design

        public override FacilityDesign ChooseDesign(FacilityHullCategory hullCat) {
            D.Assert(_playerPrefsMgr.IsAiHandlesUserCentralHubInitialDesignsEnabled);
            D.AssertEqual(FacilityHullCategory.CentralHub, hullCat);
            return base.ChooseDesign(hullCat);
        }

        public override FacilityDesign ChooseRefitDesign(FacilityDesign existingDesign) {
            D.Assert(_playerPrefsMgr.IsAiHandlesUserElementRefitDesignsEnabled);
            return base.ChooseRefitDesign(existingDesign);
        }

        // Unlike the FacilityCentralHubDesign, no ShipDesign needs to be chosen to form a Fleet.
        // Fleets are only formed from existing ships. While the AI can use this method
        // to pick the designs it wants to build in a hanger, the User never will as those
        // designs are picked from the UnitHud.
        public override ShipDesign ChooseDesign(ShipHullCategory hullCat) {
            throw new NotImplementedException();
        }

        public override ShipDesign ChooseRefitDesign(ShipDesign existingDesign) {
            D.Assert(_playerPrefsMgr.IsAiHandlesUserElementRefitDesignsEnabled);
            return base.ChooseRefitDesign(existingDesign);
        }

        public override FleetCmdModuleDesign ChooseFleetCmdModDesign() {
            D.Assert(_playerPrefsMgr.IsAiHandlesUserCmdModuleInitialDesignsEnabled);
            return base.ChooseFleetCmdModDesign();
        }

        public override FleetCmdModuleDesign ChooseRefitDesign(FleetCmdModuleDesign designToBeRefit) {
            D.Assert(_playerPrefsMgr.IsAiHandlesUserCmdModuleRefitDesignsEnabled);
            return base.ChooseRefitDesign(designToBeRefit);
        }

        public override StarbaseCmdModuleDesign ChooseStarbaseCmdModDesign() {
            D.Assert(_playerPrefsMgr.IsAiHandlesUserCmdModuleInitialDesignsEnabled);
            return base.ChooseStarbaseCmdModDesign();
        }

        public override StarbaseCmdModuleDesign ChooseRefitDesign(StarbaseCmdModuleDesign designToBeRefit) {
            D.Assert(_playerPrefsMgr.IsAiHandlesUserCmdModuleRefitDesignsEnabled);
            return base.ChooseRefitDesign(designToBeRefit);
        }

        public override SettlementCmdModuleDesign ChooseSettlementCmdModDesign() {
            D.Assert(_playerPrefsMgr.IsAiHandlesUserCmdModuleInitialDesignsEnabled);
            return base.ChooseSettlementCmdModDesign();
        }

        public override SettlementCmdModuleDesign ChooseRefitDesign(SettlementCmdModuleDesign designToBeRefit) {
            D.Assert(_playerPrefsMgr.IsAiHandlesUserCmdModuleRefitDesignsEnabled);
            return base.ChooseRefitDesign(designToBeRefit);
        }

        #endregion

    }
}

