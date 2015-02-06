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

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Generic class that manages the HUD for each command.
    /// </summary>
    public class CmdHudManager<CmdPublisherType> : AHudManager where CmdPublisherType : ACmdPublisherBase {

        private AElementItemReport[] _elementReports;
        private CmdPublisherType _publisher;

        public CmdHudManager(CmdPublisherType publisher)
            : base() {
            _publisher = publisher;
            AddContentToUpdate(UpdatableLabelContentID.CameraDistance, UpdatableLabelContentID.IntelState);
        }

        protected override ALabelText GetLabelText() {
            return _publisher.GetLabelText(LabelID.CursorHud, _elementReports);
        }

        public void Show(Vector3 position, AElementItemReport[] elementReports) {
            _elementReports = elementReports;
            ShowHud(true, position);
        }

        public void Hide() {
            ShowHud(false, default(Vector3));
        }

        protected override bool TryUpdateContent(LabelContentID contentID, out IColoredTextList content) {
            return _publisher.TryUpdateLabelTextContent(LabelID.CursorHud, contentID, _elementReports, out content);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

