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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Interface for easy access to the TrackingWidgetFactory.
    /// </summary>
    public interface ITrackingWidgetFactory {

        /// <summary>
        /// Creates a label on the UI layer that tracks the <c>target</c>.
        /// </summary>
        /// <param name="trackedTgt">The tracked TGT.</param>
        /// <param name="placement">The placement.</param>
        /// <param name="min">The minimum show distance from the camera.</param>
        /// <param name="max">The maximum show distance from the camera.</param>
        /// <returns></returns>
        ITrackingWidget MakeUITrackingLabel(IWidgetTrackable trackedTgt, WidgetPlacement placement = WidgetPlacement.Over, float min = Constants.ZeroF, float max = Mathf.Infinity);

        /// <summary>
        /// Creates a sprite on the UI layer that tracks the <c>target</c>.
        /// </summary>
        /// <param name="trackedTgt">The target.</param>
        /// <param name="atlasID">The atlas identifier.</param>
        /// <param name="placement">The placement.</param>
        /// <returns></returns>
        ITrackingWidget MakeUITrackingSprite(IWidgetTrackable trackedTgt, AtlasID atlasID, WidgetPlacement placement = WidgetPlacement.Above);

        /// <summary>
        /// Creates a tracking sprite which can respond to the mouse.
        /// The sprite's size stays constant, parented to and tracks the <c>target</c>.
        /// </summary>
        /// <param name="trackedTgt">The target this sprite will track.</param>
        /// <param name="iconInfo">The info needed to build the sprite.</param>
        /// <returns></returns>
        IResponsiveTrackingSprite MakeResponsiveTrackingSprite(IWidgetTrackable trackedTgt, IconInfo iconInfo);

        /// <summary>
        /// Creates a label whose size scales with the size of the target, parented to and tracking the <c>target</c>.
        /// </summary>
        /// <param name="trackedTgt">The target.</param>
        /// <param name="placement">The placement.</param>
        /// <returns></returns>
        ITrackingWidget MakeVariableSizeTrackingLabel(IWidgetTrackable trackedTgt, WidgetPlacement placement = WidgetPlacement.Above);

        /// <summary>
        /// Creates a sprite whose size scales with the size of the target, parented to and tracking the <c>target</c>.
        /// </summary>
        /// <param name="trackedTgt">The target.</param>
        /// <param name="atlasID">The atlas identifier.</param>
        /// <param name="placement">The placement.</param>
        /// <returns></returns>
        ITrackingWidget MakeVariableSizeTrackingSprite(IWidgetTrackable trackedTgt, AtlasID atlasID, WidgetPlacement placement = WidgetPlacement.Above);

        /// <summary>
        /// Creates a sprite whose size stays constant, independent of the size of the target, parented to and tracking the <c>target</c>.
        /// </summary>
        /// <param name="trackedTgt">The target.</param>
        /// <param name="iconInfo">The icon information.</param>
        /// <returns></returns>
        ITrackingSprite MakeConstantSizeTrackingSprite(IWidgetTrackable trackedTgt, IconInfo iconInfo);

        /// <summary>
        /// Creates a label whose size stays constant, independent of the size of the target, parented to and tracking the <c>target</c>.
        /// </summary>
        /// <param name="trackedTgt">The target.</param>
        /// <param name="placement">The placement.</param>
        /// <returns></returns>
        ITrackingWidget MakeConstantSizeTrackingLabel(IWidgetTrackable trackedTgt, WidgetPlacement placement = WidgetPlacement.Above);


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


    }
}

