// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IGameManager.cs
//  Interface for easy access to the GameManager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Interface for easy access to the GameManager.
    /// </summary>
    public interface IGameManager {

        event Action<GameState> onCurrentStateChanging;

        event Action onCurrentStateChanged;

        HumanPlayer HumanPlayer { get; }

        GameState CurrentState { get; }

    }
}

