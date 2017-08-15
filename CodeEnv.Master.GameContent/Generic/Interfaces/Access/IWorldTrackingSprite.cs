// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IWorldTrackingSprite.cs
// Interface for easy access to WorldTrackingSprites (ConstantSize).
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for easy access to WorldTrackingSprites (ConstantSize).
    /// </summary>
    public interface IWorldTrackingSprite : IWorldTrackingWidget {

        TrackingIconInfo IconInfo { get; set; }

        ICameraLosChangedListener CameraLosChangedListener { get; }

    }
}

