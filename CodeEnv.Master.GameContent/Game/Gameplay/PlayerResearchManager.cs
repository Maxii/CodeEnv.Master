// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlayerResearchManager.cs
// Manages the progression of research on technologies for a Player.
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
    using MoreLinq;

    /// <summary>
    /// Manages the progression of research on technologies for a Player.
    /// <remarks>Access via PlayerAIManager.</remarks>
    /// </summary>
    public class PlayerResearchManager : APropertyChangeTracking, IRecurringDateMinderClient, IDisposable {

        private const string DebugNameFormat = "{0}.{1}";

        public event EventHandler<FutureTechResearchCompletedEventArgs> futureTechRschCompleted;

        public event EventHandler currentResearchChanged;

        public event EventHandler<ResearchCompletedEventArgs> researchCompleted;

        public string DebugName { get { return DebugNameFormat.Inject(_player.DebugName, GetType().Name); } }

        protected ResearchTask _currentResearchTask = TempGameValues.NoResearch;
        public ResearchTask CurrentResearchTask {
            get { return _currentResearchTask; }
            private set {
                if (_currentResearchTask != value) {
                    _currentResearchTask = value;
                    CurrentResearchTaskPropChangedHandler();
                }
            }
        }

        public bool IsResearchQueued { get { return _pendingRschTaskQueue.Any(); } }

        protected Queue<ResearchTask> _pendingRschTaskQueue = new Queue<ResearchTask>();

        private HashSet<ResearchTask> _uncompletedRschTasks;
        private HashSet<ResearchTask> _completedRschTasks;

        private float _unconsumedScienceYield;
        private float _playerTotalScienceYield;

        private DateMinderDuration _researchUpdateDuration;
        private EquipmentStatFactory _eStatFactory;
        private TechnologyFactory _techFactory;
        private PlayerDesigns _playerDesigns;
        private PlayerAIManager _playerAiMgr;
        private Player _player;
        private GameTime _gameTime;
        private IList<IDisposable> _subscriptions;

        public PlayerResearchManager(PlayerAIManager aiMgr, PlayerDesigns designs) {
            _playerAiMgr = aiMgr;
            _player = aiMgr.Player;
            _playerDesigns = designs;
            InitializeValuesAndReferences();
            InitializeResearchState();
            InitializeEquipmentLevels();
        }

        private void InitializeValuesAndReferences() {
            _eStatFactory = EquipmentStatFactory.Instance;
            _techFactory = TechnologyFactory.Instance;
            _gameTime = GameTime.Instance;
            _uncompletedRschTasks = new HashSet<ResearchTask>();
            _completedRschTasks = new HashSet<ResearchTask>();
        }

        private void InitializeResearchState() {
            var allPredefinedTechs = TechnologyFactory.Instance.GetAllPredefinedTechs(_player);
            foreach (var tech in allPredefinedTechs) {
                _uncompletedRschTasks.Add(new ResearchTask(tech));
            }

            __ValidateTechPresence(allPredefinedTechs);

            IEnumerable<string> techNamesThatStartCompleted;
            if (__debugCntls.IsAllTechResearched) {
                techNamesThatStartCompleted = allPredefinedTechs.Select(tech => tech.Name);
            }
            else {
                var empireStartingLevel = GameReferences.GameManager.GameSettings.GetStartLevelFor(_player);
                techNamesThatStartCompleted = NewGameConfigurator.Instance.GetTechNamesThatStartCompleted(empireStartingLevel);
            }

            foreach (var techName in techNamesThatStartCompleted) {
                var rschTaskToComplete = _uncompletedRschTasks.Single(task => task.Tech.Name == techName);
                rschTaskToComplete.CompleteResearch();
                RecordAsCompleted(rschTaskToComplete);
            }

            __ValidateStartingRschState(_completedRschTasks);
        }

        /// <summary>
        /// Initializes the equipment levels in PlayerDesigns at startup.
        /// </summary>
        private void InitializeEquipmentLevels() {
            UpdateCurrentEquipmentLevels();
        }

        private void UpdateCurrentEquipmentLevels() {
            // ILookup is one to many whereas IDictionary is one to one
            ILookup<EquipmentCategory, AEquipmentStat> allEnabledStatsLookup
                = _completedRschTasks.SelectMany(task => task.Tech.GetEnabledEquipStats()).ToLookup(eStat => eStat.Category);
            var allEnabledEquipCats = allEnabledStatsLookup.Select(group => group.Key).Distinct();

            IList<EquipmentStatID> enabledEqCatsWithHighestLevelResearched = new List<EquipmentStatID>();
            foreach (var eqCat in allEnabledEquipCats) {
                // AHA! ILookup auto combines the many stats for this eqCat Key into an IEnumerable
                IEnumerable<AEquipmentStat> eqCatStats = allEnabledStatsLookup[eqCat];
                Level highestLevelResearched = eqCatStats.Max(stat => stat.Level);
                enabledEqCatsWithHighestLevelResearched.Add(new EquipmentStatID(eqCat, highestLevelResearched));
            }

            if (enabledEqCatsWithHighestLevelResearched.Any()) {
                _playerDesigns.UpdateEquipLevelAndReqdDesigns(enabledEqCatsWithHighestLevelResearched);
            }
        }

        public void CommenceOperations() {
            _playerAiMgr.PickFirstResearchTask();
            InitiateProgressChecks();
        }

        private void InitiateProgressChecks() {
            // OPTIMIZE 2.27.18 Updating progress every hour is expensive
            _researchUpdateDuration = new DateMinderDuration(GameTimeDuration.OneHour, this);
            _gameTime.RecurringDateMinder.Add(_researchUpdateDuration);
            RefreshTotalScienceYield();
            Subscribe();
        }

        private void Subscribe() {
            _subscriptions = new List<IDisposable>();
            _subscriptions.Add(_playerAiMgr.Knowledge.SubscribeToPropertyChanged<PlayerKnowledge, float>(pk => pk.TotalScienceYield, PlayerTotalScienceYieldPropChangedHandler));
        }

        public bool IsDiscovered(Technology tech) {
            return _completedRschTasks.Select(task => task.Tech).Contains(tech);
        }

        /// <summary>
        /// Gets all completed and uncompleted research tasks.
        /// <remarks>Must expect some tasks to be already completed from Saves and/or start conditions.</remarks>
        /// </summary>
        /// <returns></returns>
        public IList<ResearchTask> GetAllResearchTasks() {
            List<ResearchTask> tasks = new List<ResearchTask>(_completedRschTasks);
            tasks.AddRange(_uncompletedRschTasks);
            return tasks;
        }

        /// <summary>
        /// Returns the ResearchTask associated with the provided Technology.
        /// The task may already be completed, partially completed, currently underway or not started.
        /// </summary>
        /// <param name="tech">The tech.</param>
        /// <returns></returns>
        public ResearchTask GetResearchTaskFor(Technology tech) {
            return GetAllResearchTasks().Single(task => task.Tech == tech);
        }

        public void ChangeCurrentResearchTo(ResearchTask rschGoal) {
            D.Assert(!rschGoal.IsCompleted);

            _pendingRschTaskQueue.Clear();
            IEnumerable<ResearchTask> uncompletedPrereqs;
            if (TryGetUncompletedPrerequisites(rschGoal, out uncompletedPrereqs)) {
                var ascendingPrereqs = uncompletedPrereqs.OrderBy(pReq => pReq.Tech.ResearchCost);

                foreach (var pReq in ascendingPrereqs) {
                    _pendingRschTaskQueue.Enqueue(pReq);
                }
                D.Log("{0} is planning to initiate research on {1} prior to researching {2}.",
                    DebugName, ascendingPrereqs.Select(pReq => pReq.Tech.DebugName).Concatenate(), rschGoal.Tech.DebugName);
                _pendingRschTaskQueue.Enqueue(rschGoal);
                CurrentResearchTask = _pendingRschTaskQueue.Dequeue();
            }
            else {
                // all preReqs, if any, completed so rschGoal can become the CurrentResearchTask
                if (!_uncompletedRschTasks.Contains(rschGoal)) {
                    D.AssertEqual("FutureTech", rschGoal.Tech.Name);
                    //D.Log("{0}: ResearchTree has been completed so will initiate research on another FutureTech!", DebugName);
                    _uncompletedRschTasks.Add(rschGoal);
                }
                CurrentResearchTask = rschGoal;
            }
        }

        private void ProgressResearch(float availableScienceYield) {
            D.Assert(!GameReferences.GameManager.IsPaused);
            D.AssertNotEqual(TempGameValues.NoResearch, CurrentResearchTask, DebugName);
            //D.Log("{0} is checking {1} for completion on {2}.", DebugName, CurrentResearchTask.DebugName, _gameTime.CurrentDate);
            if (CurrentResearchTask.TryComplete(availableScienceYield, out _unconsumedScienceYield)) {
                //D.Log("{0} has completed research of {1} on {2}. Science/hour = {3:0.}.",
                //    DebugName, CurrentResearchTask.DebugName, _gameTime.CurrentDate, _playerTotalScienceYield);
                D.Assert(CurrentResearchTask.IsCompleted);

                var completedRsch = CurrentResearchTask;
                RecordAsCompleted(completedRsch);

                if (!__debugCntls.IsAllTechResearched) { // Update won't hurt but expensive -> no value if all equip already at highest level
                    UpdateCurrentEquipmentLevels();
                }

                OnResearchCompleted(completedRsch);
                // confirm that OnResearchCompleted didn't change CurrentResearchTask
                D.AssertEqual(completedRsch, CurrentResearchTask);

                if (IsResearchQueued) {
                    CurrentResearchTask = _pendingRschTaskQueue.Dequeue();
                }
                else {
                    if (!AssignNextResearchTask(completedRsch)) {
                        _currentResearchTask = TempGameValues.NoResearch;
                    }
                }
            }
        }

        private void RecordAsCompleted(ResearchTask completedRsch) {
            D.Assert(completedRsch.IsCompleted);
            D.Assert(_uncompletedRschTasks.Contains(completedRsch));
            bool isRemoved = _uncompletedRschTasks.Remove(completedRsch);
            D.Assert(isRemoved);
            bool isAdded = _completedRschTasks.Add(completedRsch);
            D.Assert(isAdded);
        }

        /// <summary>
        /// Selects and tries to assign the next ResearchTask that should follow <c>justCompletedRsch</c>.
        /// Returns <c>true</c> if the selected task was assigned via ChangeCurrentResearchTo(), <c>false</c> if
        /// it wasn't. 
        /// <remarks>If the AI is doing the selection and assignment, this method will always return true.
        /// If the User is using the ResearchScreen to manually do the selection/assignment, it will return 
        /// false, allowing the User to make the selection when they choose.</remarks>
        /// <remarks>All predefined Techs (including the first FutureTech) and their associated ResearchTasks 
        /// are assigned to their ResearchScreen nodes at startup. The one exception is when another FutureTech 
        /// is the only remaining tech left to be selected. Since that tech is dynamically created when needed
        /// it needs to replace the existing and just completed FutureTech in the ResearchScreen. This is
        /// handled by the OnFutureTechResearchCompleted event.</remarks>
        /// </summary>
        /// <param name="justCompletedRsch">The just completed ResearchTask.</param>
        /// <returns></returns>
        private bool AssignNextResearchTask(ResearchTask justCompletedRsch) {
            ResearchTask selectedRschTask;
            bool isFutureTechRuntimeCreation;
            bool toAssignSelectedRschTask = _playerAiMgr.TryPickNextResearchTask(justCompletedRsch, out selectedRschTask, out isFutureTechRuntimeCreation);
            bool wasAssigned = false;
            if (toAssignSelectedRschTask) {
                ChangeCurrentResearchTo(selectedRschTask);
                wasAssigned = true;
            }
            if (isFutureTechRuntimeCreation) {
                OnFutureTechResearchCompleted(selectedRschTask);
            }
            return wasAssigned;
        }

        private void UpdateTimeToCompletion() {
            foreach (var rschTask in _uncompletedRschTasks) {
                float additionalScienceReqdToComplete = rschTask.CostToResearch - rschTask.CumScienceApplied;
                float remainingHoursToComplete;
                if (rschTask == CurrentResearchTask) {   // IMPROVE
                    if (additionalScienceReqdToComplete <= _playerTotalScienceYield + _unconsumedScienceYield) {
                        remainingHoursToComplete = Constants.OneF;
                    }
                    else {
                        remainingHoursToComplete = Constants.OneF + additionalScienceReqdToComplete / _playerTotalScienceYield;
                    }
                }
                else {
                    remainingHoursToComplete = additionalScienceReqdToComplete / _playerTotalScienceYield;
                }
                GameTimeDuration remainingDurationToComplete = new GameTimeDuration(remainingHoursToComplete);
                if (remainingDurationToComplete != rschTask.TimeToComplete) {
                    //D.Log("{0}: {1}'s TimeToComplete changed from {2} to {3}.", DebugName, researchTask.DebugName, researchTask.TimeToComplete,
                    //remainingDurationToComplete);
                    rschTask.TimeToComplete = remainingDurationToComplete;
                }
            }
        }

        #region Event and Property Change Handlers

        private void CurrentResearchTaskPropChangedHandler() {
            OnCurrentResearchChanged();
        }

        private void PlayerTotalScienceYieldPropChangedHandler() {
            RefreshTotalScienceYield();
            UpdateTimeToCompletion();
        }

        private void OnCurrentResearchChanged() {
            if (currentResearchChanged != null) {
                currentResearchChanged(this, EventArgs.Empty);
            }
        }

        private void OnResearchCompleted(ResearchTask completedResearch) {
            if (researchCompleted != null) {
                researchCompleted(this, new ResearchCompletedEventArgs(completedResearch));
            }
        }

        private void OnFutureTechResearchCompleted(ResearchTask nextFutureTechRschTask) {
            if (futureTechRschCompleted != null) {
                futureTechRschCompleted(this, new FutureTechResearchCompletedEventArgs(nextFutureTechRschTask));
            }
        }

        #endregion

        public void HandlePlayerDefeated() {
            _gameTime.RecurringDateMinder.Remove(_researchUpdateDuration);
        }

        private void RefreshTotalScienceYield() {
            var playerTotalScienceYield = _playerAiMgr.Knowledge.TotalScienceYield;
            D.AssertNotEqual(Constants.ZeroF, playerTotalScienceYield);
            _playerTotalScienceYield = playerTotalScienceYield;
        }

        private bool HasUncompletedPrerequisites(Technology tech) {
            IEnumerable<Technology> unusedUncompletedPrereqs;
            return TryGetUncompletedPrerequisites(tech, out unusedUncompletedPrereqs);
        }

        private bool TryGetUncompletedPrerequisites(Technology tech, out IEnumerable<Technology> uncompletedPrerequisites) {
            var techPrerequisites = tech.Prerequisites;
            if (techPrerequisites.Any()) {
                HashSet<Technology> allUncompletedPrereqs = new HashSet<Technology>(techPrerequisites.Where(preReqTech => !IsDiscovered(preReqTech)));
                HashSet<Technology> uncompletedPrereqsToAdd = new HashSet<Technology>();   // can't modify uncompletedPrereqList while iterating
                foreach (var uncompletedPrereq in allUncompletedPrereqs) {
                    if (TryGetUncompletedPrerequisites(uncompletedPrereq, out uncompletedPrerequisites)) {
                        foreach (var uPrereq in uncompletedPrerequisites) {
                            uncompletedPrereqsToAdd.Add(uPrereq);
                        }
                    }
                }
                foreach (var uPrereq in uncompletedPrereqsToAdd) {
                    allUncompletedPrereqs.Add(uPrereq);
                }
                uncompletedPrerequisites = allUncompletedPrereqs;
                return allUncompletedPrereqs.Any();
            }
            uncompletedPrerequisites = Enumerable.Empty<Technology>();
            return false;
        }

        private bool TryGetUncompletedPrerequisites(ResearchTask rschTask, out IEnumerable<ResearchTask> uncompletedPrerequisites) {
            var techPrerequisites = rschTask.Tech.Prerequisites;
            if (techPrerequisites.Any()) {
                var rschTaskPrereqs = techPrerequisites.Select(techPrereq => GetResearchTaskFor(techPrereq));
                HashSet<ResearchTask> allUncompletedPrereqs = new HashSet<ResearchTask>(rschTaskPrereqs.Where(preReq => !preReq.IsCompleted));
                HashSet<ResearchTask> uncompletedPrereqsToAdd = new HashSet<ResearchTask>();   // can't modify uncompletedPrereqList while iterating
                foreach (var uncompletedPrereq in allUncompletedPrereqs) {
                    if (TryGetUncompletedPrerequisites(uncompletedPrereq, out uncompletedPrerequisites)) {
                        foreach (var uPrereq in uncompletedPrerequisites) {
                            uncompletedPrereqsToAdd.Add(uPrereq);
                        }
                    }
                }
                foreach (var uPrereq in uncompletedPrereqsToAdd) {
                    allUncompletedPrereqs.Add(uPrereq);
                }
                uncompletedPrerequisites = allUncompletedPrereqs;
                return allUncompletedPrereqs.Any();
            }
            uncompletedPrerequisites = Enumerable.Empty<ResearchTask>();
            return false;
        }

        private void Cleanup() {
            Unsubscribe();
        }

        private void Unsubscribe() {
            _subscriptions.ForAll(d => d.Dispose());
            _subscriptions.Clear();
        }

        public override string ToString() {
            return DebugName;
        }

        #region Debug

        private IDebugControls __debugCntls = GameReferences.DebugControls;

        public bool __TryGetRandomUncompletedRsch(out ResearchTask uncompletedRsch) {
            if (_uncompletedRschTasks.Any()) {
                uncompletedRsch = RandomExtended.Choice(_uncompletedRschTasks);
                return true;
            }
            uncompletedRsch = null;
            return false;
        }

        public ResearchTask __GetQuickestCompletionStartingRschTask() {
            // Only used to pick starting tech/task...
            ResearchTask result;
            var uncompletedTasksWithAllPrereqsCompleted = _uncompletedRschTasks.Where(task => !HasUncompletedPrerequisites(task.Tech));
            if (uncompletedTasksWithAllPrereqsCompleted.Any()) {
                var quickestCompletionTask = uncompletedTasksWithAllPrereqsCompleted.MinBy(task => task.RemainingScienceNeededToComplete);
                D.AssertNotEqual(Constants.ZeroF, quickestCompletionTask.RemainingScienceNeededToComplete);
                result = quickestCompletionTask;
            }
            else {
                // ...but if all tech starts researched ...
                var justCompletedFutureTech = _completedRschTasks.Select(task => task.Tech).Single(tech => tech.Name == "FutureTech");
                var futureTech = TechnologyFactory.Instance.MakeFutureTechInstanceFollowing(_player, justCompletedFutureTech);
                result = new ResearchTask(futureTech);
            }
            return result;
        }

        private void __ValidateTechPresence(IEnumerable<Technology> allPredefinedTechs) {
            var allPredefinedEqStatsInTechs = allPredefinedTechs.SelectMany(tech => tech.GetEnabledEquipStats());
            var allPredefinedEqCatsInTechs = allPredefinedEqStatsInTechs.Select(stat => stat.Category).Distinct();
            var allEquipCats = Enums<EquipmentCategory>.GetValues(excludeDefault: true);
            bool areAllCatsPresentInTechs = allEquipCats.SequenceEquals(allPredefinedEqCatsInTechs, ignoreOrder: true);
            if (!areAllCatsPresentInTechs) {
                D.Warn("{0}: ResearchTree is missing EquipmentStats for {1}: {2}.", DebugName, typeof(EquipmentCategory).Name,
                    allEquipCats.Except(allPredefinedEqCatsInTechs).Select(eCat => eCat.GetValueName()).Concatenate());
            }

            ILookup<Level, AEquipmentStat> levelLookup = allPredefinedEqStatsInTechs.ToLookup(stat => stat.Level);
            var allLevels = Enums<Level>.GetValues(excludeDefault: true);
            foreach (Level level in allLevels) {
                IEnumerable<AEquipmentStat> levelStats = levelLookup[level];
                var levelEqCats = levelStats.Select(stat => stat.Category);
                EquipmentCategory firstDuplicateCat;
                if (levelEqCats.ContainsDuplicates(out firstDuplicateCat)) {
                    D.Warn("{0}: ResearchTree Level {1} has a duplicate {2}.", DebugName, level.GetValueName(), firstDuplicateCat.GetValueName());
                }
            }
        }

        private void __ValidateStartingRschState(IEnumerable<ResearchTask> completedRschTasks) {
            var eqCatsReqdToStartCompleted = TempGameValues.EquipCatsReqdAtStartup;
            var eqCatsCompleted = completedRschTasks.SelectMany(task => task.Tech.GetEnabledEquipStats()).Select(stat => stat.Category);
            var reqdEqCatsNotCompleted = eqCatsReqdToStartCompleted.Except(eqCatsCompleted);
            if (reqdEqCatsNotCompleted.Any()) {
                D.Error("{0} is starting game without Reqd Equipment enabled: {1}.", DebugName, reqdEqCatsNotCompleted.Select(eCat => eCat.GetValueName()).Concatenate());
            }
        }

        #endregion

        #region Nested Classes

        public class ResearchCompletedEventArgs : EventArgs {

            public ResearchTask CompletedResearch { get; private set; }

            public ResearchCompletedEventArgs(ResearchTask completedResearch) {
                CompletedResearch = completedResearch;
            }
        }

        public class FutureTechResearchCompletedEventArgs : EventArgs {

            public ResearchTask NextFutureTechResearchTask { get; private set; }

            public FutureTechResearchCompletedEventArgs(ResearchTask nextFutureTechResearchTask) {
                NextFutureTechResearchTask = nextFutureTechResearchTask;
            }
        }

        #endregion

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

        #region IRecurringDateMinderClient Members

        public void HandleDateReached(DateMinderDuration recurringDuration) {
            D.AssertEqual(_researchUpdateDuration, recurringDuration);
            ProgressResearch(_playerTotalScienceYield + _unconsumedScienceYield);
        }

        #endregion


    }
}

