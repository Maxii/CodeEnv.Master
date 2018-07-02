// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UserResearchManager.cs
// Manages the progression of research on technologies for the User.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Manages the progression of research on technologies for the User.
    /// <remarks>Access via UserAIManager.</remarks>
    /// </summary>
    public class UserResearchManager : PlayerResearchManager {

        public UserResearchManager(UserAIManager userAiMgr, PlayerDesigns designs)
            : base(userAiMgr, designs) { }

        protected override void HandleResearchCompleted(ResearchTask completedRsch) {
            base.HandleResearchCompleted(completedRsch);
            if (!IsResearchQueued && __debugCntls.UserSelectsTechs) {
                GameReferences.UserActionButton.ShowPickResearchPromptButton(completedRsch);
            }
        }

        /// <summary>
        /// Returns <c>true</c> if there are ResearchTasks queued, <c>false</c> otherwise.
        /// <remarks>The CurrentResearchTask does not count as queued.</remarks>
        /// </summary>
        /// <param name="queuedTasks">The resulting queued ResearchTasks, if any.</param>
        /// <returns></returns>
        public bool TryGetQueuedResearchTasks(out IList<ResearchTask> queuedTasks) {
            queuedTasks = new List<ResearchTask>(_pendingRschTaskQueue);
            return queuedTasks.Any();
        }

        #region Debug

        #endregion


    }
}

