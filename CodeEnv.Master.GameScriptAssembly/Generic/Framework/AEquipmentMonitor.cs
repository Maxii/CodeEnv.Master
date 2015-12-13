// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AEquipmentMonitor.cs
// Abstract base class for a ColliderMonitor that contains a list of ranged equipment that operate it.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for a ColliderMonitor that contains a list of ranged equipment that operate it.
/// </summary>
/// <typeparam name="EquipmentType">The Type of ranged equipment.</typeparam>
public abstract class AEquipmentMonitor<EquipmentType> : AColliderMonitor where EquipmentType : ARangedEquipment {

    private static string _nameFormat = "{0}.{1}[{2}, {3:0.} Units]";

    public sealed override string Name {
        get {
            if (ParentItem == null) { return base.Name; }
            return _nameFormat.Inject(ParentItem.FullName, GetType().Name, RangeCategory.GetEnumAttributeText(), RangeDistance);
        }
    }

    /// <summary>
    /// The range category (short, medium, long) of the equipment.
    /// </summary>
    public RangeCategory RangeCategory { get; private set; }

    /// <summary>
    /// The ranged equipment associated with this monitor.
    /// </summary>
    protected IList<EquipmentType> _equipmentList;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _equipmentList = new List<EquipmentType>();
    }

    #region Event and Property Change Handlers

    protected virtual void EquipmentIsOperationalChangedEventHandler(object sender, EventArgs e) {
        RangeDistance = RefreshRangeDistance();
        AssessIsOperational();
    }

    #endregion

    public virtual void Add(EquipmentType pieceOfEquipment) {
        D.Assert(!pieceOfEquipment.IsActivated);
        D.Assert(!_equipmentList.Contains(pieceOfEquipment));
        if (RangeCategory == RangeCategory.None) {
            RangeCategory = pieceOfEquipment.RangeCategory;
        }
        D.Assert(RangeCategory == pieceOfEquipment.RangeCategory);
        AssignMonitorTo(pieceOfEquipment);
        _equipmentList.Add(pieceOfEquipment);
        pieceOfEquipment.isOperationalChanged += EquipmentIsOperationalChangedEventHandler;
        // RangeDistance is set when when a piece of equipment first becomes operational.
    }

    protected abstract void AssignMonitorTo(EquipmentType pieceOfEquipment);

    /**********************************************************************************************************
     * Remove(equipment) eliminated as it is my intention to replace existing equipment with
     * upgraded equipment by building a new element to replace the existing element.
     **********************************************************************************************************/

    protected virtual void AssessIsOperational() {
        IsOperational = _equipmentList.Where(e => e.IsOperational).Any();
    }

    /************************************************************************************************************************************
     * Note: No reason to take a direct action in the monitor when the ParentItem dies as the ParentItem sets each equipment's
     * IsOperational state to false when death occurs. The monitor's IsOperational state follows the change in all its equipment to false.
     *************************************************************************************************************************************/

    /// <summary>
    /// Refreshes the range distance of this monitor. The range of a monitor can be affected
    /// by a number of factors including an owner change and the quantity of equipment that
    /// is currently operational.
    /// </summary>
    /// <returns></returns>
    protected abstract float RefreshRangeDistance();

    /// <summary>
    /// Resets this Monitor in preparation for reuse by the same Parent.  
    /// </summary>
    protected override void ResetForReuse() {
        base.ResetForReuse();
        RangeCategory = RangeCategory.None;
        D.Assert(_equipmentList.Count == Constants.Zero);
    }

    protected override void Cleanup() {
        base.Cleanup();
        _equipmentList.ForAll(e => {
            if (e is IDisposable) {
                (e as IDisposable).Dispose();
            }
        });
    }

    protected override void Unsubscribe() {
        base.Unsubscribe();
        _equipmentList.ForAll(e => e.isOperationalChanged -= EquipmentIsOperationalChangedEventHandler);
    }

}

