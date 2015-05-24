// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: APublisher.cs
// Abstract base class for Report and HudContent Publishers.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Abstract base class for Report and HudContent Publishers.
    /// </summary>
    public abstract class APublisher {

        public abstract ColoredStringBuilder HudContent { get; }

        protected IGameManager _gameMgr;

        public APublisher() {
            _gameMgr = References.GameManager;
        }

    }
}

