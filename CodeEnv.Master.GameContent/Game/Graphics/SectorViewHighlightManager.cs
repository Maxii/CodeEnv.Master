﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorViewHighlightManager.cs
// Item Manager for SectorView Highlights, aka highlights the contents of a sector.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Item Manager for SectorView Highlights, aka highlights the contents of a sector.
    /// </summary>
    public class SectorViewHighlightManager : AHighlightManager {

        public override bool IsHighlightShowing { get { return _sectorViewHighlight != null && _sectorViewHighlight.IsShowing; } }

        private ISphericalHighlight _sectorViewHighlight;
        private IWidgetTrackable _trackedClientItem;
        private float _highlightRadius;

        public SectorViewHighlightManager(IWidgetTrackable trackedClientItem, float highlightRadius) : base(trackedClientItem.transform) {
            _trackedClientItem = trackedClientItem;
            _highlightRadius = highlightRadius;
        }

        public override void Show(bool toShow) {
            //D.Log("{0}.Show({1}) called.", DebugName, toShow);
            if (toShow) {
                D.Assert(!IsHighlightShowing, DebugName);
                _sectorViewHighlight = GameReferences.GamePoolManager.SpawnHighlight(_trackedClientTransform.position);
                _sectorViewHighlight.SetTarget(_trackedClientItem);
                _sectorViewHighlight.Color = TempGameValues.SectorHighlightColor;
                _sectorViewHighlight.Alpha = 0.4F;  // Should follow color as color comes with its own alpha, typically 1.0
                _sectorViewHighlight.SetRadius(_highlightRadius);
                _sectorViewHighlight.Show(true);
            }
            else {
                D.Assert(IsHighlightShowing, DebugName);
                _sectorViewHighlight.Show(false);
                GameReferences.GamePoolManager.DespawnHighlight(_sectorViewHighlight.transform);
                _sectorViewHighlight = null;
            }
        }

        public override void HandleClientDeath() {
            base.HandleClientDeath();
            // As these highlight instances are pooled, we don't want to do anything else except 
            // stop it from showing which is handled by the base class. This action despawns the instance.
        }

        protected override void Cleanup() {
            // Nothing to do as the highlight is a MonoBehaviour which will destroy itself
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

