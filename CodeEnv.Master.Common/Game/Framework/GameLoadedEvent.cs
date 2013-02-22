// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameLoadedEvent.cs
// Event indicating a new or saved game has been loaded and is ready to run.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    public class GameLoadedEvent : GameEvent {

        public GameLoadedEvent() { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

