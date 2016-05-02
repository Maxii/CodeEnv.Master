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

        ITrackingWidget MakeUITrackingLabel(IWidgetTrackable target, WidgetPlacement placement = WidgetPlacement.Over, float min = Constants.ZeroF, float max = Mathf.Infinity);

        /// <summary>
        /// Creates a sprite on the UI layer that tracks the <c>target</c>.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="atlasID">The atlas identifier.</param>
        /// <param name="placement">The placement.</param>
        /// <param name="min">The minimum show distance.</param>
        /// <param name="max">The maximum show distance.</param>
        /// <returns></returns>
        ITrackingWidget MakeUITrackingSprite(IWidgetTrackable target, AtlasID atlasID, WidgetPlacement placement = WidgetPlacement.Above, float min = Constants.ZeroF, float max = Mathf.Infinity);

        /// <summary>
        /// Creates a tracking sprite which can respond to the mouse.
        /// The sprite's size stays constant, parented to and tracks the <c>target</c>.
        /// </summary>
        /// <param name="target">The target this sprite will track.</param>
        /// <param name="iconInfo">The info needed to build the sprite.</param>
        /// <param name="min">The minimum show distance.</param>
        /// <param name="max">The maximum show distance.</param>
        /// <returns></returns>
        IResponsiveTrackingSprite MakeResponsiveTrackingSprite(IWidgetTrackable target, IconInfo iconInfo, float min = Constants.ZeroF, float max = Mathf.Infinity);

        /// <summary>
        /// Creates a label whose size scales with the size of the target, parented to and tracking the <c>target</c>.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="placement">The placement.</param>
        /// <param name="min">The minimum show distance.</param>
        /// <returns></returns>
        ITrackingWidget MakeVariableSizeTrackingLabel(IWidgetTrackable target, WidgetPlacement placement = WidgetPlacement.Above, float min = Constants.ZeroF);

        /// <summary>
        /// Creates a sprite whose size scales with the size of the target, parented to and tracking the <c>target</c>.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="atlasID">The atlas identifier.</param>
        /// <param name="placement">The placement.</param>
        /// <param name="min">The minimum show distance.</param>
        /// <returns></returns>
        ITrackingWidget MakeVariableSizeTrackingSprite(IWidgetTrackable target, AtlasID atlasID, WidgetPlacement placement = WidgetPlacement.Above, float min = Constants.ZeroF);

        /// <summary>
        /// Creates a sprite whose size stays constant, independent of the size of the target, parented to and tracking the <c>target</c>.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="iconInfo">The icon information.</param>
        /// <param name="min">The minimum show distance.</param>
        /// <param name="max">The maximum show distance.</param>
        /// <returns></returns>
        ITrackingSprite MakeConstantSizeTrackingSprite(IWidgetTrackable target, IconInfo iconInfo, float min = Constants.ZeroF, float max = Mathf.Infinity);

        /// <summary>
        ///  Creates a label whose size stays constant, independent of the size of the target, parented to and tracking the <c>target</c>.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="placement">The placement.</param>
        /// <param name="min">The minimum show distance.</param>
        /// <param name="max">The maximum show distance.</param>
        /// <returns></returns>
        ITrackingWidget MakeConstantSizeTrackingLabel(IWidgetTrackable target, WidgetPlacement placement = WidgetPlacement.Above, float min = Constants.ZeroF, float max = Mathf.Infinity);

    }
}

