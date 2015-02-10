// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AReport.cs
// Abstract base class for Reports associated with an AItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Abstract base class for Reports associated with an AItem.
    /// </summary>
    public abstract class AReport {

        public string Name { get; protected set; }

        public Player Owner { get; protected set; }

        public Player Player { get; private set; }

        public AReport(Player player) {
            Player = player;
        }

    }
}

