// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Yielders.cs
// Cached collection of WaitFor YieldInstructions that reduce garbage allocation.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Cached collection of WaitFor YieldInstructions that reduce garbage allocation.
    /// <see cref="http://forum.unity3d.com/threads/c-coroutine-waitforseconds-garbage-collection-tip.224878/"/>
    /// </summary>
    public static class Yielders {

        private static IDictionary<float, WaitForSeconds> _waitForSecsLookup = new Dictionary<float, WaitForSeconds>(FloatEqualityComparer.Default);

        private static WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();
        public static WaitForEndOfFrame WaitForEndOfFrame {
            get { return _waitForEndOfFrame; }
        }

        private static WaitForFixedUpdate _waitForFixedUpdate = new WaitForFixedUpdate();
        public static WaitForFixedUpdate WaitForFixedUpdate {
            get { return _waitForFixedUpdate; }
        }

        public static WaitForSeconds GetWaitForSeconds(float seconds) {
            WaitForSeconds wfs;
            if (!_waitForSecsLookup.TryGetValue(seconds, out wfs)) {
                _waitForSecsLookup.Add(seconds, wfs = new WaitForSeconds(seconds));
                //D.Log("{0} added {1} seconds to WaitForSecondsLookup.", typeof(Yielders).Name, seconds);
            }
            else {
                //D.Log("{0} found {1} seconds in WaitForSecondsLookup.", typeof(Yielders).Name, seconds);
            }
            return wfs;
        }

        #region GetWaitForHours Archive

        /***************************************************************************************
         * Can't cache WaitForHours as the target date that is generated is derived from the
         * current date when WaitForHours is created. I made the mistake of adding a 
         * RefreshTargetDate() method in WaitForHours, but that changes the target date for
         * ALL instances currently in use as the cached WaitForHours is a Reference Type.
         * Every time the same instance for a duration is reused (and refreshed) it changes
         * the target date for all current users of that instance, thereby causing the coroutine
         * using that instance to not return control until the new (and later) date is reached.
         ****************************************************************************************/

        //private static IDictionary<GameTimeDuration, WaitForHours> _waitForHoursLookup = new Dictionary<GameTimeDuration, WaitForHours>(100);

        //public static WaitForHours GetWaitForHours(float hours) {
        //    return GetWaitForHours(new GameTimeDuration(hours));
        //}

        //public static WaitForHours GetWaitForHours(GameTimeDuration duration) {
        //    WaitForHours w;
        //    if (!_waitForHoursLookup.TryGetValue(duration, out w)) {
        //        _waitForHoursLookup.Add(duration, w = new WaitForHours(duration));
        //        D.Log("{0}: {1} not yet cached. Caching.", typeof(Yielders).Name, w);
        //    }
        //    else {
        //        wfh.RefreshTargetDate(duration);
        //        D.Log("{0}: reusing cached {1}.", typeof(Yielders).Name, w);
        //    }
        //    return w;
        //}

        #endregion

    }
}

