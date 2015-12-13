// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnconsumedMouseButtonPress.cs
//  Container class indicating a Mouse Button has been Pressed but the event was not consumed.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Container class indicating a Mouse Button has been Pressed but the event was not consumed.
    /// </summary>
    public class UnconsumedMouseButtonPress {

        public bool IsDown { get; private set; }

        public NguiMouseButton Button { get; private set; }

        public UnconsumedMouseButtonPress(NguiMouseButton button, bool isDown) {
            Button = button;
            IsDown = isDown;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

