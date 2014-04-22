// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCmdHumanPresenter.cs
//  An MVPresenter associated with a SettlementCmdHumanView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    ///  An MVPresenter associated with a SettlementCmdHumanView.
    /// </summary>
    public class SettlementCmdHumanPresenter : SettlementCmdPresenter {

        public SettlementCmdHumanPresenter(ICommandViewable view)
            : base(view) { }

        public void RequestContextMenu(bool isDown) {
            _cameraControl.ShowContextMenuOnPress(isDown);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

