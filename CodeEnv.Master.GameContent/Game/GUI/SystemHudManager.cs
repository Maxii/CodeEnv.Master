// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemHudManager.cs
// Manages the HUD for Systems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Manages the HUD for Systems.
    /// </summary>
    public class SystemHudManager : AHudManager {

        private StarReport _starReport;
        private PlanetoidReport[] _planetoidReports;
        private SystemPublisher _publisher;

        public SystemHudManager(SystemPublisher publisher)
            : base() {
            _publisher = publisher;
            AddContentToUpdate(UpdatableLabelContentID.CameraDistance);
        }

        protected override ALabelText GetLabelText() {
            return _publisher.GetLabelText(LabelID.CursorHud, _starReport, _planetoidReports);
        }

        public void Show(Vector3 position, StarReport starReport, PlanetoidReport[] planetoidReports) {
            _starReport = starReport;
            _planetoidReports = planetoidReports;
            ShowHud(true, position);
        }

        public void Hide() {
            ShowHud(false, default(Vector3));
        }

        protected override bool TryUpdateContent(LabelContentID contentID, out IColoredTextList content) {
            return _publisher.TryUpdateLabelTextContent(LabelID.CursorHud, contentID, _starReport, _planetoidReports, out content);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

