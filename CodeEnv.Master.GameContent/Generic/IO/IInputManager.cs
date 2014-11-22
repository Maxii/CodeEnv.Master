// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IInputManager.cs
// Interface allowing access to the associated Unity-compiled script. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.ComponentModel;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Interface allowing access to the associated Unity-compiled script. 
    /// Typically, a static reference to the script is established in References.cs, providing access to the script from classes located in pre-compiled assemblies.
    /// </summary>
    public interface IInputManager : INotifyPropertyChanged, INotifyPropertyChanging, IChangeTracking {

        /// <summary>
        /// Occurs when the mouse is pressed down, but not over a gameObject.
        /// </summary>
        event Action<NguiMouseButton> onUnconsumedPressDown;

        /// <summary>
        /// The current GameInputMode of the game.
        /// </summary>
        GameInputMode InputMode { get; }

        bool IsDragging { get; }

    }
}

