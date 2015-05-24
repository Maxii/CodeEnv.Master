// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IHud.cs
// Interface for Huds so non-scripts can refer to it.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Text;

    /// <summary>
    /// Interface for Huds so non-scripts can refer to it.
    /// </summary>
    [System.Obsolete]
    public interface IHud {

        /// <summary>
        /// Clear the HUD.
        /// </summary>
        void Clear();

    }
}

