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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine.Profiling;

/// <summary>
/// Abstract base class for a ColliderMonitor that contains a list of ranged equipment that operate it.
/// </summary>
/// <typeparam name="EquipmentType">The Type of ranged equipment.</typeparam>
public abstract class AEquipmentMonitor<EquipmentType> : AColliderMonitor where EquipmentType : ARangedEquipment {

    private const string DebugNameFormat = "{0}[{1}, {2:0.} Units]";

    public sealed override string DebugName {
        get {
            return DebugNameFormat.Inject(base.DebugName, RangeCategory.GetValueName(), RangeDistance);
        }
    }

    private RangeCategory _rangeCategory;
    /// <summary>
    /// The range category (short, medium, long) of the equipment.
    /// </summary>
    public RangeCategory RangeCategory {
        get { return _rangeCategory; }
        private set { SetProperty<RangeCategory>(ref _rangeCategory, value, "RangeCategory"); }
    }

    protected abstract int MaxEquipmentCount { get; }

    /// <summary>
    /// The ranged equipment associated with this monitor.
    /// </summary>
    protected IList<EquipmentType> _equipmentList;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _equipmentList = new List<EquipmentType>(MaxEquipmentCount);
    }

    /// <summary>
    /// Initializes all range distance values for this monitor and its equipment.
    /// <remarks>Equipment simply tracks the range distance value of the monitor.</remarks>
    /// </summary>
    public void InitializeRangeDistance() {
        RangeDistance = RefreshRangeDistance();
    }

    #region Event and Property Change Handlers

    // protected to allow SensorRangeMonitor to support Remove(sensor) 
    protected void EquipmentIsOperationalChangedEventHandler(object sender, EventArgs e) {
        HandleEquipmentIsOperationalChanged();
    }

    // protected to allow SensorRangeMonitor to support Remove(sensor) 
    protected void EquipmentIsDamagedChangedEventHandler(object sender, EventArgs e) {
        HandleEquipmentIsDamagedChanged();
    }

    #endregion

    private void HandleEquipmentIsOperationalChanged() {
        // 11.5.16 RefreshRangeDistance moved to HandleEquipmentIsDamagedChanged
        AssessIsOperational();
    }

    private void HandleEquipmentIsDamagedChanged() {
        RangeDistance = RefreshRangeDistance();
    }

    protected override void HandleRangeDistanceChanged() {
        base.HandleRangeDistanceChanged();
        RefreshEquipmentRangeDistance();
    }

    public virtual void Add(EquipmentType pieceOfEquipment) {
        D.Assert(!pieceOfEquipment.IsActivated);
        D.Assert(!_equipmentList.Contains(pieceOfEquipment));
        if (RangeCategory == RangeCategory.None) {
            RangeCategory = pieceOfEquipment.RangeCategory;
        }
        D.AssertEqual(RangeCategory, pieceOfEquipment.RangeCategory);
        AssignMonitorTo(pieceOfEquipment);
        _equipmentList.Add(pieceOfEquipment);

        Profiler.BeginSample("Event Subscription allocation", gameObject);
        pieceOfEquipment.isOperationalChanged += EquipmentIsOperationalChangedEventHandler;
        pieceOfEquipment.isDamagedChanged += EquipmentIsDamagedChangedEventHandler;
        Profiler.EndSample();
    }

    protected abstract void AssignMonitorTo(EquipmentType pieceOfEquipment);

    /// <summary>
    /// Removes the specified equipment. Returns <c>true</c> if this monitor
    /// is still in use (has equipment remaining even if not operational), <c>false</c> otherwise.
    /// <remarks>Equipment should be deactivated before removal.</remarks>
    /// </summary>
    /// <param name="pieceOfEquipment">The piece of equipment.</param>
    /// <returns></returns>
    ////[Obsolete("Not currently used")]
    protected virtual bool Remove(EquipmentType pieceOfEquipment) {
        D.Assert(!pieceOfEquipment.IsActivated);

        RemoveMonitorFrom(pieceOfEquipment);
        bool isRemoved = _equipmentList.Remove(pieceOfEquipment);
        D.Assert(isRemoved);
        if (_equipmentList.Count == Constants.Zero) {
            RangeCategory = RangeCategory.None;
        }

        Profiler.BeginSample("Event Subscription allocation", gameObject);
        pieceOfEquipment.isOperationalChanged -= EquipmentIsOperationalChangedEventHandler;
        pieceOfEquipment.isDamagedChanged -= EquipmentIsDamagedChangedEventHandler;
        Profiler.EndSample();

        return _equipmentList.Count > Constants.Zero;
        // Note: no need to RefreshRangeDistance(); as it occurs when the equipment is made non-operational just before removal
    }

    ////[Obsolete("Not currently used")]
    protected abstract void RemoveMonitorFrom(EquipmentType pieceOfEquipment);

    //**********************************************************************************************************
    // * Remove(equipment) obsoleted as it is my intention to replace existing equipment with
    // * upgraded equipment by building a new element to replace the existing element.
    // **********************************************************************************************************/    

    protected virtual void AssessIsOperational() {
        IsOperational = _equipmentList.Where(e => e.IsOperational).Any();
    }

    /************************************************************************************************************************************
     * Note: No reason to take a direct action in the monitor when the ParentItem dies as the ParentItem sets each equipment's
     * IsOperational state to false when death occurs. The monitor's IsOperational state follows the change in all its equipment to false.
     *************************************************************************************************************************************/

    /// <summary>
    /// Refreshes the range distance of this monitor. The range of a monitor can be affected by a number of factors 
    /// including an owner change and the quantity of equipment that is currently operational. When a monitor's
    /// range distance is refreshed, the resulting change, if any, also sets the RangeDistance value of each piece of
    /// equipment to the same value. RangeDistance is never set to 0.
    /// </summary>
    /// <returns></returns>
    protected abstract float RefreshRangeDistance();

    private void RefreshEquipmentRangeDistance() {
        _equipmentList.ForAll(e => e.RangeDistance = RangeDistance);
    }

    /// <summary>
    /// Hook that allows derived monitors to complete their reset in preparation for reuse by the same Parent.
    /// <remarks>This monitor supports installed piece(s) of equipment. Accordingly, deactivates and removes each piece of equipment in 
    /// preparation for adding new equipment.</remarks>
    /// </summary>
    protected override void CompleteResetForReuse() {
        base.CompleteResetForReuse();
        var equipmentListCopy = new List<EquipmentType>(_equipmentList);
        foreach (var equip in equipmentListCopy) {
            equip.IsActivated = false;
            Remove(equip);
        }
        ////RangeCategory = RangeCategory.None;
        D.AssertDefault((int)RangeCategory);
        D.AssertEqual(Constants.Zero, _equipmentList.Count);
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

        Profiler.BeginSample("Event Subscription allocation", gameObject);
        _equipmentList.ForAll(e => {
            e.isOperationalChanged -= EquipmentIsOperationalChangedEventHandler;
            e.isDamagedChanged -= EquipmentIsDamagedChangedEventHandler;
        });
        Profiler.EndSample();
    }

}

