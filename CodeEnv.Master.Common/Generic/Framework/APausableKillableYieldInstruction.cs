﻿// --------------------------------------------------------------------------------------------------------------------
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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using UnityEngine;

    /// <summary>
    /// Abstract base class allowing Unity CustomYieldInstructions to be paused and killed.
    /// </summary>
    public abstract class APausableKillableYieldInstruction : CustomYieldInstruction {

        public virtual string DebugName { get { return GetType().Name; } }

        public bool IsPaused { protected get; set; }

        public bool IsKilled { get; private set; }

        public void Kill() {
            IsKilled = true;
        }

        public sealed override string ToString() {
            return DebugName;
        }

    }
}

