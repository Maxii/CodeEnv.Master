// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGameEvent.cs
// Abstract Base class for all GameEvents processed by the GameEventManager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Abstract Base class for all GameEvents processed by the GameEventManager.
    /// </summary>
    public abstract class AGameEvent {

        public object Source { get; private set; }

        public AGameEvent(object source) {
            Source = source;
        }

    }
}

