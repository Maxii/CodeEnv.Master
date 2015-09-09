// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ITopographyMonitorable.cs
// Interface associated with Items that monitor their Topography boundaries.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Interface associated with Items that monitor their Topography boundries.
    /// </summary>
    [System.Obsolete]
    public interface ITopographyMonitorable {

        Topography Topography { get; }

        string FullName { get; }

        float Radius { get; }

    }
}

