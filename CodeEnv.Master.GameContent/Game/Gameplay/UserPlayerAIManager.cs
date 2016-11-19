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

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// The AI Manager for the user player.
    /// </summary>
    public class UserPlayerAIManager : PlayerAIManager {

        public new UserPlayerKnowledge Knowledge { get { return base.Knowledge as UserPlayerKnowledge; } }

        public UserPlayerAIManager(UserPlayerKnowledge knowledge)
            : base(References.GameManager.UserPlayer, knowledge) { }    // Warning: _gameMgr in base not yet initialized

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

