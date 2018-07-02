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
    /// <remarks>The provided fsmTgt value, if any, does not have to be the same value as the StateMachine's _fsmTgt.</remarks>
    /// </summary>
    public class FsmEventSubscriptionManager {

        public string DebugName { get { return "{0}.{1}".Inject(_client.DebugName, GetType().Name); } }

        private bool ShowDebugLog { get { return _client.ShowDebugLog; } }

        private IDictionary<FsmEventSubscriptionMode, IList<INavigableDestination>> _fsmTgtEventSubscriptionStatusLookup =
            new Dictionary<FsmEventSubscriptionMode, IList<INavigableDestination>>(EventSubscriptionModeEqualityComparer.Default) {
                { FsmEventSubscriptionMode.FsmTgtDeath, new List<INavigableDestination>() },
                { FsmEventSubscriptionMode.FsmTgtInfoAccessChg, new List<INavigableDestination>() },
                { FsmEventSubscriptionMode.FsmTgtOwnerChg, new List<INavigableDestination>() },
                { FsmEventSubscriptionMode.FsmTgtVacancyChg, new List<INavigableDestination>() }
            };

        private IDictionary<FsmEventSubscriptionMode, bool> _ownerAwareEventSubscriptionStatusLookup =
            new Dictionary<FsmEventSubscriptionMode, bool>(EventSubscriptionModeEqualityComparer.Default) {
                 {FsmEventSubscriptionMode.OwnerAwareChg_Fleet, false },
                { FsmEventSubscriptionMode.OwnerAwareChg_Ship, false },
                { FsmEventSubscriptionMode.OwnerAwareChg_Base, false },
                { FsmEventSubscriptionMode.OwnerAwareChg_Facility, false },
                { FsmEventSubscriptionMode.OwnerAware_Planet, false },
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
        /// <param name="target">The target to subscribe to that the mode requires, if any.</param>
        /// <returns></returns>
        public bool AttemptToSubscribeToFsmEvent(FsmEventSubscriptionMode mode, INavigableDestination target = null) {
            return AttemptFsmEventSubscriptionChg(mode, true, target);
        }

        /// <summary>
        /// Attempts to unsubscribe to an FsmEvent in the mode provided.
        /// Returns <c>true</c> if unsubscribed, <c>false</c> if not.
        /// <remarks>Issues a warning if attempting a duplicate unsubscribe action.</remarks>
        /// <remarks>4.17.17 Confirmed with Asserts that this warning system works.</remarks>
        /// </summary>
        /// <param name="subscriptionMode">The subscription mode.</param>
        /// <param name="target">The target from which to unsubscribe that the mode requires, if any.</param>
        /// <returns></returns>
        public bool AttemptToUnsubscribeToFsmEvent(FsmEventSubscriptionMode mode, INavigableDestination target = null) {
            return AttemptFsmEventSubscriptionChg(mode, false, target);
        }

        /// <summary>
        /// Attempts subscribing or unsubscribing in the mode provided.
        /// Returns <c>true</c> if the indicated subscribe action was taken, <c>false</c> if not.
        /// <remarks>Issues a warning if attempting a duplicate subscribe action.</remarks>
        /// <remarks>4.17.17 Confirmed with Asserts that this warning system works.</remarks>
        /// </summary>
        /// <param name="subscriptionMode">The subscription mode.</param>
        /// <param name="toSubscribe">if set to <c>true</c> subscribe, otherwise unsubscribe.</param>
        /// <param name="target">The target to execute the subscription action on that the mode requires, if any.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private bool AttemptFsmEventSubscriptionChg(FsmEventSubscriptionMode subscriptionMode, bool toSubscribe, INavigableDestination target) {
            bool isSubscribeActionTaken = false;
            bool isDuplicateSubscriptionAttempted = false;
            IOwnerItem_Ltd itemFsmTgt = null;
            bool isSubscribed = GetSubscriptionStatus(subscriptionMode, target);
            switch (subscriptionMode) {
                case FsmEventSubscriptionMode.FsmTgtDeath:
                    D.AssertNotNull(target);
                    var mortalFsmTgt = target as IMortalItem_Ltd;
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
                    D.AssertNotNull(target);
                    itemFsmTgt = target as IOwnerItem_Ltd;
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
                    D.AssertNotNull(target);
                    itemFsmTgt = target as IOwnerItem_Ltd;
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
                case FsmEventSubscriptionMode.FsmTgtVacancyChg:
                    ISector_Ltd sector = target as ISector_Ltd;
                    D.AssertNotNull(sector);
                    if (!toSubscribe) {
                        sector.stationVacancyChgd -= SectorStationVacancyChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else if (!isSubscribed) {
                        sector.stationVacancyChgd += SectorStationVacancyChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else {
                        isDuplicateSubscriptionAttempted = true;
                    }
                    break;
                case FsmEventSubscriptionMode.OwnerAwareChg_Fleet:
                    D.AssertNull(target);
                    if (!toSubscribe) {
                        _client.OwnerAiMgr.awareChgd_Fleet -= AwarenessChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else if (!isSubscribed) {
                        _client.OwnerAiMgr.awareChgd_Fleet += AwarenessChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else {
                        isDuplicateSubscriptionAttempted = true;
                    }
                    break;
                case FsmEventSubscriptionMode.OwnerAwareChg_Ship:
                    D.AssertNull(target);
                    if (!toSubscribe) {
                        _client.OwnerAiMgr.awareChgd_Ship -= AwarenessChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else if (!isSubscribed) {
                        _client.OwnerAiMgr.awareChgd_Ship += AwarenessChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else {
                        isDuplicateSubscriptionAttempted = true;
                    }
                    break;
                case FsmEventSubscriptionMode.OwnerAwareChg_Base:
                    D.AssertNull(target);
                    if (!toSubscribe) {
                        _client.OwnerAiMgr.awareChgd_Base -= AwarenessChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else if (!isSubscribed) {
                        _client.OwnerAiMgr.awareChgd_Base += AwarenessChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else {
                        isDuplicateSubscriptionAttempted = true;
                    }
                    break;
                case FsmEventSubscriptionMode.OwnerAwareChg_Facility:
                    D.AssertNull(target);
                    if (!toSubscribe) {
                        _client.OwnerAiMgr.awareChgd_Facility -= AwarenessChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else if (!isSubscribed) {
                        _client.OwnerAiMgr.awareChgd_Facility += AwarenessChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else {
                        isDuplicateSubscriptionAttempted = true;
                    }
                    break;
                case FsmEventSubscriptionMode.OwnerAware_Planet:
                    D.AssertNull(target);
                    if (!toSubscribe) {
                        _client.OwnerAiMgr.awareChgd_Planet -= AwarenessChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else if (!isSubscribed) {
                        _client.OwnerAiMgr.awareChgd_Planet += AwarenessChgdEventHandler;
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
                ChangeSubscriptionStatus(subscriptionMode, toSubscribe, target);
            }
            return isSubscribeActionTaken;
        }

        private bool GetSubscriptionStatus(FsmEventSubscriptionMode mode, INavigableDestination target) {
            if (target != null) {
                return _fsmTgtEventSubscriptionStatusLookup[mode].Contains(target);
            }
            return _ownerAwareEventSubscriptionStatusLookup[mode];
        }

        private void ChangeSubscriptionStatus(FsmEventSubscriptionMode mode, bool toSubscribe, INavigableDestination target) {
            if (target != null) {
                if (toSubscribe) {
                    var subscribedTgts = _fsmTgtEventSubscriptionStatusLookup[mode];
                    D.Assert(!subscribedTgts.Contains(target));
                    subscribedTgts.Add(target);
                }
                else {
                    bool isRemoved = _fsmTgtEventSubscriptionStatusLookup[mode].Remove(target);
                    D.Assert(isRemoved);
                }
            }
            else {
                _ownerAwareEventSubscriptionStatusLookup[mode] = toSubscribe;
            }
        }

        #region Event and Property Change Handlers

        private void FsmTargetDeathEventHandler(object sender, EventArgs e) {
            IMortalItem_Ltd deadTgt = sender as IMortalItem_Ltd;
            _client.HandleFsmTgtDeath(deadTgt);
        }

        private void FsmTgtInfoAccessChgdEventHandler(object sender, InfoAccessChangedEventArgs e) {
            if (_client.Owner == e.Player) {
                IOwnerItem_Ltd target = sender as IOwnerItem_Ltd;
                _client.HandleFsmTgtInfoAccessChgd(target);
            }
        }

        private void FsmTgtOwnerChgdEventHandler(object sender, EventArgs e) {
            IOwnerItem_Ltd target = sender as IOwnerItem_Ltd;
            _client.HandleFsmTgtOwnerChgd(target);
        }

        private void SectorStationVacancyChgdEventHandler(object sender, SectorStarbaseStationVacancyEventArgs e) {
            _client.HandleSectorStationVacancyChgd(e.Station, e.IsVacant);
        }

        private void AwarenessChgdEventHandler(object sender, PlayerAIManager.AwareChgdEventArgs e) {
            _client.HandleAwarenessChgd(e.Item);
        }

        #endregion

        public override string ToString() {
            return DebugName;
        }

        #region Debug

        [Obsolete("Update to new approach if you want to use")]
        public void __LogSubscriptionStatus() { }

        public void __ValidateNoRemainingSubscriptions() {
            D.Assert(_fsmTgtEventSubscriptionStatusLookup.Values.All(subscribedTgts => !subscribedTgts.Any()));
            D.Assert(_ownerAwareEventSubscriptionStatusLookup.Values.All(subStatus => subStatus == false));
        }

        #endregion

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

