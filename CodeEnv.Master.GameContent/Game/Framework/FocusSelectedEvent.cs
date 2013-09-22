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

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    public class FocusSelectedEvent : AGameEvent {

        public Transform FocusTransform { get; private set; }

        public FocusSelectedEvent(object source, Transform focusTransform)
            : base(source) {
            FocusTransform = focusTransform;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

