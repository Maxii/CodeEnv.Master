// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: HoverHighlightManager.cs
// Item Manager for Hover Highlights.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Item Manager for Hover Highlights.
    /// </summary>
    public class HoverHighlightManager : AHighlightManager {

        public override bool IsHighlightShowing { get { return _hoverHighlight != null && _hoverHighlight.IsShowing; } }

        private IWidgetTrackable _trackedClientItem;
        private float _highlightRadius;
        private ISphericalHighlight _hoverHighlight;

        public HoverHighlightManager(IWidgetTrackable trackedClientItem, float highlightRadius) : base(trackedClientItem.transform) {
            _trackedClientItem = trackedClientItem;
            _highlightRadius = highlightRadius;
        }

        public override void Show(bool toShow) {
            if (GameReferences.HoverHighlight != null) {  // allows deactivation of the SphericalHighlight gameObject
                _hoverHighlight = GameReferences.HoverHighlight;
                if (toShow) {
                    if (IsHighlightShowing) {
                        D.Warn("{0} shouldn't still be showing over {1}. Fixing.", DebugName, _hoverHighlight.TargetName);
                        _hoverHighlight.Show(false);
                    }
                    _hoverHighlight.SetTarget(_trackedClientItem);
                    _hoverHighlight.Color = TempGameValues.HoveredHighlightColor;
                    _hoverHighlight.Alpha = 0.15F;  // Should follow color as color comes with its own alpha, typically 1.0
                    _hoverHighlight.SetRadius(_highlightRadius);
                    _hoverHighlight.Show(true);
                }
                else {
                    if (!IsHighlightShowing) {
                        D.Warn("{0} should be showing over {1}.", DebugName, _hoverHighlight.TargetName);
                    }
                    _hoverHighlight.Show(false);
                    _hoverHighlight = null;
                }
            }
        }

        public override void HandleClientDeath() {
            base.HandleClientDeath();
            // As this single highlight instance is used by all Items, we don't want 
            // to do anything else except stop it from showing which is handled by the base class
        }

        protected override void Cleanup() {
            // Nothing to do as the highlight is a MonoBehaviour which will destroy itself
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

