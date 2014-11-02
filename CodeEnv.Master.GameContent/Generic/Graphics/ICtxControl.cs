// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICtxControl.cs
//  Interface for Context Menu Controls.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for Context Menu Controls.
    /// </summary>
    public interface ICtxControl {

        bool IsShowing { get; }

        void OnRightPressRelease();

        void Show(bool toShow);

    }
}

