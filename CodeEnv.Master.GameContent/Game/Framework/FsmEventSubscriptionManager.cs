// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FsmEventSubscriptionManager.cs
// Event Subscription Manager for a UnitItem's state machine.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Event Subscription Manager for a UnitItem's state machine.
    /// </summary>
    public class FsmEventSubscriptionManager {

        public string DebugName { get { return "{0}.{1}".Inject(_client.DebugName, GetType().Name); } }

        private bool ShowDebugLog { get { return _client.ShowDebugLog; } }

        private IDictionary<FsmEventSubscriptionMode, bool> _eventSubscriptionStatusLookup =
            new Dictionary<FsmEventSubscriptionMode, bool>(EventSubscriptionModeEqualityComparer.Default) {
                {FsmEventSubscriptionMode.FsmTgtDeath, false },
                {FsmEventSubscriptionMode.FsmTgtInfoAccessChg, false },
                {FsmEventSubscriptionMode.FsmTgtOwnerChg, false },

                {FsmEventSubscriptionMode.OwnerAwareChg_Fleet, false },
                {FsmEventSubscriptionMode.OwnerAwareChg_Ship, false },
                {FsmEventSubscriptionMode.OwnerAwareChg_Base, false },
                {FsmEventSubscriptionMode.OwnerAwareChg_Facility, false },

                {FsmEventSubscriptionMode.OwnerAware_Planet, false },
            };

        private IFsmEventSubscriptionMgrClient _client;

        public FsmEventSubscriptionManager(IFsmEventSubscriptionMgrClient client) {
            _client = client;
        }

        /// <summary>
        /// Attempts to subscribe to an FsmEvent in the mode provided.
        /// Returns <c>true</c> if a subscription was made, <c>false</c> if not.
        /// <remarks>Issues a warning if attempting a duplicate subscribe action.</remarks>
        /// <remarks>4.17.17 Confirmed with Asserts that this warning system works.</remarks>
        /// </summary>
        /// <param name="subscriptionMode">The subscription mode.</param>
        /// <param name="fsmTgt">The target to subscribe to that the mode requires, if any.</param>
        /// <returns></returns>
        public bool AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode mode, INavigableDestination fsmTgt = null) {
            return AttemptFsmEventSubscriptionChg(mode, true, fsmTgt);
        }

        /// <summary>
        /// Attempts to unsubscribe to an FsmEvent in the mode provided.
        /// Returns <c>true</c> if unsubscribed, <c>false</c> if not.
        /// <remarks>Issues a warning if attempting a duplicate unsubscribe action.</remarks>
        /// <remarks>4.17.17 Confirmed with Asserts that this warning system works.</remarks>
        /// </summary>
        /// <param name="subscriptionMode">The subscription mode.</param>
        /// <param name="fsmTgt">The target from which to unsubscribe that the mode requires, if any.</param>
        /// <returns></returns>
        public bool AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode mode, INavigableDestination fsmTgt = null) {
            return AttemptFsmEventSubscriptionChg(mode, false, fsmTgt);
        }

        /// <summary>
        /// Attempts subscribing or unsubscribing in the mode provided.
        /// Returns <c>true</c> if the indicated subscribe action was taken, <c>false</c> if not.
        /// <remarks>Issues a warning if attempting a duplicate subscribe action.</remarks>
        /// <remarks>4.17.17 Confirmed with Asserts that this warning system works.</remarks>
        /// </summary>
        /// <param name="subscriptionMode">The subscription mode.</param>
        /// <param name="toSubscribe">if set to <c>true</c> subscribe, otherwise unsubscribe.</param>
        /// <param name="fsmTgt">The target to execute the subscription action on that the mode requires, if any.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private bool AttemptFsmEventSubscriptionChg(FsmEventSubscriptionMode subscriptionMode, bool toSubscribe, INavigableDestination fsmTgt = null) {
            bool isSubscribeActionTaken = false;
            bool isDuplicateSubscriptionAttempted = false;
            IOwnerItem_Ltd itemFsmTgt = null;
            bool isSubscribed = _eventSubscriptionStatusLookup[subscriptionMode];
            switch (subscriptionMode) {
                case FsmEventSubscriptionMode.FsmTgtDeath:
                    D.AssertNotNull(fsmTgt);
                    var mortalFsmTgt = fsmTgt as IMortalItem_Ltd;
                    if (mortalFsmTgt != null) {
                        if (!toSubscribe) {
                            mortalFsmTgt.deathOneShot -= FsmTargetDeathEventHandler;
                            isSubscribeActionTaken = true;
                        }
                        else if (!isSubscribed) {
                            mortalFsmTgt.deathOneShot += FsmTargetDeathEventHandler;
                            isSubscribeActionTaken = true;
                        }
                        else {
                            isDuplicateSubscriptionAttempted = true;
                        }
                    }
                    break;
                case FsmEventSubscriptionMode.FsmTgtInfoAccessChg:
                    D.AssertNotNull(fsmTgt);
                    itemFsmTgt = fsmTgt as IOwnerItem_Ltd;
                    if (itemFsmTgt != null) {    // fsmTgt can be a StationaryLocation
                        if (!toSubscribe) {
                            itemFsmTgt.infoAccessChgd -= FsmTgtInfoAccessChgdEventHandler;
                            isSubscribeActionTaken = true;
                        }
                        else if (!isSubscribed) {
                            itemFsmTgt.infoAccessChgd += FsmTgtInfoAccessChgdEventHandler;
                            isSubscribeActionTaken = true;
                        }
                        else {
                            isDuplicateSubscriptionAttempted = true;
                        }
                    }
                    break;
                case FsmEventSubscriptionMode.FsmTgtOwnerChg:
                    D.AssertNotNull(fsmTgt);
                    itemFsmTgt = fsmTgt as IOwnerItem_Ltd;
                    if (itemFsmTgt != null) {    // fsmTgt can be a StationaryLocation
                        if (!toSubscribe) {
                            itemFsmTgt.ownerChanged -= FsmTgtOwnerChgdEventHandler;
                            isSubscribeActionTaken = true;
                        }
                        else if (!isSubscribed) {
                            itemFsmTgt.ownerChanged += FsmTgtOwnerChgdEventHandler;
                            isSubscribeActionTaken = true;
                        }
                        else {
                            isDuplicateSubscriptionAttempted = true;
                        }
                    }
                    break;
                case FsmEventSubscriptionMode.OwnerAwareChg_Fleet:
                    D.AssertNull(fsmTgt);
                    if (!toSubscribe) {
                        _client.OwnerAIMgr.awareChgd_Fleet -= AwarenessChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else if (!isSubscribed) {
                        _client.OwnerAIMgr.awareChgd_Fleet += AwarenessChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else {
                        isDuplicateSubscriptionAttempted = true;
                    }
                    break;
                case FsmEventSubscriptionMode.OwnerAwareChg_Ship:
                    D.AssertNull(fsmTgt);
                    if (!toSubscribe) {
                        _client.OwnerAIMgr.awareChgd_Ship -= AwarenessChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else if (!isSubscribed) {
                        _client.OwnerAIMgr.awareChgd_Ship += AwarenessChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else {
                        isDuplicateSubscriptionAttempted = true;
                    }
                    break;
                case FsmEventSubscriptionMode.OwnerAwareChg_Base:
                    D.AssertNull(fsmTgt);
                    if (!toSubscribe) {
                        _client.OwnerAIMgr.awareChgd_Base -= AwarenessChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else if (!isSubscribed) {
                        _client.OwnerAIMgr.awareChgd_Base += AwarenessChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else {
                        isDuplicateSubscriptionAttempted = true;
                    }
                    break;
                case FsmEventSubscriptionMode.OwnerAwareChg_Facility:
                    D.AssertNull(fsmTgt);
                    if (!toSubscribe) {
                        _client.OwnerAIMgr.awareChgd_Facility -= AwarenessChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else if (!isSubscribed) {
                        _client.OwnerAIMgr.awareChgd_Facility += AwarenessChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else {
                        isDuplicateSubscriptionAttempted = true;
                    }
                    break;
                case FsmEventSubscriptionMode.OwnerAware_Planet:
                    D.AssertNull(fsmTgt);
                    if (!toSubscribe) {
                        _client.OwnerAIMgr.awareChgd_Planet -= AwarenessChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else if (!isSubscribed) {
                        _client.OwnerAIMgr.awareChgd_Planet += AwarenessChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else {
                        isDuplicateSubscriptionAttempted = true;
                    }
                    break;
                case FsmEventSubscriptionMode.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(subscriptionMode));
            }
            if (isDuplicateSubscriptionAttempted) {
                D.Warn("{0}: Attempting to subscribe using {1} when already subscribed.", DebugName, subscriptionMode.GetValueName());
            }
            if (isSubscribeActionTaken) {
                _eventSubscriptionStatusLookup[subscriptionMode] = toSubscribe;
            }
            return isSubscribeActionTaken;
        }

        #region Event and Property Change Handlers

        private void FsmTargetDeathEventHandler(object sender, EventArgs e) {
            IMortalItem_Ltd deadFsmTgt = sender as IMortalItem_Ltd;
            _client.HandleFsmTgtDeath(deadFsmTgt);
        }

        private void FsmTgtInfoAccessChgdEventHandler(object sender, InfoAccessChangedEventArgs e) {
            if (_client.Owner == e.Player) {
                IOwnerItem_Ltd fsmTgt = sender as IOwnerItem_Ltd;
                _client.HandleFsmTgtInfoAccessChgd(fsmTgt);
            }
        }

        private void FsmTgtOwnerChgdEventHandler(object sender, EventArgs e) {
            IOwnerItem_Ltd fsmTgt = sender as IOwnerItem_Ltd;
            _client.HandleFsmTgtOwnerChgd(fsmTgt);
        }

        private void AwarenessChgdEventHandler(object sender, PlayerAIManager.AwareChgdEventArgs e) {
            _client.HandleAwarenessChgd(e.Item);
        }

        #endregion

        #region Debug

        public void __LogSubscriptionStatus() {
            D.Log("{0}: SubscriptionMode: {1}.", DebugName, _eventSubscriptionStatusLookup.Keys.Select(mode => mode.GetValueName()).Concatenate());
            D.Log("{0}: SubscriptionStatus: {1}.", DebugName, _eventSubscriptionStatusLookup.Values.Concatenate());
        }

        #endregion

        public override string ToString() {
            return DebugName;
        }

        #region Nested Classes

        /// <summary>
        /// IEqualityComparer for EventSubscriptionMode. 
        /// <remarks>For use when EventSubscriptionMode is used as a Dictionary key as it avoids boxing from use of object.Equals.</remarks>
        /// </summary>
        protected class EventSubscriptionModeEqualityComparer : IEqualityComparer<FsmEventSubscriptionMode> {

            public static readonly EventSubscriptionModeEqualityComparer Default = new EventSubscriptionModeEqualityComparer();

            #region IEqualityComparer<EventSubscriptionMode> Members

            public bool Equals(FsmEventSubscriptionMode value1, FsmEventSubscriptionMode value2) {
                return value1 == value2;
            }

            public int GetHashCode(FsmEventSubscriptionMode value) {
                return value.GetHashCode();
            }

            #endregion

        }

        // IDisposable: Can't Dispose() with no references to subscriptions. Clients must properly exit states to unsubscribe

        #endregion

    }
}

