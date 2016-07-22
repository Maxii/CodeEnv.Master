// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IMortalDisplayManager.cs
// Interface for ADisplayManagers which need to be able to handle the death of their AMortalItem client.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for ADisplayManagers which need to be able to handle the death of their AMortalItem client.
    /// <remarks>Essentially a replacement for a theoretical AMortalDisplayManager that was not implemented.</remarks>
    /// </summary>
    public interface IMortalDisplayManager {

        /// <summary>
        /// Called on the death of the client. Disables the display and ends all InCameraLOS calls.
        /// </summary>
        void HandleDeath();

    }
}

