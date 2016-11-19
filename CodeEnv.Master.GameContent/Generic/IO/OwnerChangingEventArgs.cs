// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OwnerChangingEventArgs.cs
// Event Argument class for OwnerChanging events that only contain the incoming owner.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Event Argument class for OwnerChanging events that only contain the incoming owner.
    /// </summary>
    public class OwnerChangingEventArgs : EventArgs {

        public Player IncomingOwner { get; private set; }

        public OwnerChangingEventArgs(Player incomingOwner) {
            IncomingOwner = incomingOwner;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

