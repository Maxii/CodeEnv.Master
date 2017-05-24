// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ElementSensorRangeMonitor.cs
// SensorRangeMonitor for SR Sensors located on each UnitElement.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// SensorRangeMonitor for SR Sensors located on each UnitElement.
/// </summary>
public class ElementSensorRangeMonitor : ASensorRangeMonitor, IElementSensorRangeMonitor {

    public new IUnitElement ParentItem {
        get { return base.ParentItem as IUnitElement; }
        set { base.ParentItem = value as IUnitElement; }
    }

    private UnifiedSRSensorMonitor CmdsUnifiedMonitor { get { return ParentItem.Command.UnifiedSRSensorMonitor; } }

    protected override void HandleEnemyElementAdded(IUnitElement_Ltd enemyElement) {
        base.HandleEnemyElementAdded(enemyElement);
        D.AssertNotNull(ParentItem.Command, ParentItem.DebugName);
        D.AssertNotNull(CmdsUnifiedMonitor, ParentItem.DebugName);
        CmdsUnifiedMonitor.AddEnemyElement(enemyElement, this);
    }

    protected override void HandleWarEnemyElementAdded(IUnitElement_Ltd enemyElement) {
        base.HandleWarEnemyElementAdded(enemyElement);
        D.AssertNotNull(ParentItem.Command, ParentItem.DebugName);
        D.AssertNotNull(CmdsUnifiedMonitor, ParentItem.DebugName);
        CmdsUnifiedMonitor.AddWarEnemyElement(enemyElement, this);
    }

    protected override void HandleEnemyCmdAdded(IUnitCmd_Ltd command) {
        base.HandleEnemyCmdAdded(command);
        if (ShouldUpdateCmdsUnifiedMonitor) {
            D.AssertNotNull(ParentItem.Command, ParentItem.DebugName);
            D.AssertNotNull(CmdsUnifiedMonitor, ParentItem.DebugName);
            CmdsUnifiedMonitor.AddEnemyCmd(command, this);
        }
        else {
            //D.Log(ShowDebugLog, "{0}.HandleEnemyCmdAdded called while ParentItem's Cmd ref is null.", DebugName);
        }
    }

    protected override void HandleWarEnemyCmdAdded(IUnitCmd_Ltd command) {
        base.HandleWarEnemyCmdAdded(command);
        if (ShouldUpdateCmdsUnifiedMonitor) {
            D.AssertNotNull(ParentItem.Command, ParentItem.DebugName);
            D.AssertNotNull(CmdsUnifiedMonitor, ParentItem.DebugName);
            CmdsUnifiedMonitor.AddWarEnemyCmd(command, this);
        }
        else {
            //D.Log(ShowDebugLog, "{0}.HandleWarEnemyCmdAdded called while ParentItem's Cmd ref is null.", DebugName);
        }
    }

    protected override void HandleEnemyElementRemoved(IUnitElement_Ltd enemyElement) {
        base.HandleEnemyElementRemoved(enemyElement);
        D.AssertNotNull(ParentItem.Command, ParentItem.DebugName);
        D.AssertNotNull(CmdsUnifiedMonitor, ParentItem.DebugName);
        CmdsUnifiedMonitor.RemoveEnemyElement(enemyElement, this);
    }

    protected override void HandleWarEnemyElementRemoved(IUnitElement_Ltd enemyElement) {
        base.HandleWarEnemyElementRemoved(enemyElement);
        D.AssertNotNull(ParentItem.Command, ParentItem.DebugName);
        D.AssertNotNull(CmdsUnifiedMonitor, ParentItem.DebugName);
        CmdsUnifiedMonitor.RemoveWarEnemyElement(enemyElement, this);
    }

    protected override void HandleEnemyCmdRemoved(IUnitCmd_Ltd command) {
        base.HandleEnemyCmdRemoved(command);
        if (ShouldUpdateCmdsUnifiedMonitor) {
            D.AssertNotNull(ParentItem.Command, ParentItem.DebugName);
            D.AssertNotNull(CmdsUnifiedMonitor, ParentItem.DebugName);
            CmdsUnifiedMonitor.RemoveEnemyCmd(command, this);
        }
        else {
            // OPTIMIZE 5.15.17 I don't think this ever happens
            D.Warn("FYI. {0}.HandleEnemyCmdRemoved called while ParentItem's Cmd ref is null.", DebugName);
        }
    }

    protected override void HandleWarEnemyCmdRemoved(IUnitCmd_Ltd command) {
        base.HandleWarEnemyCmdRemoved(command);
        if (ShouldUpdateCmdsUnifiedMonitor) {
            D.AssertNotNull(ParentItem.Command, ParentItem.DebugName);
            D.AssertNotNull(CmdsUnifiedMonitor, ParentItem.DebugName);
            CmdsUnifiedMonitor.RemoveWarEnemyCmd(command, this);
        }
        else {
            // OPTIMIZE 5.15.17 I don't think this ever happens
            D.Warn("FYI. {0}.HandleWarEnemyCmdRemoved called while ParentItem's Cmd ref is null.", DebugName);
        }
    }

    protected override void HandleSensorDetectedItemsCleared() {
        base.HandleSensorDetectedItemsCleared();
        D.AssertNotNull(ParentItem.Command, ParentItem.DebugName);
        D.AssertNotNull(CmdsUnifiedMonitor, ParentItem.DebugName);
        CmdsUnifiedMonitor.Remove(this);
    }

    /// <summary>
    /// Returns <c>true</c> if this monitor should update CmdsUnifiedMonitor, <c>false</c> otherwise.
    /// <remarks>If <c>false</c> it means the ParentItem's Cmd reference is currently null or, if the
    /// ParentItem's Cmd reference is not null, then Cmd's UnifiedSRSensorMonitor is still null indicating
    /// it has not yet been initialized.
    /// There is no need to update in this situation as a LoneFleetCmd
    /// is in process of being created, and when it completes initialization, its 
    /// SRUnifiedSensorRangeMonitor will contain all the items currently detected
    /// by this ParentElement, including the element that sent the IsHQChgd 
    /// event that generated this update request to add/remove a Cmd.</remarks>
    /// </summary>
    /// <returns></returns>
    private bool ShouldUpdateCmdsUnifiedMonitor {
        get {
            if (ParentItem.Command == null || ParentItem.Command.UnifiedSRSensorMonitor == null) {
                D.Assert(__IsMonitorHandlingADetectedElementIsHQChgdEvent);
                return false;
            }
            return true;
        }
    }

    #region Debug

    protected override bool __ToReportTargetReacquisitionChanges { get { return false; } }

    #endregion

    #region Handle ParentItem Command Reference Unavailable Deferral System Archive

    // 5.15.17 OPTIMIZE I know the Add versions get called, but I've never seen the Remove versions called

    private bool _isParentElementCmdChangedSubscribed = false;

    private IList<IUnitCmd_Ltd> _deferredEnemyCmdsToAdd;

    private IList<IUnitCmd_Ltd> _deferredWarEnemyCmdsToAdd;

    private IList<IUnitCmd_Ltd> _deferredEnemyCmdsToRemove;

    private IList<IUnitCmd_Ltd> _deferredWarEnemyCmdsToRemove;

    private void HandleElementMissingCmdRef_AddEnemyCmd(IUnitCmd_Ltd cmdToAdd) {
        D.AssertNull(ParentItem.Command);
        D.Warn("FYI. {0}.HandleElementMissingCmdRef_AddEnemyCmd({1}) called.", DebugName, cmdToAdd.DebugName);
        _deferredEnemyCmdsToAdd = _deferredEnemyCmdsToAdd ?? new List<IUnitCmd_Ltd>();
        _deferredEnemyCmdsToAdd.Add(cmdToAdd);

        if (!_isParentElementCmdChangedSubscribed) {
            ParentItem.commandChanged += ParentElementCmdChangedEventHandler;
            _isParentElementCmdChangedSubscribed = true;
        }
    }

    private void HandleElementMissingCmdRef_AddWarEnemyCmd(IUnitCmd_Ltd cmdToAdd) {
        D.AssertNull(ParentItem.Command);
        D.Warn("FYI. {0}.HandleElementMissingCmdRef_AddWarEnemyCmd({1}) called.", DebugName, cmdToAdd.DebugName);
        _deferredWarEnemyCmdsToAdd = _deferredWarEnemyCmdsToAdd ?? new List<IUnitCmd_Ltd>();
        _deferredWarEnemyCmdsToAdd.Add(cmdToAdd);

        if (!_isParentElementCmdChangedSubscribed) {
            ParentItem.commandChanged += ParentElementCmdChangedEventHandler;
            _isParentElementCmdChangedSubscribed = true;
        }
    }

    private void HandleElementMissingCmdRef_RemoveEnemyCmd(IUnitCmd_Ltd cmdToRemove) {
        D.AssertNull(ParentItem.Command);
        D.Warn("FYI. {0}.HandleElementMissingCmdRef_RemoveEnemyCmd({1}) called.", DebugName, cmdToRemove.DebugName);
        _deferredEnemyCmdsToRemove = _deferredEnemyCmdsToRemove ?? new List<IUnitCmd_Ltd>();
        _deferredEnemyCmdsToRemove.Add(cmdToRemove);

        if (!_isParentElementCmdChangedSubscribed) {
            ParentItem.commandChanged += ParentElementCmdChangedEventHandler;
            _isParentElementCmdChangedSubscribed = true;
        }
    }

    private void HandleElementMissingCmdRef_RemoveWarEnemyCmd(IUnitCmd_Ltd cmdToRemove) {
        D.AssertNull(ParentItem.Command);
        D.Warn("FYI. {0}.HandleElementMissingCmdRef_RemoveWarEnemyCmd({1}) called.", DebugName, cmdToRemove.DebugName);
        _deferredWarEnemyCmdsToRemove = _deferredWarEnemyCmdsToRemove ?? new List<IUnitCmd_Ltd>();
        _deferredWarEnemyCmdsToRemove.Add(cmdToRemove);

        if (!_isParentElementCmdChangedSubscribed) {
            ParentItem.commandChanged += ParentElementCmdChangedEventHandler;
            _isParentElementCmdChangedSubscribed = true;
        }
    }

    private void ParentElementCmdChangedEventHandler(object sender, EventArgs e) {
        D.AssertNotNull(ParentItem.Command);
        D.Assert(_isParentElementCmdChangedSubscribed);
        D.Assert(ParentItem.Command.IsLoneCmd);
        D.Assert(!ParentItem.Command.IsOperational);

        ParentItem.Command.isOperationalOneshot += ParentElementCmdIsOperationalEventHandler;

        ParentItem.commandChanged -= ParentElementCmdChangedEventHandler;
        _isParentElementCmdChangedSubscribed = false;
    }

    private void ParentElementCmdIsOperationalEventHandler(object sender, EventArgs e) {
        // 5.15.17 CmdsUnifiedMonitor will be initialized before IsOperational becomes true. isOperational does not fire when dieing
        D.Assert(ParentItem.Command.IsOperational);
        D.AssertNotNull(CmdsUnifiedMonitor);

        if (_deferredEnemyCmdsToAdd != null && _deferredEnemyCmdsToAdd.Any()) {
            foreach (var cmd in _deferredEnemyCmdsToAdd) {
                D.Warn("FYI. {0} is adding {1} now that the reference to {2} is available and operational.", DebugName, cmd.DebugName, ParentItem.Command.DebugName);
                HandleEnemyCmdAdded(cmd);
            }
            _deferredEnemyCmdsToAdd.Clear();
        }

        if (_deferredWarEnemyCmdsToAdd != null && _deferredWarEnemyCmdsToAdd.Any()) {
            foreach (var cmd in _deferredWarEnemyCmdsToAdd) {
                D.Warn("FYI. {0} is adding {1} now that the reference to {2} is available and operational.", DebugName, cmd.DebugName, ParentItem.Command.DebugName);
                HandleWarEnemyCmdAdded(cmd);
            }
            _deferredWarEnemyCmdsToAdd.Clear();
        }

        if (_deferredEnemyCmdsToRemove != null && _deferredEnemyCmdsToRemove.Any()) {
            foreach (var cmd in _deferredEnemyCmdsToRemove) {
                D.Warn("FYI. {0} is removing {1} now that the reference to {2} is available and operational.", DebugName, cmd.DebugName, ParentItem.Command.DebugName);
                HandleEnemyCmdRemoved(cmd);
            }
            _deferredEnemyCmdsToRemove.Clear();
        }

        if (_deferredWarEnemyCmdsToRemove != null && _deferredWarEnemyCmdsToRemove.Any()) {
            foreach (var cmd in _deferredWarEnemyCmdsToRemove) {
                D.Warn("FYI. {0} is removing {1} now that the reference to {2} is available and operational.", DebugName, cmd.DebugName, ParentItem.Command.DebugName);
                HandleWarEnemyCmdRemoved(cmd);
            }
            _deferredWarEnemyCmdsToRemove.Clear();
        }

        ParentItem.Command.isOperationalOneshot -= ParentElementCmdIsOperationalEventHandler;
    }

    #endregion

}

