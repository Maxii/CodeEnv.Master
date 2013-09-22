// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IIcon.cs
// Interface for AIcons used by IconFactory to make instances of AIcon&lt;T&gt;.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Interface for AIcons used by IconFactory to make instances of AIcon&lt;T&gt;.
    /// </summary>
    public interface IIcon {

        IconSection Section { get; }

        IconSelectionCriteria[] Criteria { get; }

        string Filename { get; }

    }
}

