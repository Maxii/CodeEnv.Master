// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ITrackingWidgetFactory.cs
// Interface for easy access to the TrackingWidgetFactory.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Interface for easy access to the TrackingWidgetFactory.
    /// </summary>
    public interface ITrackingWidgetFactory {

        /// <summary>
        /// Creates a label on the UI layer that tracks the <c>target</c>. It does not interact with the mouse.
        /// </summary>
        /// <param name="trackedTgt">The tracked TGT.</param>
        /// <param name="placement">The placement.</param>
        /// <param name="min">The minimum show distance from the camera.</param>
        /// <param name="max">The maximum show distance from the camera.</param>
        /// <returns></returns>
        ITrackingWidget MakeUITrackingLabel(IWidgetTrackable trackedTgt, WidgetPlacement placement = WidgetPlacement.Over, float min = Constants.ZeroF, float max = Mathf.Infinity);

        /// <summary>
        /// Creates a sprite on the UI layer that tracks the <c>target</c>. It does not interact with the mouse.
        /// </summary>
        /// <param name="trackedTgt">The target.</param>
        /// <param name="atlasID">The atlas identifier.</param>
        /// <param name="placement">The placement.</param>
        /// <returns></returns>
        ITrackingWidget MakeUITrackingSprite(IWidgetTrackable trackedTgt, AtlasID atlasID, WidgetPlacement placement = WidgetPlacement.Above);

        /// <summary>
        /// Creates a sprite whose size doesn't change on the screen. It does interact with the mouse.
        /// <remarks>The approach taken to UIPanel usage will be determined by the DebugControls setting.</remarks>
        /// </summary>
        /// <param name="trackedTgt">The target this sprite will track.</param>
        /// <param name="iconInfo">The info needed to build the sprite.</param>
        /// <returns></returns>
        IInteractiveWorldTrackingSprite MakeInteractiveWorldTrackingSprite(IWidgetTrackable trackedTgt, IconInfo iconInfo);

        /// <summary>
        /// Creates a sprite whose size doesn't change on the screen. It does not interact with the mouse.
        /// <remarks>The approach taken to UIPanel usage will be determined by the DebugControls setting.</remarks>
        /// </summary>
        /// <param name="trackedTgt">The target this sprite will track.</param>
        /// <param name="iconInfo">The info needed to build the sprite.</param>
        /// <returns></returns>
        IWorldTrackingSprite MakeWorldTrackingSprite(IWidgetTrackable trackedTgt, IconInfo iconInfo);

        /// <summary>
        /// Creates a sprite whose size doesn't change on the screen. It does not interact with the mouse.
        /// <remarks>The approach taken to UIPanel usage will be determined by the DebugControls setting.</remarks>
        /// </summary>
        /// <param name="trackedTgt">The target this sprite will track.</param>
        /// <param name="iconInfo">The info needed to build the sprite.</param>
        /// <returns></returns>
        ITrackingWidget MakeWorldTrackingLabel(IWidgetTrackable trackedTgt, WidgetPlacement placement = WidgetPlacement.Above);

        /// <summary>
        /// Creates a sprite whose size doesn't change on the screen. It does interact with the mouse.
        /// <remarks>This version has its own UIPanel which is parented to the tracked target.</remarks>
        /// </summary>
        /// <param name="trackedTgt">The target this sprite will track.</param>
        /// <param name="iconInfo">The info needed to build the sprite.</param>
        /// <returns></returns>
        IInteractiveWorldTrackingSprite_Independent MakeInteractiveWorldTrackingSprite_Independent(IWidgetTrackable trackedTgt, IconInfo iconInfo);

        /// <summary>
        /// Creates a sprite whose size doesn't change on the screen. It does interact with the mouse.
        /// <remarks>This version is parented to a common UIPanel for the tracked target's type.</remarks>
        /// </summary>
        /// <param name="trackedTgt">The target this sprite will track.</param>
        /// <param name="iconInfo">The info needed to build the sprite.</param>
        /// <returns></returns>
        IInteractiveWorldTrackingSprite MakeInteractiveWorldTrackingSprite_Common(IWidgetTrackable trackedTgt, IconInfo iconInfo);

        /// <summary>
        /// Creates a sprite whose size doesn't change on the screen. It does not interact with the mouse.
        /// <remarks>This version has its own UIPanel which is parented to the tracked target.</remarks>
        /// </summary>
        /// <param name="trackedTgt">The target.</param>
        /// <param name="iconInfo">The icon information.</param>
        /// <returns></returns>
        IWorldTrackingSprite_Independent MakeWorldTrackingSprite_Independent(IWidgetTrackable trackedTgt, IconInfo iconInfo);

        /// <summary>
        /// Creates a sprite whose size doesn't change on the screen. It does not interact with the mouse.
        /// <remarks>This version is parented to a common UIPanel for the tracked target's type.</remarks>
        /// </summary>
        /// <param name="trackedTgt">The target.</param>
        /// <param name="iconInfo">The icon information.</param>
        /// <param name="parentFolder">The parent folder.</param>
        /// <returns></returns>
        IWorldTrackingSprite MakeWorldTrackingSprite_Common(IWidgetTrackable trackedTgt, IconInfo iconInfo);

        /// <summary>
        /// Creates a label whose size doesn't change on the screen. It does not interact with the mouse.
        /// <remarks>This version has its own UIPanel which is parented to the tracked target.</remarks>
        /// </summary>
        /// <param name="trackedTgt">The target.</param>
        /// <param name="placement">The placement.</param>
        /// <returns></returns>
        ITrackingWidget MakeWorldTrackingLabel_Independent(IWidgetTrackable trackedTgt, WidgetPlacement placement = WidgetPlacement.Above);

        /// <summary>
        /// Creates a label whose size doesn't change on the screen. It does not interact with the mouse.
        /// <remarks>This version is parented to a common UIPanel for the tracked target's type.</remarks>
        /// </summary>
        /// <param name="trackedTgt">The target.</param>
        /// <param name="placement">The placement.</param>
        /// <returns></returns>
        ITrackingWidget MakeWorldTrackingLabel_Common(IWidgetTrackable trackedTgt, WidgetPlacement placement = WidgetPlacement.Above);

        /// <summary>
        /// Makes and returns an invisible CameraLosChangedListener, parented to and tracking the trackedTgt.
        /// </summary>
        /// <param name="trackedTgt">The tracked TGT.</param>
        /// <param name="listenerLayer">The listener layer.</param>
        /// <returns></returns>
        ICameraLosChangedListener MakeInvisibleCameraLosChangedListener(IWidgetTrackable trackedTgt, Layers listenerLayer);

        /// <summary>
        /// Makes an IWidgetTrackable location useful for hosting an invisible CameraLosChangedListener.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns></returns>
        IWidgetTrackable MakeTrackableLocation(GameObject parent);

        /// <summary>
        /// Creates a label whose size changes on the screen. It does not interact with the mouse.
        /// <remarks>This version has its own UIPanel which is parented to the tracked target.</remarks>
        /// </summary>
        /// <param name="trackedTgt">The target.</param>
        /// <param name="placement">The placement.</param>
        /// <returns></returns>
        ITrackingWidget MakeWorldTrackingLabel_IndependentVariableSize(IWidgetTrackable trackedTgt, WidgetPlacement placement = WidgetPlacement.Above);

        /// <summary>
        /// Creates a sprite whose size changes on the screen. It does not interact with the mouse.
        /// <remarks>This version has its own UIPanel which is parented to the tracked target.</remarks>
        /// </summary>
        /// <param name="trackedTgt">The target.</param>
        /// <param name="atlasID">The atlas identifier.</param>
        /// <param name="placement">The placement.</param>
        /// <returns></returns>
        ITrackingWidget MakeWorldTrackingSprite_IndependentVariableSize(IWidgetTrackable trackedTgt, AtlasID atlasID, WidgetPlacement placement = WidgetPlacement.Above);


    }
}

