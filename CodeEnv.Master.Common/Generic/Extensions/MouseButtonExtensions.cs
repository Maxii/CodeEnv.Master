// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MouseButtonExtensions.cs
// Extensions for Unity and Ngui Mouse Button enums.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    public static class MouseButtonExtensions {

        public static int ToUnityMouseButton(this NguiMouseButton button) {
            return -1 - (int)button;
        }

        public static int ToNguiMouseButton(this UnityMouseButton button) {
            return -((int)button + 1);
        }

    }
}

