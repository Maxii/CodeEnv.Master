// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UserPlayerAIManager.cs
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
    public class UserPlayerAIManager : PlayerAIManager {

        public new UserPlayerKnowledge Knowledge { get { return base.Knowledge as UserPlayerKnowledge; } }

        public UserPlayerAIManager(UserPlayerKnowledge knowledge)
            : base(GameReferences.GameManager.UserPlayer, knowledge) { }    // Warning: _gameMgr in base not yet initialized

        protected override void __HandleResearchCompleted(ResearchTask researchTask) {
            _gameMgr.RequestPauseStateChange(toPause: true);
            D.Log("{0} is pausing to simulate User being prompted to open ResearchWindow to select a new Technology.", DebugName);
            base.__HandleResearchCompleted(researchTask);
            _gameMgr.RequestPauseStateChange(toPause: false);
        }
    }
}

