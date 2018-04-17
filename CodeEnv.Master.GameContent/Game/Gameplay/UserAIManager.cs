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

    using CodeEnv.Master.Common;

    /// <summary>
    /// The AI Manager for the user player.
    /// </summary>
    public class UserAIManager : PlayerAIManager {

        public new UserPlayerKnowledge Knowledge { get { return base.Knowledge as UserPlayerKnowledge; } }

        public new UserResearchManager ResearchMgr { get { return base.ResearchMgr as UserResearchManager; } }

        public UserAIManager(UserPlayerKnowledge knowledge)
            : base(GameReferences.GameManager.UserPlayer, knowledge) { }    // Warning: _gameMgr in base not yet initialized

        protected override PlayerResearchManager InitializeResearchMgr() {
            return new UserResearchManager(this, Designs);
        }

        public override void PickFirstResearchTask() {
            if (_debugControls.UserSelectsTechs) {
                return;
            }
            base.PickFirstResearchTask();
        }

        public override bool TryPickNextResearchTask(ResearchTask justCompletedRsch, out ResearchTask nextRschTask, out bool isFutureTechRuntimeCreation) {
            if (_debugControls.UserSelectsTechs) {
                bool isNextTaskSelected = base.TryPickNextResearchTask(justCompletedRsch, out nextRschTask, out isFutureTechRuntimeCreation);
                D.Assert(isNextTaskSelected);
                return false;   // aka don't assign next task yet as User must pick it in ResearchScreen
            }
            return base.TryPickNextResearchTask(justCompletedRsch, out nextRschTask, out isFutureTechRuntimeCreation);
        }


    }
}

