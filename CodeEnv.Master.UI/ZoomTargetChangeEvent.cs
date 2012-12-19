// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ZoomTargetChangeEvent.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.UI {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Resources;
    using UnityEngine;

    public class ZoomTargetChangeEvent : GameEvent {

        /// <summary>
        /// Gets the zoom target transform. Can be null if no longer the Zoom Target.
        /// </summary>
        /// <value>
        /// The zoom target when the mouse has entered the object, or null if the mouse has left it.
        /// </value>
        public Transform ZoomTarget { get; private set; }

        public ZoomTargetChangeEvent(Transform zoomTarget) {
            ZoomTarget = zoomTarget;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }



    }
}

