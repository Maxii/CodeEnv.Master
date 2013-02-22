// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CameraRollOptionChangedEvent.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    public class CameraRollOptionChangedEvent : GameEvent {

        public bool IsCameraRollEnabled { get; private set; }

        public CameraRollOptionChangedEvent(bool isCameraRollEnabled) {
            IsCameraRollEnabled = isCameraRollEnabled;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

