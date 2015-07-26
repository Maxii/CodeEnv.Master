// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IResponsiveTrackingSprite.cs
// Interface for easy access to ResponsiveTrackingSprite.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.ComponentModel;

    /// <summary>
    /// Interface for easy access to ResponsiveTrackingSprite.
    /// </summary>
    public interface IResponsiveTrackingSprite : ITrackingWidget, IChangeTracking, INotifyPropertyChanging, INotifyPropertyChanged {

        IconInfo IconInfo { get; set; }

        ICameraLosChangedListener CameraLosChangedListener { get; }

        IMyNguiEventListener EventListener { get; }

    }
}

