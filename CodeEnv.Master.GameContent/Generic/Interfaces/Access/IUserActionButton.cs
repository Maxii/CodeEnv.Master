// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IUserActionButton.cs
// Interface for easy access to UserActionButton MonoBehaviour Singleton.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for easy access to UserActionButton MonoBehaviour Singleton.
    /// </summary>
    public interface IUserActionButton {

        void ShowPickResearchPromptButton(ResearchTask completedResearch);

    }
}

