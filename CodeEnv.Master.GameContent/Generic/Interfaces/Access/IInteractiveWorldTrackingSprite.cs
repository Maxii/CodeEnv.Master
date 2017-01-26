// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IInteractiveWorldTrackingSprite.cs
// Interface for easy access to InteractiveWorldTrackingSprites.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for easy access to InteractiveWorldTrackingSprites.
    /// </summary>
    public interface IInteractiveWorldTrackingSprite : IWorldTrackingSprite {

        IMyEventListener EventListener { get; }

    }
}

