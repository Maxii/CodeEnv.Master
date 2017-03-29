// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFpsReadout.cs
// Interface for easy access to FPSReadout.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for easy access to FPSReadout.
    /// </summary>
    public interface IFpsReadout {

        float FramesPerSecond { get; }

        /// <summary>
        /// Indicates whether this FPS Readout should display its value.
        /// </summary>
        bool IsReadoutToShow { get; set; }

    }
}

