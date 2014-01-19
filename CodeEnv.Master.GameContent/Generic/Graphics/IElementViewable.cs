// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IElementViewable.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Interface used by a ElementPresenter to communicate with their associated ElementView.
    /// </summary>
    public interface IElementViewable : IViewable {

        event Action onShowCompletion;

        void ShowAttacking();

        void ShowHit();

        void ShowDying();

        void ShowRepairing();

        void ShowRefitting();

        void StopShowing();

    }
}

