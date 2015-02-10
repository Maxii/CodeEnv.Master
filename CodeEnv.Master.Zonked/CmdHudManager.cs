// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CmdHudManager.cs
// Generic class that manages the HUD for each command.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Generic class that manages the HUD for each command.
    /// </summary>
    [Obsolete]
    public class CmdHudManager : AHudManager {

        private APublisher _publisher;

        public CmdHudManager(APublisher publisher)
            : base() {
            _publisher = publisher;
            AddContentToUpdate(UpdatableLabelContentID.CameraDistance, UpdatableLabelContentID.IntelState);
        }

        protected override ALabelText GetLabelText() {
            return _publisher.GetLabelText(LabelID.CursorHud);
        }

        public void Show(Vector3 position) {
            ShowHud(true, position);
        }

        public void Hide() {
            ShowHud(false, default(Vector3));
        }

        protected override bool TryUpdateContent(LabelContentID contentID, out IColoredTextList content) {
            return _publisher.TryUpdateLabelTextContent(LabelID.CursorHud, contentID, out content);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

