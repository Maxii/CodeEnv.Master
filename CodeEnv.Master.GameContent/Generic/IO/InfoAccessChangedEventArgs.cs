// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: InfoAccessChangedEventArgs.cs
// EventArg supporting InfoAccessChanged events.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// EventArg supporting InfoAccessChanged events.
    /// </summary>
    public class InfoAccessChangedEventArgs : EventArgs {

        /// <summary>
        /// The player whose Info access rights to this item changed.
        /// </summary>
        public Player Player { get; private set; }

        public InfoAccessChangedEventArgs(Player player) {
            Player = player;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

