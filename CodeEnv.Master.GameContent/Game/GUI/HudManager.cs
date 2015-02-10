// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: HudManager.cs
// Generic class that manages the HUD for each item.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Generic class that manages the HUD for each item.
    /// </summary>
    public class HudManager : AHudManager {

        private APublisher _publisher;

        public HudManager(APublisher publisher)
            : base() {
            _publisher = publisher;
            AddContentToUpdate(UpdatableLabelContentID.CameraDistance);
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

