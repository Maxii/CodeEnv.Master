// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IIntel.cs
// Interface for accessing Intel meta data.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Interface for accessing Intel meta data.
    /// </summary>
    public interface IIntel {

        IntelCoverage DatedCoverage { get; }

        IntelCoverage CurrentCoverage { get; set; }

        IGameDate DateStamp { get; }

    }
}

