// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IWorldTrackingSprite_Independent.cs
// Interface for easy access to WorldTrackingSprite_Independent (ConstantSize) sprites that 
// are a child of the tracked object.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for easy access to WorldTrackingSprite_Independent (ConstantSize) sprites that 
    /// are a child of the tracked object.
    /// </summary>
    public interface IWorldTrackingSprite_Independent : IWorldTrackingSprite {

        /// <summary>
        /// The depth of the UIPanel that determines draw order. Higher values will be
        /// drawn after lower values placing them in front of the lower values. In general, 
        /// these depth values should be less than 0 as the Panels that manage the UI are
        /// usually set to 0 so they draw over other Panels.
        /// </summary>
        int DrawDepth { get; set; }

    }
}

