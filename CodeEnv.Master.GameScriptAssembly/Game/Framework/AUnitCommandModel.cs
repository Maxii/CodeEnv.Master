// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitCommandModel.cs
//  Abstract base class for a CommandItem, an object that commands Elements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for a CommandItem, an object that commands Elements.
/// </summary>
public abstract class AUnitCommandModel : AMortalItemModelStateMachine, ICommandModel, ICommandTarget {

    public event Action<IElementModel> onSubordinateElementDeath;

    public string UnitName { get { return Data.ParentName; } }

    public new ACommandData Data {
        get { return base.Data as ACommandData; }
        set { base.Data = value; }
    }

    private IElementModel _hqElement;
    public IElementModel HQElement {
        get { return _hqElement; }
        set { SetProperty<IElementModel>(ref _hqElement, value, "HQElement", OnHQElementChanged, OnHQElementChanging); }
    }

    public IList<IElementModel> Elements { get; set; }

    protected FormationGenerator _formationGenerator;

    protected override void Awake() {
        base.Awake();
        Elements = new List<IElementModel>();
        _formationGenerator = new FormationGenerator(this);
        // Derived class should call Subscribe() after all used references have been established
    }

    protected override void Initialize() {
        base.Initialize();
    }

    // formations are now generated when an element is added and/or when a HQ element is assigned

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscribers.Add(Data.SubscribeToPropertyChanged<ACommandData, Formation>(d => d.UnitFormation, OnFormationChanged));
    }

    /// <summary>
    /// Adds the Element to this Command including parenting if needed.
    /// </summary>
    /// <param name="element">The Element to add.</param>
    public virtual void AddElement(IElementModel element) {
        D.Assert(!Elements.Contains(element), "{0} attempting to add {1} that is already present.".Inject(FullName, element.FullName));
        D.Assert(!element.IsHQElement, "{0} adding element {1} already designated as the HQ Element.".Inject(FullName, element.FullName));
        // elements should already be enabled when added to a Cmd as that is commonly their state when transferred during runtime
        D.Assert((element as MonoBehaviour).enabled, "{0} is not yet enabled.".Inject(element.FullName));
        element.onItemDeath += OnSubordinateElementDeath;
        Elements.Add(element);
        Data.AddElement(element.Data);
        Transform parentTransform = _transform.parent;
        if (element.Transform.parent != parentTransform) {
            element.Transform.parent = parentTransform;   // local position, rotation and scale are auto adjusted to keep ship unchanged in worldspace
        }
        // TODO consider changing HQElement
    }

    public virtual void RemoveElement(IElementModel element) {
        element.onItemDeath -= OnSubordinateElementDeath;
        bool isRemoved = Elements.Remove(element);
        isRemoved = isRemoved && Data.RemoveElement(element.Data);
        D.Assert(isRemoved, "{0} not found.".Inject(element.FullName));
        if (Elements.Count == Constants.Zero) {
            D.Assert(Data.UnitHealth <= Constants.ZeroF, "{0} UnitHealth error.".Inject(FullName));
            KillCommand();
        }
    }

    private void OnSubordinateElementDeath(IMortalModel mortalItem) {
        D.Assert(mortalItem is AUnitElementModel);
        D.Log("{0} acknowledging {1} has been lost.", FullName, mortalItem.Data.Name);
        AUnitElementModel element = mortalItem as AUnitElementModel;
        RemoveElement(element);

        var temp = onSubordinateElementDeath;
        if (temp != null) {
            temp(element);
        }
    }

    protected virtual void OnHQElementChanging(IElementModel newElement) {
        Arguments.ValidateNotNull(newElement);
        if (HQElement != null) {
            HQElement.IsHQElement = false;
        }
        if (!Elements.Contains(newElement)) {
            // the player will typically select/change the HQ element of a Unit from the elements already present in the unit
            D.Warn("{0} assigned HQElement {1} that is not already present in Unit.", FullName, newElement.FullName);
            AddElement(newElement);
        }
    }

    protected virtual void OnHQElementChanged() {
        HQElement.IsHQElement = true;
        Data.HQElementData = HQElement.Data;
        Radius = HQElement.Radius;
        D.Log("{0}'s HQElement is now {1}.", Data.ParentName, HQElement.Data.Name);
        _formationGenerator.RegenerateFormation();
    }

    private void OnFormationChanged() {
        _formationGenerator.RegenerateFormation();
    }

    public override void __SimulateAttacked() {
        Elements.ForAll(e => e.__SimulateAttacked());
    }

    /// <summary>
    /// Checks for damage to this Command when its HQElement takes a hit.
    /// </summary>
    /// <param name="isHQElementAlive">if set to <c>true</c> [is hq element alive].</param>
    /// <returns><c>true</c> if the Command has taken damage.</returns>
    public bool __CheckForDamage(bool isHQElementAlive) {   // HACK needs work. Cmds should be hardened to defend against weapons, so pass along attackerWeaponStrength?
        bool isHit = (isHQElementAlive) ? RandomExtended<bool>.SplitChance() : true;
        if (isHit) {
            TakeHit(new CombatStrength(RandomExtended<ArmamentCategory>.Choice(offensiveArmamentCategories), UnityEngine.Random.Range(1F, Data.MaxHitPoints)));
        }
        else {
            D.Log("{0} avoided a hit.", FullName);
        }
        return isHit;
    }

    protected internal virtual void PositionElementInFormation(IElementModel element, Vector3 stationOffset) {
        element.Transform.position = HQElement.Transform.position + stationOffset;
        //D.Log("{0} positioned at {1}, offset by {2} from {3} at {4}.",
        //    element.FullName, element.Transform.position, stationOffset, HQElement.FullName, HQElement.Transform.position);
    }

    protected internal virtual void CleanupAfterFormationGeneration() { }

    protected abstract void KillCommand();

    protected override void Cleanup() {
        base.Cleanup();
        if (Data != null) { Data.Dispose(); }
    }

    # region StateMachine Support Methods

    protected void Dead_ExitState() {
        LogEvent();
        D.Error("{0}.Dead_ExitState should not occur.", Data.Name);
    }

    #endregion

    # region StateMachine Callbacks

    public override void OnShowCompletion() {
        RelayToCurrentState();
    }

    protected void OnTargetDeath(IMortalModel deadTarget) {
        //LogEvent();
        RelayToCurrentState(deadTarget);
    }

    void OnDetectedEnemy() {  // TODO connect to sensors when I get them
        RelayToCurrentState();
    }

    #endregion

    // subscriptions contained completely within this gameobject (both subscriber
    // and subscribee) donot have to be cleaned up as all instances are destroyed

    #region IMortalTarget Members

    public override void TakeHit(CombatStrength attackerWeaponStrength) {
        float damage = Data.Strength - attackerWeaponStrength;
        bool isCmdAlive = ApplyDamage(damage);
        D.Assert(isCmdAlive, "{0} should never die as a result of being hit.".Inject(Data.Name));
    }

    #endregion

    #region ICommandTarget Members

    public IEnumerable<IElementTarget> ElementTargets {
        get { return Elements.Cast<IElementTarget>(); }
    }

    #endregion

}

