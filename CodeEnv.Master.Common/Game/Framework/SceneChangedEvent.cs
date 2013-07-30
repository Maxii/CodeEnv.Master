// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SceneChangedEvent.cs
// Event indicating a Scene change via Application.LoadLevel(level) has just completed.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    public class SceneChangedEvent : AEnumValueChangeEvent<SceneLevel> {

        public SceneChangedEvent(object source, SceneLevel newScene)
            : base(source, newScene) {
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

