// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SceneStartupSequenceEvent.cs
// Event raised on the first Awake, Start, Update and LateUpdate event called in a scene.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;

    /// <summary>
    /// Event raised on the first Awake, Start, Update and LateUpdate event called in a scene.
    /// </summary>
    [Obsolete]
    public class SceneStartupSequenceEvent : AGameEvent {

        public SceneStartupEventName EventName { get; private set; }

        public SceneStartupSequenceEvent(object source, SceneStartupEventName eventName)
            : base(source) {
            EventName = eventName;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }

    [Obsolete]
    public enum SceneStartupEventName {
        None,
        Awake,
        Start,
        Update,
        LateUpdate
    }
}

