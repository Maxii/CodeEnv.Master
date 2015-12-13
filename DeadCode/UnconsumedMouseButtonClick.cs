// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnconsumedMouseButtonClick.cs
// Container class indicating a Mouse Button has been clicked but the event was not consumed.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Container class indicating a Mouse Button has been clicked but the event was not consumed.
    /// </summary>
    public class UnconsumedMouseButtonClick {

        public NguiMouseButton MouseButton { get; private set; }

        public UnconsumedMouseButtonClick(NguiMouseButton button) {
            MouseButton = button;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

