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

using CodeEnv.Master.GameContent;

/// <summary>
/// SensorRangeMonitor for SR Sensors located on each UnitElement.
/// </summary>
public class ElementSensorRangeMonitor : ASensorRangeMonitor, IElementSensorRangeMonitor {

    public new IUnitElement ParentItem {
        get { return base.ParentItem as IUnitElement; }
        set { base.ParentItem = value as IUnitElement; }
    }

    protected override void HandleEnemyElementAdded(IUnitElement_Ltd enemyElement) {
        base.HandleEnemyElementAdded(enemyElement);
        ParentItem.Command.UnifiedSRSensorMonitor.AddEnemyElement(enemyElement, this);
    }

    protected override void HandleWarEnemyElementAdded(IUnitElement_Ltd enemyElement) {
        base.HandleWarEnemyElementAdded(enemyElement);
        ParentItem.Command.UnifiedSRSensorMonitor.AddWarEnemyElement(enemyElement, this);
    }

    protected override void HandleEnemyCmdAdded(IUnitCmd_Ltd command) {
        base.HandleEnemyCmdAdded(command);
        ParentItem.Command.UnifiedSRSensorMonitor.AddEnemyCmd(command, this);
    }

    protected override void HandleWarEnemyCmdAdded(IUnitCmd_Ltd command) {
        base.HandleWarEnemyCmdAdded(command);
        ParentItem.Command.UnifiedSRSensorMonitor.AddWarEnemyCmd(command, this);
    }


    protected override void HandleEnemyElementRemoved(IUnitElement_Ltd enemyElement) {
        base.HandleEnemyElementRemoved(enemyElement);
        ParentItem.Command.UnifiedSRSensorMonitor.RemoveEnemyElement(enemyElement, this);
    }

    protected override void HandleWarEnemyElementRemoved(IUnitElement_Ltd enemyElement) {
        base.HandleWarEnemyElementRemoved(enemyElement);
        ParentItem.Command.UnifiedSRSensorMonitor.RemoveWarEnemyElement(enemyElement, this);
    }

    protected override void HandleEnemyCmdRemoved(IUnitCmd_Ltd command) {
        base.HandleEnemyCmdRemoved(command);
        ParentItem.Command.UnifiedSRSensorMonitor.RemoveEnemyCmd(command, this);
    }

    protected override void HandleWarEnemyCmdRemoved(IUnitCmd_Ltd command) {
        base.HandleWarEnemyCmdRemoved(command);
        ParentItem.Command.UnifiedSRSensorMonitor.RemoveWarEnemyCmd(command, this);
    }

    protected override void HandleSensorDetectedItemsCleared() {
        base.HandleSensorDetectedItemsCleared();
        ParentItem.Command.UnifiedSRSensorMonitor.Remove(this);
    }
}

