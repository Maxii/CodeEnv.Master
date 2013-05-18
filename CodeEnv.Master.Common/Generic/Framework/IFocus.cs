// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFocus.cs
// Interface designating the game object it is attached too as a focusable
// object that can be approached by the camera.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Interface designating the game object it is attached too as a focusable
    /// object that can be approached by the camera.
    /// </summary>
    public interface IFocus : ICameraTarget {

        float OptimalCameraApproachDistance { get; }

        void OnClick();

    }
}

