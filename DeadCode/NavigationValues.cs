// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NavigationValues.cs
// Wrapper holding values required by ICanNavigate Items (Ships and FleetCmds) to navigate to an INavigableTarget.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Wrapper holding values required by ICanNavigate Items (Ships and FleetCmds) to navigate to an INavigableTarget.
    /// </summary>
    [System.Obsolete]
    public class NavigationValues {

        /// <summary>
        /// The distance from the INavigableTarget "Target" that is "close enough" 
        /// for the ICanNavigate item (fleet or ship) to be considered "arrived".
        /// </summary>
        public float CloseEnoughDistance {
            get {
                if (_closeEnoughDistanceRef != null) {
                    return _closeEnoughDistanceRef.Value;
                }
                return _closeEnoughDistance;
            }
        }

        /// <summary>
        /// The intended distance the ICanNavigate item (fleet or ship)
        /// travels between course progress checks. The ICanNavigate item's
        /// ProgressCheckPeriod is derived from this distance.
        /// </summary>
        public float ProgressCheckDistance {
            get {
                if (_progressCheckDistanceRef != null) {
                    return _progressCheckDistanceRef.Value;
                }
                return _progressCheckDistance;
            }
        }

        private Reference<float> _closeEnoughDistanceRef;
        private Reference<float> _progressCheckDistanceRef;
        private float _closeEnoughDistance;
        private float _progressCheckDistance;

        public NavigationValues(float closeEnoughDistance, float progressCheckDistance) {
            _closeEnoughDistance = closeEnoughDistance;
            _progressCheckDistance = progressCheckDistance;
        }

        public NavigationValues(Reference<float> closeEnoughDistanceRef, Reference<float> progressCheckDistanceRef) {
            _closeEnoughDistanceRef = closeEnoughDistanceRef;
            _progressCheckDistanceRef = progressCheckDistanceRef;
        }

        public NavigationValues(Reference<float> closeEnoughDistanceRef, float progressCheckDistance) {
            _closeEnoughDistanceRef = closeEnoughDistanceRef;
            _progressCheckDistance = progressCheckDistance;
        }

        public NavigationValues(float closeEnoughDistance, Reference<float> progressCheckDistanceRef) {
            _closeEnoughDistance = closeEnoughDistance;
            _progressCheckDistanceRef = progressCheckDistanceRef;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }


    }
}

