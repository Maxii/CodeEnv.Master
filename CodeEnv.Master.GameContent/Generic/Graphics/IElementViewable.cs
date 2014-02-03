// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IElementViewable.cs
//  Interface used by a ElementPresenter to communicate with their associated ElementView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface used by a ElementPresenter to communicate with their associated ElementView.
    /// </summary>
    public interface IElementViewable : IMortalViewable {

        void AssessHighlighting();

        void ShowAttacking();

        void ShowRepairing();

        void ShowRefitting();

        void StopShowing();

    }
}

