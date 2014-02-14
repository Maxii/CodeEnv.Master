// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: References.cs
// Simple source of useful static references to important MonoBehaviour interfaces.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Simple source of useful static references to important MonoBehaviour interfaces.
    /// </summary>
    public static class References {

        public static IGameManager GameManager { get; set; }

        public static ICameraControl CameraControl { get; set; }

        public static IDynamicObjects DynamicObjects { get; set; }

        public static IGameInputHelper InputHelper { get; set; }

    }
}


