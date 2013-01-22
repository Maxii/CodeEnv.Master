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

namespace CodeEnv.Master.Common.UI {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.Resources;
    using UnityEngine;

    public class FocusSelectedEvent : GameEvent {

        public Transform FocusTransform { get; private set; }

        public FocusSelectedEvent(Transform focusTransform) {
            FocusTransform = focusTransform;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

