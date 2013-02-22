// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OptionChangeEvent.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    public class OptionChangeEvent : GameEvent {

        public OptionSettings Settings { get; private set; }

        public OptionChangeEvent(OptionSettings settings) {
            Settings = settings;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

