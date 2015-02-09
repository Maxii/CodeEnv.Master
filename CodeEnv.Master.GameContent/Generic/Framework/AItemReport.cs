// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AItemReport.cs
// Abstract class for Reports associated with an AItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Abstract class for Reports associated with an AItem.
    /// </summary>
    public abstract class AItemReport {

        public string Name { get; protected set; }

        public Player Owner { get; protected set; }

        public Player Player { get; private set; }

        public AItemReport(Player player) {
            Player = player;
        }

    }
}

