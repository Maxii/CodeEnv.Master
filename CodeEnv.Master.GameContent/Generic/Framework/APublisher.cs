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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Abstract base class for Item Publishers that create reports and text.
    /// </summary>
    public abstract class APublisher {

        public virtual string DebugName { get { return GetType().Name; } }

        public abstract ColoredStringBuilder ItemHudText { get; }

        protected IGameManager _gameMgr;

        public APublisher() {
            _gameMgr = GameReferences.GameManager;
        }

        public sealed override string ToString() {
            return DebugName;
        }
    }
}

