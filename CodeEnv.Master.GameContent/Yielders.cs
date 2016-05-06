// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Yielders.cs
// Cached collection of WaitFor YieldInstructions that reduces garbage allocation.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Cached collection of WaitFor YieldInstructions that reduces garbage allocation.
    /// <see cref="http://forum.unity3d.com/threads/c-coroutine-waitforseconds-garbage-collection-tip.224878/"/>
    /// </summary>
    public static class Yielders {

        private static IDictionary<float, WaitForSeconds> _waitForSecsLookup = new Dictionary<float, WaitForSeconds>(new FloatEqualityComparer());

        //private static IDictionary<float, WaitForHours> _waitForHoursLookup = new Dictionary<float, WaitForHours>(100, new FloatEqualityComparer());
        private static IDictionary<GameTimeDuration, WaitForHours> _waitForHoursLookup = new Dictionary<GameTimeDuration, WaitForHours>(100);

        private static WaitForEndOfFrame _endOfFrame = new WaitForEndOfFrame();
        public static WaitForEndOfFrame EndOfFrame {
            get { return _endOfFrame; }
        }

        private static WaitForFixedUpdate _fixedUpdate = new WaitForFixedUpdate();
        public static WaitForFixedUpdate FixedUpdate {
            get { return _fixedUpdate; }
        }

        public static WaitForSeconds GetWaitForSeconds(float seconds) {
            WaitForSeconds wfs;
            if (!_waitForSecsLookup.TryGetValue(seconds, out wfs)) {
                _waitForSecsLookup.Add(seconds, wfs = new WaitForSeconds(seconds));
            }
            return wfs;
        }

        //public static WaitForHours GetWaitForHours(float hours) {
        //    WaitForHours wfh;
        //    if (!_waitForHoursLookup.TryGetValue(hours, out wfh)) {
        //        D.Log("{0}: WaitForHours({1:0.00}) not yet cached. Caching.", typeof(Yielders).Name, hours);
        //        _waitForHoursLookup.Add(hours, wfh = new WaitForHours(hours));
        //    }
        //    else {
        //        D.Log("{0}: reusing cached WaitForHours({1:0.00}).", typeof(Yielders).Name, hours);
        //    }
        //    wfh.RefreshTargetDate();
        //    return wfh;
        //}
        public static WaitForHours GetWaitForHours(float hours) {
            return GetWaitForHours(new GameTimeDuration(hours));
        }

        public static WaitForHours GetWaitForHours(GameTimeDuration duration) {
            WaitForHours wfh;
            if (!_waitForHoursLookup.TryGetValue(duration, out wfh)) {
                //D.Log("{0}: WaitForHours({1:0.00}) not yet cached. Caching.", typeof(Yielders).Name, duration);
                _waitForHoursLookup.Add(duration, wfh = new WaitForHours(duration));
            }
            else {
                //D.Log("{0}: reusing cached WaitForHours({1:0.00}).", typeof(Yielders).Name, duration);
            }
            wfh.RefreshTargetDate();
            return wfh;
        }


    }
}

