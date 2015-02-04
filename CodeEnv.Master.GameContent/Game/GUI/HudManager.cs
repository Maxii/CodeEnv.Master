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
    /// <typeparam name="PublisherType">The type of Publisher.</typeparam>
    public class HudManager<PublisherType> : AHudManager where PublisherType : APublisherBase {

        private PublisherType _publisher;

        public HudManager(PublisherType publisher)
            : base() {
            _publisher = publisher;
            AssignContentToUpdate(UpdatableLabelContentID.CameraDistance, UpdatableLabelContentID.IntelState);
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

        protected override IColoredTextList UpdateContent(LabelContentID contentID) {
            return _publisher.UpdateContent(LabelID.CursorHud, contentID);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

