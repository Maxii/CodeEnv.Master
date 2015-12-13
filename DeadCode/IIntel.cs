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
    [System.Obsolete]
    public interface IIntel {

        bool HasDatedCoverage { get; }

        IntelCoverage DatedCoverage { get; }

        GameDate DateStamp { get; }

        IntelCoverage CurrentCoverage { get; set; }

    }
}

