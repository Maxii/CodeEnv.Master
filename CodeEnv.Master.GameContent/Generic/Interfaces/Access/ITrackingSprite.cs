// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ITrackingSprite.cs
// Interface for easy access to ConstantSizeTrackingSprite.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

using CodeEnv.Master.Common;

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for easy access to ConstantSizeTrackingSprite.
    /// </summary>
    public interface ITrackingSprite : ITrackingWidget {

        IconInfo IconInfo { get; set; }

        ICameraLosChangedListener CameraLosChangedListener { get; }

    }
}

