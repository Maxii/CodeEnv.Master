﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipHumanPresenter.cs
// An MVPresenter associated with a ShipHumanView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// An MVPresenter associated with a ShipHumanView.
    /// </summary>
    public class ShipHumanPresenter : ShipPresenter {

        public ShipHumanPresenter(IElementViewable view)
            : base(view) { }

        public void RequestContextMenu(bool isDown) {
            _cameraControl.ShowContextMenuOnPress(isDown);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

