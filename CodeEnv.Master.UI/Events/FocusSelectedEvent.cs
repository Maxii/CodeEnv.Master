// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FocusSelectedEvent.cs
// Event in support of the Select Focus user action.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.UI {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Resources;
    using UnityEngine;

    public class FocusSelectedEvent : GameEvent {

        public Transform Focus { get; private set; }

        public FocusSelectedEvent(Transform focus) {
            Focus = focus;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

