// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DateMinderDuration.cs
// Container holding a Client interface and a GameTimeDuration for use by DateMinder and/or RecurringDateMinder.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Container holding a Client interface and a GameTimeDuration for use by DateMinder and/or RecurringDateMinder.
    /// <remarks>Important that this is a class (Reference equality) as the uniqueness of each instance
    /// is key to its use.</remarks>
    /// </summary>
    public class DateMinderDuration {

        private const string DebugNameFormat = "{0}[{1}, {2}]";
        private static int IdCount = 0;

        private int _uniqueID;

        public string DebugName { get { return DebugNameFormat.Inject(Client.DebugName, Duration, _uniqueID); } }

        public GameTimeDuration Duration { get; private set; }

        public IRecurringDateMinderClient Client { get; private set; }

        public DateMinderDuration(GameTimeDuration duration, IRecurringDateMinderClient client) {
            Duration = duration;
            Client = client;
            _uniqueID = IdCount;
            IdCount++;
        }

        public override string ToString() {
            return DebugName;
        }

        #region Reference version Archive

        /// <summary>
        /// Indicates whether [Recurring]DateMinder needs to update the date being used for this Duration. 
        /// <remarks>This becomes <c>true</c> after Duration is accessed and found that its Reference
        /// value has changed. The Duration returned will be the changed value. After DateMinder updates the 
        /// date, it should set manually set this value to false.</remarks>
        /// </summary>
        //public bool IsDateUpdateRequired { get; set; }

        //private GameTimeDuration _duration;
        //public GameTimeDuration Duration {
        //    get {
        //        if (_doesDurationRefNeedChecking) {
        //            if (_duration != _durationRef.Value) {
        //                _duration = _durationRef.Value;
        //                IsDateUpdateRequired = true;
        //            }
        //        }
        //        return _duration;
        //    }
        //}

        //private bool _doesDurationRefNeedChecking;
        //private Reference<GameTimeDuration> _durationRef;

        //public DateMinderDuration(GameTimeDuration duration, IRecurringDateMinderClient client) {
        //    _duration = duration;
        //    Client = client;
        //}

        //public DateMinderDuration(Reference<GameTimeDuration> durationRef, IRecurringDateMinderClient client)
        //    : this(durationRef.Value, client) {
        //    _durationRef = durationRef;
        //    _doesDurationRefNeedChecking = true;
        //}

        #endregion

    }
}

