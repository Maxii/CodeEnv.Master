// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: APausableKillableYieldInstruction.cs
// Abstract base class allowing Unity CustomYieldInstructions to be paused and killed.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using UnityEngine;

    /// <summary>
    /// Abstract base class allowing Unity CustomYieldInstructions to be paused and killed.
    /// </summary>
    public abstract class APausableKillableYieldInstruction : CustomYieldInstruction {

        public bool IsPaused { protected get; set; }

        protected bool _toKill = false;

        public void Kill() {
            _toKill = true;
        }

    }
}

