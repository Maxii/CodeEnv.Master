// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IDebugHud.cs
// Interface for DebugHuds so non-scripts can refer to it.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for DebugHuds so non-scripts can refer to it.
    /// </summary>
    public interface IDebugHud : IHud {

        /// <summary>
        /// Simplist way to publishes text for the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="text">The text.</param>
        void Publish(DebugHudLineKeys key, string text);

    }
}

