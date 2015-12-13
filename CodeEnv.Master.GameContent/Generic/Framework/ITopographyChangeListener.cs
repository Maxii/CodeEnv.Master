// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ITopographyChangeListener.cs
// Interface for mobile objects that need to know when they make
// a Topography Boundary transition.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Interface for mobile objects that need to know when they make
    /// a Topography Boundary transition.
    /// </summary>
    public interface ITopographyChangeListener {

        void HandleTopographyChanged(Topography newTopography);

    }
}

