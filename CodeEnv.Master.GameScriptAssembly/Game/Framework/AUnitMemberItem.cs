// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitMemberItem.cs
// Abstract class for members of a Unit, aka UnitCmds and UnitElements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract class for members of a Unit, aka UnitCmds and UnitElements.
/// <remarks>9.27.17 Not yet used.</remarks>
/// </summary>
[Obsolete("Not currently used")]
public abstract class AUnitMemberItem : AMortalItemStateMachine, ISensorDetector, IFsmEventSubscriptionMgrClient {

    public event EventHandler isAvailableChanged;

    /// <summary>
    /// Indicates whether this Element or Command is available for a new assignment.
    /// <remarks>Typically, a unit member that is available is Idling.</remarks>
    /// </summary>
    private bool _isAvailable;
    public bool IsAvailable {
        get { return _isAvailable; }
        protected set {
            if (_isAvailable != value) {
                _isAvailable = value;
                IsAvailablePropChangedHandler();
            }
        }
    }

    /// <summary>
    /// Indicates whether this Element or Command is capable of attacking an enemy target.
    /// </summary>
    public abstract bool IsAttackCapable { get; }

    protected FsmEventSubscriptionManager FsmEventSubscriptionMgr { get; private set; }
    protected sealed override bool IsPaused { get { return _gameMgr.IsPaused; } }

    protected Job _repairJob;

    #region Initialization

    public override void FinalInitialize() {
        base.FinalInitialize();
        InitializeFsmEventSubscriptionMgr();
    }

    private void InitializeFsmEventSubscriptionMgr() {
        FsmEventSubscriptionMgr = new FsmEventSubscriptionManager(this);
    }

    #endregion

    #region Event and Property Change Handlers

    private void IsAvailablePropChangedHandler() {
        if (IsAvailable) {
            __ValidateCurrentOrderAndStateWhenAvailable();
        }
        OnIsAvailable();
    }

    private void OnIsAvailable() {
        if (isAvailableChanged != null) {
            isAvailableChanged(this, EventArgs.Empty);
        }
    }

    #endregion

    #region Orders Support Members

    protected abstract void ResetOrderAndState();

    #endregion

    #region State Machine Support Members

    protected abstract bool IsCurrentStateCalled { get; }

    #region FsmReturnHandler and Callback System

    /// <summary>
    /// Indicates whether an order outcome failure callback to direct superior is allowed.
    /// <remarks>Typically, an order outcome failure callback is allowed until the ExecuteXXXOrder_EnterState
    /// successfully finishes executing, aka it wasn't interrupted by an event.</remarks>
    /// <remarks>4.9.17 Used to filter which OrderOutcome callbacks to events (e.g. XXX_UponNewOrderReceived()) 
    /// should be allowed. Typically, a callback will not occur from an event once the order has 
    /// successfully finished executing.</remarks>
    /// </summary>
    protected bool _allowOrderFailureCallback = true;

    /// <summary>
    /// Stack of FsmReturnHandlers that are currently in use. 
    /// <remarks>Allows use of nested Call()ed states.</remarks>
    /// </summary>
    protected Stack<FsmReturnHandler> _activeFsmReturnHandlers = new Stack<FsmReturnHandler>();

    /// <summary>
    /// Removes the FsmReturnHandler from the top of _activeFsmReturnHandlers. 
    /// Throws an error if not on top.
    /// </summary>
    /// <param name="handlerToRemove">The handler to remove.</param>
    protected void RemoveReturnHandlerFromTopOfStack(FsmReturnHandler handlerToRemove) {
        var topHandler = _activeFsmReturnHandlers.Pop();
        D.AssertEqual(topHandler, handlerToRemove);
    }

    /// <summary>
    /// Gets the FsmReturnHandler for the current Call()ed state.
    /// Throws an error if the CurrentState is not a Call()ed state or if not found.
    /// </summary>
    /// <returns></returns>
    protected FsmReturnHandler GetCurrentCalledStateReturnHandler() {
        D.Assert(IsCurrentStateCalled);
        D.AssertException(_activeFsmReturnHandlers.Count != Constants.Zero);
        string currentStateName = CurrentState.ToString();
        var peekHandler = _activeFsmReturnHandlers.Peek();
        if (peekHandler.CalledStateName != currentStateName) {
            // 4.11.17 When an event occurs in the 1 frame delay between Call()ing a state and processing the results
            D.Warn("{0}: {1} is not correct for state {2}. Replacing.", DebugName, peekHandler.DebugName, currentStateName);
            RemoveReturnHandlerFromTopOfStack(peekHandler);
            return GetCurrentCalledStateReturnHandler();
        }
        return peekHandler;
    }

    /// <summary>
    /// Gets the FsmReturnHandler for the Call()ed state named <c>calledStateName</c>.
    /// Throws an error if not found.
    /// <remarks>TEMP version that allows use in CalledState_ExitState methods where CurrentState has already changed.</remarks>
    /// </summary>
    /// <param name="calledStateName">Name of the Call()ed state.</param>
    /// <returns></returns>
    protected FsmReturnHandler __GetCalledStateReturnHandlerFor(string calledStateName) {
        D.AssertException(_activeFsmReturnHandlers.Count != Constants.Zero);
        var peekHandler = _activeFsmReturnHandlers.Peek();
        if (peekHandler.CalledStateName != calledStateName) {
            // 4.11.17 This can occur in the 1 frame delay between Call()ing a state and processing the results
            D.Warn("{0}: {1} is not correct for state {2}. Replacing.", DebugName, peekHandler.DebugName, calledStateName);
            RemoveReturnHandlerFromTopOfStack(peekHandler);
            return __GetCalledStateReturnHandlerFor(calledStateName);
        }
        return peekHandler;
    }

    #endregion

    /// <summary>
    /// Validates the common starting values of a State that is Call()able.
    /// </summary>
    protected virtual void ValidateCommonCallableStateValues(string calledStateName) {
        D.AssertNotEqual(Constants.Zero, _activeFsmReturnHandlers.Count);
        _activeFsmReturnHandlers.Peek().__Validate(calledStateName);
    }

    /// <summary>
    /// Validates the common starting values of a State that is not Call()able.
    /// </summary>
    protected virtual void ValidateCommonNotCallableStateValues() {
        D.AssertEqual(Constants.Zero, _activeFsmReturnHandlers.Count);
    }

    protected void ReturnFromCalledStates() {
        while (IsCurrentStateCalled) {
            Return();
        }
        D.Assert(!IsCurrentStateCalled);
    }

    protected void KillRepairJob() {
        if (_repairJob != null) {
            _repairJob.Kill();
            _repairJob = null;
        }
    }

    protected sealed override void PreconfigureCurrentState() {
        base.PreconfigureCurrentState();
        UponPreconfigureState();
    }

    protected void Dead_ExitState() {
        LogEventWarning();
    }

    #region Relays

    protected void UponEffectSequenceFinished(EffectSequenceID effectSeqID) { RelayToCurrentState(effectSeqID); }

    /// <summary>
    /// Called prior to entering the Dead state, this method notifies the current
    /// state that the element is dying, allowing any current state housekeeping
    /// required before entering the Dead state.
    /// </summary>
    protected void UponDeath() { RelayToCurrentState(); }

    /// <summary>
    /// Called prior to the Owner changing, this method notifies the current
    /// state that the element is losing ownership, allowing any current state housekeeping
    /// required before the Owner is changed.
    /// </summary>
    protected void UponLosingOwnership() { RelayToCurrentState(); }

    protected void UponNewOrderReceived() { RelayToCurrentState(); }

    /// <summary>
    /// Called from the StateMachine just after a state
    /// change and just before state_EnterState() is called. When EnterState
    /// is a coroutine method (returns IEnumerator), the relayed version
    /// of this method provides an opportunity to configure the state
    /// before any other event relay methods can be called during the state.
    /// </summary>
    private void UponPreconfigureState() { RelayToCurrentState(); }

    /// <summary>
    /// Called when the current target being used by the State Machine dies.
    /// </summary>
    /// <param name="deadFsmTgt">The dead target.</param>
    private void UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) { RelayToCurrentState(deadFsmTgt); }

    private void UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) { RelayToCurrentState(fsmTgt); }

    private void UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) { RelayToCurrentState(fsmTgt); }

    #endregion

    #region Repair Support

    /// <summary>
    /// Assesses this Cmd's need for repair, returning <c>true</c> if immediate repairs are needed, <c>false</c> otherwise.
    /// <remarks>Abstract to simply remind of need for functionality.</remarks>
    /// </summary>
    /// <param name="healthThreshold">The health threshold.</param>
    /// <returns></returns>
    protected abstract bool AssessNeedForRepair(float healthThreshold);

    #endregion

    #endregion

    #region Debug

    protected abstract void __ValidateCurrentOrderAndStateWhenAvailable();

    #endregion

    #region IFsmEventSubscriptionMgrClient Members

    void IFsmEventSubscriptionMgrClient.HandleFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        D.Log(ShowDebugLog, "{0}'s access to info about FsmTgt {1} has changed.", DebugName, fsmTgt.DebugName);
        UponFsmTgtInfoAccessChgd(fsmTgt);
    }

    void IFsmEventSubscriptionMgrClient.HandleFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        UponFsmTgtOwnerChgd(fsmTgt);
    }

    void IFsmEventSubscriptionMgrClient.HandleFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        UponFsmTgtDeath(deadFsmTgt);
    }

    // FIXME: Can't use a protected abstract method when explicitly implementing an interface
    public abstract void HandleAwarenessChgd(IMortalItem_Ltd item);

    #endregion
}

