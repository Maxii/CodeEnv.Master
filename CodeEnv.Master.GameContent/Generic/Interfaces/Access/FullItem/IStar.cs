// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IStar.cs
// Interface for easy access to MonoBehaviours that are StarItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Interface for easy access to MonoBehaviours that are StarItems.
    /// </summary>
    public interface IStar : IIntelItem {

        Index3D SectorIndex { get; }


    }
}

