// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SceneChangingEvent.cs
// Event indicating a Scene change via Application.LoadLevel(level) is imminent.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    public class SceneChangingEvent : AEnumValueChangeEvent<SceneLevel> {

        public SceneChangingEvent(object source, SceneLevel newScene)
            : base(source, newScene) {
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

