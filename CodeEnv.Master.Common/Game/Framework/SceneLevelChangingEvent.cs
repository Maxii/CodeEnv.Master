// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SceneLevelChangingEvent.cs
// Event indicating a SceneLevel change via Application.LoadLevel(sceneLevel) is imminent.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    public class SceneLevelChangingEvent : GameEvent {

        public SceneLevel NewSceneLevel { get; private set; }

        public SceneLevelChangingEvent(SceneLevel newLevel) {
            NewSceneLevel = newLevel;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

