// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnifiedSRSensorMonitor.cs
// UnitCmd's SRSensor Monitor that unifies the results of the SRSensorMonitor of all elements.
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
    using UnityEngine;

    /// <summary>
    /// UnitCmd's SRSensor Monitor that unifies the results of the SRSensorMonitor of all elements.
    /// </summary>
    public class UnifiedSRSensorMonitor : IDebugable, IDisposable {

        private const string DebugNameFormat = "{0}.{1}";

        /// <summary>
        /// Occurs when AreEnemyCmdsInRange changes. Only fires on a change
        /// in the property state, not when the qty of enemy cmds in range changes.
        /// </summary>
        public event EventHandler enemyCmdsInRangeChgd;

        /// <summary>
        /// Occurs when AreWarEnemyElementsInRange changes. Only fires on a change
        /// in the property state, not when the qty of war enemy elements in range changes.
        /// </summary>
        public event EventHandler warEnemyElementsInRangeChgd;

        private string _debugName;
        public string DebugName {
            get {
                _debugName = _debugName ?? DebugNameFormat.Inject(_parentCmd.DebugName, typeof(UnifiedSRSensorMonitor).Name);
                return _debugName;
            }
        }

        public bool ShowDebugLog { get { return _parentCmd.ShowDebugLog; } }

        /// <summary>
        /// Indicates whether there are any enemy UnitElements in range.
        /// <remarks>Not subscribable.</remarks>
        /// </summary>
        public bool AreEnemyElementsInRange { get; private set; }

        /// <summary>
        /// Indicates whether there are any enemy UnitCmds in range.
        /// <remarks>Not subscribable.</remarks>
        /// </summary>
        public bool AreEnemyCmdsInRange { get; private set; }

        /// <summary>
        /// Indicates whether there are any enemy UnitElements in range where DiplomaticRelationship.War exists.
        /// <remarks>Not subscribable.</remarks>
        /// </summary>
        public bool AreWarEnemyElementsInRange { get; private set; }

        /// <summary>
        /// Indicates whether there are any enemy UnitCmds in range where DiplomaticRelationship.War exists.
        /// <remarks>Not subscribable.</remarks>
        /// </summary>
        public bool AreWarEnemyCmdsInRange { get; private set; }

        private HashSet<IUnitElement_Ltd> _enemyElementsDetected = new HashSet<IUnitElement_Ltd>();
        /// <summary>
        /// A copy of all the detected enemy UnitElements that are in range of the sensors of this monitor.
        /// <remarks>Can contain both ColdWar and War enemies.</remarks>
        /// </summary>
        public HashSet<IUnitElement_Ltd> EnemyElementsDetected {
            get { return GetSetCopy(_enemyElementsDetected); }
        }

        private HashSet<IUnitCmd_Ltd> _enemyCmdsDetected = new HashSet<IUnitCmd_Ltd>();
        /// <summary>
        /// A copy of all the detected enemy UnitCmds that are in range of the sensors of this monitor.
        /// <remarks>Can contain both ColdWar and War enemies.</remarks>
        /// <remarks>While a UnitCmd is not itself detectable, its HQElement is.</remarks>
        /// </summary>
        public HashSet<IUnitCmd_Ltd> EnemyCmdsDetected {
            get { return GetSetCopy(_enemyCmdsDetected); }
        }

        private HashSet<IUnitElement_Ltd> _warEnemyElementsDetected = new HashSet<IUnitElement_Ltd>();
        /// <summary>
        /// A copy of all the detected war enemy UnitElements that are in range of the sensors of this monitor.
        /// </summary>
        public HashSet<IUnitElement_Ltd> WarEnemyElementsDetected {
            get { return GetSetCopy(_warEnemyElementsDetected); }
        }

        private HashSet<IUnitCmd_Ltd> _warEnemyCmdsDetected = new HashSet<IUnitCmd_Ltd>();
        /// <summary>
        /// A copy of all the detected war enemy UnitCmds that are in range of the sensors of this monitor.
        /// <remarks>While a UnitCmd is not itself detectable, its HQElement is.</remarks>
        /// </summary>
        public HashSet<IUnitCmd_Ltd> WarEnemyCmdsDetected {
            get { return GetSetCopy(_warEnemyCmdsDetected); }
        }

        private IDictionary<IUnitElement_Ltd, HashSet<IElementSensorRangeMonitor>> _monitorsByEnemyElementLookup = new Dictionary<IUnitElement_Ltd, HashSet<IElementSensorRangeMonitor>>();
        private IDictionary<IUnitCmd_Ltd, HashSet<IElementSensorRangeMonitor>> _monitorsByWarEnemyCmdLookup = new Dictionary<IUnitCmd_Ltd, HashSet<IElementSensorRangeMonitor>>();
        private IDictionary<IUnitCmd_Ltd, HashSet<IElementSensorRangeMonitor>> _monitorsByEnemyCmdLookup = new Dictionary<IUnitCmd_Ltd, HashSet<IElementSensorRangeMonitor>>();
        private IDictionary<IUnitElement_Ltd, HashSet<IElementSensorRangeMonitor>> _monitorsByWarEnemyElementLookup = new Dictionary<IUnitElement_Ltd, HashSet<IElementSensorRangeMonitor>>();

        private IDictionary<IElementSensorRangeMonitor, HashSet<IUnitElement_Ltd>> _enemyElementsByMonitorLookup = new Dictionary<IElementSensorRangeMonitor, HashSet<IUnitElement_Ltd>>();
        private IDictionary<IElementSensorRangeMonitor, HashSet<IUnitElement_Ltd>> _warEnemyElementsByMonitorLookup = new Dictionary<IElementSensorRangeMonitor, HashSet<IUnitElement_Ltd>>();
        private IDictionary<IElementSensorRangeMonitor, HashSet<IUnitCmd_Ltd>> _enemyCmdsByMonitorLookup = new Dictionary<IElementSensorRangeMonitor, HashSet<IUnitCmd_Ltd>>();
        private IDictionary<IElementSensorRangeMonitor, HashSet<IUnitCmd_Ltd>> _warEnemyCmdsByMonitorLookup = new Dictionary<IElementSensorRangeMonitor, HashSet<IUnitCmd_Ltd>>();

        private IUnitCmd _parentCmd;

        public UnifiedSRSensorMonitor(IUnitCmd parentCmd) {
            _parentCmd = parentCmd;
            Subscribe();
        }

        private void Subscribe() {
            GameReferences.GameManager.sceneLoading += SceneLoadingEventHandler;
        }

        public void AddEnemyElement(IUnitElement_Ltd element, IElementSensorRangeMonitor monitor) {
            HashSet<IUnitElement_Ltd> elements;
            if (!_enemyElementsByMonitorLookup.TryGetValue(monitor, out elements)) {
                elements = GetEmptyElementSet();
                _enemyElementsByMonitorLookup.Add(monitor, elements);
            }
            bool isAdded = elements.Add(element);
            D.Assert(isAdded);

            HashSet<IElementSensorRangeMonitor> monitors;
            if (!_monitorsByEnemyElementLookup.TryGetValue(element, out monitors)) {
                monitors = GetEmptyMonitorSet();
                _monitorsByEnemyElementLookup.Add(element, monitors);
                isAdded = _enemyElementsDetected.Add(element);
                D.Assert(isAdded);
            }
            isAdded = monitors.Add(monitor);
            D.Assert(isAdded);

            if (!AreEnemyElementsInRange) {
                AreEnemyElementsInRange = true;
            }
        }

        public void AddWarEnemyElement(IUnitElement_Ltd element, IElementSensorRangeMonitor monitor) {
            HashSet<IUnitElement_Ltd> elements;
            if (!_warEnemyElementsByMonitorLookup.TryGetValue(monitor, out elements)) {
                elements = GetEmptyElementSet();
                _warEnemyElementsByMonitorLookup.Add(monitor, elements);
            }
            bool isAdded = elements.Add(element);
            D.Assert(isAdded);

            HashSet<IElementSensorRangeMonitor> monitors;
            if (!_monitorsByWarEnemyElementLookup.TryGetValue(element, out monitors)) {
                monitors = GetEmptyMonitorSet();
                _monitorsByWarEnemyElementLookup.Add(element, monitors);
                isAdded = _warEnemyElementsDetected.Add(element);
                D.Assert(isAdded);
            }
            isAdded = monitors.Add(monitor);
            D.Assert(isAdded);

            if (!AreWarEnemyElementsInRange) {
                AreWarEnemyElementsInRange = true;
                OnWarEnemyElementsInRangeChgd();
            }
        }

        public void AddEnemyCmd(IUnitCmd_Ltd cmd, IElementSensorRangeMonitor monitor) {
            //D.Log(ShowDebugLog, "{0}.AddEnemyCmd. Monitor = {1}. Frame = {2}.", DebugName, monitor.DebugName, Time.frameCount);
            HashSet<IUnitCmd_Ltd> cmds;
            if (!_enemyCmdsByMonitorLookup.TryGetValue(monitor, out cmds)) {
                cmds = GetEmptyCmdSet();
                //D.Log(ShowDebugLog, "{0}.AddMonitor. Adding Monitor {1} to monitor lookup. Frame = {2}.", DebugName, monitor.DebugName, Time.frameCount);
                _enemyCmdsByMonitorLookup.Add(monitor, cmds);
            }
            bool isAdded = cmds.Add(cmd);
            D.Assert(isAdded);

            HashSet<IElementSensorRangeMonitor> monitors;
            if (!_monitorsByEnemyCmdLookup.TryGetValue(cmd, out monitors)) {
                monitors = GetEmptyMonitorSet();
                _monitorsByEnemyCmdLookup.Add(cmd, monitors);
                isAdded = _enemyCmdsDetected.Add(cmd);
                D.Assert(isAdded);
            }
            isAdded = monitors.Add(monitor);
            D.Assert(isAdded);

            if (!AreEnemyCmdsInRange) {
                AreEnemyCmdsInRange = true;
                OnEnemyCmdsInRangeChgd();
            }
        }

        public void AddWarEnemyCmd(IUnitCmd_Ltd cmd, IElementSensorRangeMonitor monitor) {
            HashSet<IUnitCmd_Ltd> cmds;
            if (!_warEnemyCmdsByMonitorLookup.TryGetValue(monitor, out cmds)) {
                cmds = GetEmptyCmdSet();
                _warEnemyCmdsByMonitorLookup.Add(monitor, cmds);
            }
            bool isAdded = cmds.Add(cmd);
            D.Assert(isAdded);

            HashSet<IElementSensorRangeMonitor> monitors;
            if (!_monitorsByWarEnemyCmdLookup.TryGetValue(cmd, out monitors)) {
                monitors = GetEmptyMonitorSet();
                _monitorsByWarEnemyCmdLookup.Add(cmd, monitors);
                isAdded = _warEnemyCmdsDetected.Add(cmd);
                D.Assert(isAdded);
            }
            isAdded = monitors.Add(monitor);
            D.Assert(isAdded);

            if (!AreWarEnemyCmdsInRange) {
                AreWarEnemyCmdsInRange = true;
            }
        }

        public void RemoveEnemyElement(IUnitElement_Ltd element, IElementSensorRangeMonitor monitor) {
            D.Assert(!_parentCmd.IsDead);
            HashSet<IUnitElement_Ltd> elements;
            bool isKeyPresent = _enemyElementsByMonitorLookup.TryGetValue(monitor, out elements);
            D.Assert(isKeyPresent, "{0}: {1} not found when removing {2}.".Inject(DebugName, monitor.DebugName, element.DebugName));

            bool isRemoved = elements.Remove(element);
            D.Assert(isRemoved, element.DebugName);

            if (elements.Count == Constants.Zero) {
                _enemyElementsByMonitorLookup.Remove(monitor);
                RecycleSet(elements);
            }

            HashSet<IElementSensorRangeMonitor> monitors;
            isKeyPresent = _monitorsByEnemyElementLookup.TryGetValue(element, out monitors);
            D.Assert(isKeyPresent, element.DebugName);

            isRemoved = monitors.Remove(monitor);
            D.Assert(isRemoved);

            if (monitors.Count == Constants.Zero) {
                _monitorsByEnemyElementLookup.Remove(element);
                isRemoved = _enemyElementsDetected.Remove(element);
                D.Assert(isRemoved);
                RecycleSet(monitors);

            }

            if (_enemyElementsDetected.Count == Constants.Zero) {
                D.Assert(AreEnemyElementsInRange);
                AreEnemyElementsInRange = false;
            }
        }

        public void RemoveWarEnemyElement(IUnitElement_Ltd element, IElementSensorRangeMonitor monitor) {
            D.Assert(!_parentCmd.IsDead);
            HashSet<IUnitElement_Ltd> elements;
            bool isKeyPresent = _warEnemyElementsByMonitorLookup.TryGetValue(monitor, out elements);
            D.Assert(isKeyPresent);

            bool isRemoved = elements.Remove(element);
            D.Assert(isRemoved);

            if (elements.Count == Constants.Zero) {
                _warEnemyElementsByMonitorLookup.Remove(monitor);
                RecycleSet(elements);
            }

            HashSet<IElementSensorRangeMonitor> monitors;
            isKeyPresent = _monitorsByWarEnemyElementLookup.TryGetValue(element, out monitors);
            D.Assert(isKeyPresent);

            isRemoved = monitors.Remove(monitor);
            D.Assert(isRemoved);

            if (monitors.Count == Constants.Zero) {
                _monitorsByWarEnemyElementLookup.Remove(element);
                isRemoved = _warEnemyElementsDetected.Remove(element);
                D.Assert(isRemoved);
                RecycleSet(monitors);
            }

            if (_warEnemyElementsDetected.Count == Constants.Zero) {
                D.Assert(AreWarEnemyElementsInRange);
                AreWarEnemyElementsInRange = false;
                OnWarEnemyElementsInRangeChgd();
            }
        }

        public void RemoveEnemyCmd(IUnitCmd_Ltd cmd, IElementSensorRangeMonitor monitor) {
            D.Assert(!_parentCmd.IsDead);
            HashSet<IUnitCmd_Ltd> cmds;
            bool isKeyPresent = _enemyCmdsByMonitorLookup.TryGetValue(monitor, out cmds);
            D.Assert(isKeyPresent, "{0}: {1} not present. Frame = {2}.".Inject(DebugName, monitor.DebugName, Time.frameCount));

            bool isRemoved = cmds.Remove(cmd);
            D.Assert(isRemoved);

            if (cmds.Count == Constants.Zero) {
                _enemyCmdsByMonitorLookup.Remove(monitor);
                RecycleSet(cmds);
            }

            HashSet<IElementSensorRangeMonitor> monitors;
            isKeyPresent = _monitorsByEnemyCmdLookup.TryGetValue(cmd, out monitors);
            D.Assert(isKeyPresent);


            isRemoved = monitors.Remove(monitor);
            D.Assert(isRemoved);
            //D.Log(ShowDebugLog, "{0}: Removing Monitor {1}. Frame = {2}", DebugName, monitor.DebugName, Time.frameCount);

            if (monitors.Count == Constants.Zero) {
                _monitorsByEnemyCmdLookup.Remove(cmd);
                isRemoved = _enemyCmdsDetected.Remove(cmd);
                D.Assert(isRemoved);
                RecycleSet(monitors);
            }

            if (_enemyCmdsDetected.Count == Constants.Zero) {
                D.Assert(AreEnemyCmdsInRange);
                AreEnemyCmdsInRange = false;
                OnEnemyCmdsInRangeChgd();
            }
        }

        public void RemoveWarEnemyCmd(IUnitCmd_Ltd cmd, IElementSensorRangeMonitor monitor) {
            D.Assert(!_parentCmd.IsDead);
            HashSet<IUnitCmd_Ltd> cmds;
            bool isKeyPresent = _warEnemyCmdsByMonitorLookup.TryGetValue(monitor, out cmds);
            D.Assert(isKeyPresent);

            bool isRemoved = cmds.Remove(cmd);
            D.Assert(isRemoved);

            if (cmds.Count == Constants.Zero) {
                _warEnemyCmdsByMonitorLookup.Remove(monitor);
                RecycleSet(cmds);
            }

            HashSet<IElementSensorRangeMonitor> monitors;
            isKeyPresent = _monitorsByWarEnemyCmdLookup.TryGetValue(cmd, out monitors);
            D.Assert(isKeyPresent);

            isRemoved = monitors.Remove(monitor);
            D.Assert(isRemoved);

            if (monitors.Count == Constants.Zero) {
                _monitorsByWarEnemyCmdLookup.Remove(cmd);
                isRemoved = _warEnemyCmdsDetected.Remove(cmd);
                D.Assert(isRemoved);
                RecycleSet(monitors);
            }

            if (_warEnemyCmdsDetected.Count == Constants.Zero) {
                D.Assert(AreWarEnemyCmdsInRange);
                AreWarEnemyCmdsInRange = false;
            }
        }

        /// <summary>
        /// Adds the specified monitor and all ISensorDetectables currently detected by
        /// that monitor to this UnifiedSRSensorMonitor.
        /// <remarks>Called when an element is added to a Command whether during construction
        /// or runtime. If called during construction, the monitor has not detected any
        /// ISensorDetectables yet since it is not yet operational. When added to 
        /// a Command during runtime, it immediately populates the new Command's 
        /// UnifiedSRSensorMonitor with all its ISensorDetectables.</remarks>
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        public void Add(IElementSensorRangeMonitor monitor) {
            D.Assert(!_enemyElementsByMonitorLookup.ContainsKey(monitor));

            D.Assert(monitor.AreEnemyElementsInRange == monitor.EnemyElementsDetected.Any(), DebugName);
            //D.Log(ShowDebugLog, "{0}.AddMonitor. Monitor = {1}. Frame = {2}.", DebugName, monitor.DebugName, Time.frameCount);
            if (monitor.AreEnemyElementsInRange) {
                foreach (var element in monitor.EnemyElementsDetected) {
                    AddEnemyElement(element, monitor);
                }
                foreach (var element in monitor.WarEnemyElementsDetected) {
                    AddWarEnemyElement(element, monitor);
                }
            }
            D.Assert(monitor.AreEnemyCmdsInRange == monitor.EnemyCmdsDetected.Any(), DebugName);
            if (monitor.AreEnemyCmdsInRange) {
                foreach (var cmd in monitor.EnemyCmdsDetected) {
                    AddEnemyCmd(cmd, monitor);
                }
                foreach (var cmd in monitor.WarEnemyCmdsDetected) {
                    AddWarEnemyCmd(cmd, monitor);
                }
            }
        }

        /// <summary>
        /// Removes the specified monitor and all ISensorDetectables currently detected by
        /// that monitor from this UnifiedSRSensorMonitor.
        /// <remarks>Called when an IElementSensorRangeMonitor removes and reacquires its
        /// ISensorDetectables and when an element is removed from its Command.</remarks>
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        public void Remove(IElementSensorRangeMonitor monitor) {
            D.Assert(!_parentCmd.IsDead);
            //D.Log(ShowDebugLog, "{0}.RemoveMonitor. Monitor = {1}. Frame = {2}.", DebugName, monitor.DebugName, Time.frameCount);
            HashSet<IUnitElement_Ltd> elements;
            bool isKeyPresent = _enemyElementsByMonitorLookup.TryGetValue(monitor, out elements);
            if (isKeyPresent) {
                var elementsCopy = GetSetCopy(elements);
                foreach (var element in elementsCopy) {
                    RemoveEnemyElement(element, monitor);
                }
                RecycleSet(elementsCopy);

                isKeyPresent = _warEnemyElementsByMonitorLookup.TryGetValue(monitor, out elements);
                if (isKeyPresent) {
                    elementsCopy = GetSetCopy(elements);
                    foreach (var element in elementsCopy) {
                        RemoveWarEnemyElement(element, monitor);
                    }
                    RecycleSet(elementsCopy);
                }
            }

            HashSet<IUnitCmd_Ltd> cmds;
            isKeyPresent = _enemyCmdsByMonitorLookup.TryGetValue(monitor, out cmds);
            if (isKeyPresent) {
                var cmdsCopy = GetSetCopy(cmds);
                foreach (var cmd in cmdsCopy) {
                    RemoveEnemyCmd(cmd, monitor);
                }
                RecycleSet(cmdsCopy);

                isKeyPresent = _warEnemyCmdsByMonitorLookup.TryGetValue(monitor, out cmds);
                if (isKeyPresent) {
                    cmdsCopy = GetSetCopy(cmds);
                    foreach (var cmd in cmdsCopy) {
                        RemoveWarEnemyCmd(cmd, monitor);
                    }
                    RecycleSet(cmdsCopy);
                }
            }
        }

        #region Event and Property Change Handlers

        private void SceneLoadingEventHandler(object sender, EventArgs e) {
            HandleSceneLoading();
        }

        private void HandleSceneLoading() {
            ResetRecycleSystemForReuse();
        }

        private void OnEnemyCmdsInRangeChgd() {
            if (enemyCmdsInRangeChgd != null) {
                enemyCmdsInRangeChgd(this, EventArgs.Empty);
            }
        }

        private void OnWarEnemyElementsInRangeChgd() {
            if (warEnemyElementsInRangeChgd != null) {
                warEnemyElementsInRangeChgd(this, EventArgs.Empty);
            }
        }

        #endregion

        #region Recycle Sets System

        private void ResetRecycleSystemForReuse() {
            RecycleAllSets();
        }

        private void RecycleAllSets() {
            RecycleSet(_enemyElementsDetected);
            RecycleSet(_warEnemyElementsDetected);
            RecycleSet(_enemyCmdsDetected);
            RecycleSet(_warEnemyCmdsDetected);
            foreach (var set in _enemyElementsByMonitorLookup.Values) {
                RecycleSet(set);
            }
            foreach (var set in _warEnemyElementsByMonitorLookup.Values) {
                RecycleSet(set);
            }
            foreach (var set in _enemyCmdsByMonitorLookup.Values) {
                RecycleSet(set);
            }
            foreach (var set in _warEnemyCmdsByMonitorLookup.Values) {
                RecycleSet(set);
            }
            foreach (var set in _monitorsByEnemyElementLookup.Values) {
                RecycleSet(set);
            }
            foreach (var set in _monitorsByWarEnemyElementLookup.Values) {
                RecycleSet(set);
            }
            foreach (var set in _monitorsByEnemyCmdLookup.Values) {
                RecycleSet(set);
            }
            foreach (var set in _monitorsByWarEnemyCmdLookup.Values) {
                RecycleSet(set);
            }
        }

        private static Stack<HashSet<IElementSensorRangeMonitor>> _reusableMonitorSets = new Stack<HashSet<IElementSensorRangeMonitor>>(100);

        private HashSet<IElementSensorRangeMonitor> GetEmptyMonitorSet() {
            if (_reusableMonitorSets.Count == Constants.Zero) {
                __newSetsCreated++;
                return new HashSet<IElementSensorRangeMonitor>();
            }
            __recycledSetsUseCount++;
            return _reusableMonitorSets.Pop();
        }

        private HashSet<IElementSensorRangeMonitor> GetSetCopy(HashSet<IElementSensorRangeMonitor> set) {
            var copy = GetEmptyMonitorSet();
            foreach (var member in set) {
                copy.Add(member);
            }
            return copy;
        }

        private void RecycleSet(HashSet<IElementSensorRangeMonitor> set) {
            set.Clear();
            _reusableMonitorSets.Push(set);
        }

        private static Stack<HashSet<IUnitElement_Ltd>> _reusableElementSets = new Stack<HashSet<IUnitElement_Ltd>>(100);

        private HashSet<IUnitElement_Ltd> GetEmptyElementSet() {
            if (_reusableElementSets.Count == Constants.Zero) {
                __newSetsCreated++;
                return new HashSet<IUnitElement_Ltd>();
            }
            __recycledSetsUseCount++;
            return _reusableElementSets.Pop();
        }

        private HashSet<IUnitElement_Ltd> GetSetCopy(HashSet<IUnitElement_Ltd> set) {
            var copy = GetEmptyElementSet();
            foreach (var member in set) {
                copy.Add(member);
            }
            return copy;
        }

        private void RecycleSet(HashSet<IUnitElement_Ltd> set) {
            set.Clear();
            _reusableElementSets.Push(set);
        }

        private static Stack<HashSet<IUnitCmd_Ltd>> _reusableCmdSets = new Stack<HashSet<IUnitCmd_Ltd>>(20);

        private HashSet<IUnitCmd_Ltd> GetEmptyCmdSet() {
            if (_reusableCmdSets.Count == Constants.Zero) {
                __newSetsCreated++;
                return new HashSet<IUnitCmd_Ltd>();
            }
            __recycledSetsUseCount++;
            return _reusableCmdSets.Pop();
        }

        private HashSet<IUnitCmd_Ltd> GetSetCopy(HashSet<IUnitCmd_Ltd> set) {
            var copy = GetEmptyCmdSet();
            foreach (var member in set) {
                copy.Add(member);
            }
            return copy;
        }

        private void RecycleSet(HashSet<IUnitCmd_Ltd> set) {
            set.Clear();
            _reusableCmdSets.Push(set);
        }

        private static int __newSetsCreated;
        private static int __recycledSetsUseCount;
        // max size/capacity of a HashSet is irrelevant as there is no constructor that sets capacity

        public static void __ReportUsage() {
            Debug.LogFormat("All UnifiedSRSensorMonitor reuse statistics: {0} reuses of {1} Sets during session.", __recycledSetsUseCount, __newSetsCreated);
        }

        #endregion

        #region Cleanup

        private void Cleanup() {
            Unsubscribe();
            RecycleAllSets();
        }

        private void Unsubscribe() {
            GameReferences.GameManager.sceneLoading -= SceneLoadingEventHandler;
        }

        #endregion

        public override string ToString() {
            return DebugName;
        }

        #region IDisposable

        private bool _alreadyDisposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {

            Dispose(true);

            // This object is being cleaned up by you explicitly calling Dispose() so take this object off
            // the finalization queue and prevent finalization code from 'disposing' a second time
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="isExplicitlyDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isExplicitlyDisposing) {
            if (_alreadyDisposed) { // Allows Dispose(isExplicitlyDisposing) to mistakenly be called more than once
                D.Warn("{0} has already been disposed.", GetType().Name);
                return; //throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
            }

            if (isExplicitlyDisposing) {
                // Dispose of managed resources here as you have called Dispose() explicitly
                Cleanup();
            }

            // Dispose of unmanaged resources here as either 1) you have called Dispose() explicitly so
            // may as well clean up both managed and unmanaged at the same time, or 2) the Finalizer has
            // called Dispose(false) to cleanup unmanaged resources

            _alreadyDisposed = true;
        }

        #endregion

    }
}

