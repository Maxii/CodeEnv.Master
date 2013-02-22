﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ResetOnFocusOptionChangedEvent.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    public class ResetOnFocusOptionChangedEvent : GameEvent {

        public bool IsResetOnFocusEnabled { get; private set; }

        public ResetOnFocusOptionChangedEvent(bool isResetOnFocusEnabled) {
            IsResetOnFocusEnabled = isResetOnFocusEnabled;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }


    }
}

