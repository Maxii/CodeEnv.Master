// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IGameInput.cs
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
    /// 
    /// </summary>
    public interface IGameInput {

        float GetScrollWheelMovement();

        bool IsDragging { get; set; }

        Vector2 GetDragDelta();

        // used by SelectionManager to clear the Selection when an unconsumed click occurs
        event Action<NguiMouseButton> onUnconsumedClick;

        // activates ViewMode in PlayerViews
        event Action<ViewModeKeys> onViewModeKeyPressed;

        void CheckForKeyActivity();

    }
}

