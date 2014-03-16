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
public abstract class AUnitCommandModel : AMortalItemModelStateMachine, ICommandTarget {

    public event Action<AUnitElementModel> onSubordinateElementDeath;

    public string PieceName { get { return Data.OptionalParentName; } }

    public new ACommandData Data {
        get { return base.Data as ACommandData; }
        set { base.Data = value; }
    }

    private AUnitElementModel _hqElement;
    public AUnitElementModel HQElement {
        get { return _hqElement; }
        set { SetProperty<AUnitElementModel>(ref _hqElement, value, "HQElement", OnHQElementChanged, OnHQElementChanging); }
    }

    public IList<AUnitElementModel> Elements { get; set; }

    protected FormationGenerator _formationGenerator;

    protected override void Awake() {
        base.Awake();
        Elements = new List<AUnitElementModel>();
        _formationGenerator = new FormationGenerator(this);
        // Derived class should call Subscribe() after all used references have been established
    }

    protected override void Initialize() {
        HQElement = SelectHQElement();
        _formationGenerator.RegenerateFormation();
    }

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscribers.Add(Data.SubscribeToPropertyChanged<ACommandData, Formation>(d => d.UnitFormation, OnFormationChanged));
    }

    /// <summary>
    /// Adds the Element to this Command including parenting if needed.
    /// </summary>
    /// <param name="element">The Element to add.</param>
    public virtual void AddElement(AUnitElementModel element) {
        D.Assert(!element.IsHQElement, "{0} adding element {1} already designated as the HQ Element.".Inject(Name, element.Name));   // by definition, an element can't already be the HQ Element when it is being added
        element.onItemDeath += OnSubordinateElementDeath;
        Elements.Add(element);
        Data.AddElement(element.Data);
        Transform parentTransform = _transform.parent;
        if (element.transform.parent != parentTransform) {
            element.transform.parent = parentTransform;   // local position, rotation and scale are auto adjusted to keep ship unchanged in worldspace
        }
        // TODO consider changing HQElement
    }

    public virtual void RemoveElement(AUnitElementModel element) {
        element.onItemDeath -= OnSubordinateElementDeath;
        bool isRemoved = Elements.Remove(element);
        isRemoved = isRemoved && Data.RemoveElement(element.Data);
        D.Assert(isRemoved, "{0} not found.".Inject(element.Data.Name));
        if (Elements.Count <= Constants.Zero) {
            D.Assert(Data.UnitHealth <= Constants.ZeroF, "{0} UnitHealth error.".Inject(Data.Name));
            KillCommand();
            return;
        }
        if (element == HQElement) {
            // HQ Element has left
            HQElement = SelectHQElement();
            D.Log("{0} new HQElement = {1}.", Data.Name, HQElement.Data.Name);
        }
    }

    private void OnSubordinateElementDeath(IMortalTarget mortalItem) {
        D.Assert(mortalItem is AUnitElementModel);
        D.Log("{0} acknowledging {1} has been lost.", Data.Name, mortalItem.Name);
        AUnitElementModel element = mortalItem as AUnitElementModel;
        RemoveElement(element);

        var temp = onSubordinateElementDeath;
        if (temp != null) {
            temp(element);
        }
    }

    protected virtual void OnHQElementChanging(AUnitElementModel newElement) {
        if (HQElement != null) {
            HQElement.IsHQElement = false;
        }
    }

    protected virtual void OnHQElementChanged() {
        HQElement.IsHQElement = true;
        Data.HQElementData = HQElement.Data;
    }

    private void OnFormationChanged() {
        _formationGenerator.RegenerateFormation();
    }

    public override void __SimulateAttacked() {
        Elements.ForAll<AUnitElementModel>(e => e.__SimulateAttacked());
    }

    /// <summary>
    /// Checks for damage to this Command when its HQElement takes a hit.
    /// </summary>
    /// <param name="isHQElementAlive">if set to <c>true</c> [is hq element alive].</param>
    /// <returns><c>true</c> if the Command has taken damage.</returns>
    public bool __CheckForDamage(bool isHQElementAlive) {
        bool isHit = (isHQElementAlive) ? RandomExtended<bool>.SplitChance() : true;
        if (isHit) {
            TakeDamage(UnityEngine.Random.Range(1F, Data.MaxHitPoints + 1F));
        }
        else {
            D.Log("{0} avoided a hit.", Data.Name);
        }
        return isHit;
    }

    ///// <summary>
    ///// Generates a new formation based on the formation selected and the 
    ///// number of elements present in the Unit.
    ///// </summary>
    ///// <exception cref="System.NotImplementedException"></exception>
    //protected void RegenerateFormation() {
    //    switch (Data.UnitFormation) {
    //        case Formation.Circle:
    //            PositionElementsEquidistantInCircle();
    //            break;
    //        case Formation.Globe:
    //            PositionElementsRandomlyInSphere();
    //            break;
    //        case Formation.None:
    //        default:
    //            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(Data.UnitFormation));
    //    }
    //    CleanupAfterFormationGeneration();
    //}

    ///// <summary>
    ///// Randomly positions the elements of the unit in a spherical globe around the HQ Element.
    ///// </summary>
    //private void PositionElementsRandomlyInSphere() {
    //    float globeRadius = 1F * (float)Math.Pow(Elements.Count * 0.2F, 0.33F);  // cube root of number of groups of 5 elements

    //    var elementsToPosition = Elements.Except(HQElement).ToArray();
    //    if (!TryPositionRandomWithinSphere(HQElement, globeRadius, ref elementsToPosition)) {
    //        // try again with a larger radius
    //        D.Assert(TryPositionRandomWithinSphere(HQElement, globeRadius * 1.5F, ref elementsToPosition),
    //            "{0} Formation Positioning Error.".Inject(Data.Name));
    //    }
    //}

    ///// <summary>
    ///// Positions the provided game objects randomly inside a sphere in such a way that the meshes
    ///// are not in contact.
    ///// </summary>
    ///// <param name="hqElement">The hq element with FormationPosition fixed at Vector3.zero.</param>
    ///// <param name="radius">The radius of the sphere in units.</param>
    ///// <param name="elementsToPosition">The non-HQ elements to position.</param>
    ///// <returns>
    /////   <c>true</c> if all elements were successfully positioned without overlap.
    ///// </returns>
    //private bool TryPositionRandomWithinSphere(AUnitElementModel hqElement, float radius, ref UnitElementModelType[] elementsToPosition) {
    //    IList<Bounds> allElementBounds = new List<Bounds>();

    //    Bounds hqElementBounds = new Bounds();
    //    bool toEncapsulateHqElement = false;
    //    D.Assert(UnityUtility.GetBoundWithChildren(hqElement.transform, ref hqElementBounds, ref toEncapsulateHqElement),
    //        "{0} unable to construct a Bound for HQ Element {1}.".Inject(Name, hqElement.Name));
    //    allElementBounds.Add(hqElementBounds);

    //    int iterateCount = 0;
    //    Vector3[] formationStationOffsets = new Vector3[elementsToPosition.Length];
    //    for (int i = 0; i < elementsToPosition.Length; i++) {
    //        bool toEncapsulate = false;
    //        Vector3 candidateStationOffset = UnityEngine.Random.insideUnitSphere * radius;
    //        Bounds elementBounds = new Bounds();
    //        UnitElementModelType element = elementsToPosition[i];
    //        if (UnityUtility.GetBoundWithChildren(element.transform, ref elementBounds, ref toEncapsulate)) {
    //            elementBounds.center = candidateStationOffset;
    //            //D.Log("Bounds = {0}.", elementBounds.ToString());
    //            if (allElementBounds.All(eb => !eb.Intersects(elementBounds))) {
    //                allElementBounds.Add(elementBounds);
    //                formationStationOffsets[i] = candidateStationOffset;
    //                iterateCount = 0;
    //            }
    //            else {
    //                i--;
    //                iterateCount++;
    //                if (iterateCount >= 10) {
    //                    D.Warn("{0} had a formation positioning iteration error.", Name);
    //                    return false;
    //                }
    //            }
    //        }
    //        else {
    //            D.Error("{0} unable to construct a Bound for {1}.", Name, element.name);
    //            return false;
    //        }
    //    }
    //    for (int i = 0; i < elementsToPosition.Length; i++) {
    //        PositionElementInFormation(elementsToPosition[i], formationStationOffsets[i]);
    //        //elementsToPosition[i].transform.localPosition = localFormationPositions[i];   // won't work as the position of the Element's parent is arbitrary
    //    }
    //    return true;
    //}

    ///// <summary>
    ///// Positions the elements equidistant in a circle around the HQ Element.
    ///// </summary>
    //protected void PositionElementsEquidistantInCircle() {
    //    float globeRadius = 1F * (float)Math.Pow(Elements.Count * 0.2F, 0.33F);  // cube root of number of groups of 5 elements

    //    Vector3 hqElementPosition = HQElement.Position;
    //    var elementsToPosition = Elements.Except(HQElement);
    //    //D.Log("{0}.elementsCount = {1}.", GetType().Name, _elements.Count);
    //    Stack<Vector3> formationStationOffsets = new Stack<Vector3>(Mathfx.UniformPointsOnCircle(globeRadius, elementsToPosition.Count()));
    //    foreach (var element in elementsToPosition) {
    //        Vector3 stationOffset = formationStationOffsets.Pop();
    //        PositionElementInFormation(element, stationOffset);
    //    }
    //}

    protected internal virtual void PositionElementInFormation(AUnitElementModel element, Vector3 stationOffset) {
        element.transform.position = HQElement.transform.position + stationOffset;
    }

    protected internal virtual void CleanupAfterFormationGeneration() { }

    protected abstract void KillCommand();

    protected abstract AUnitElementModel SelectHQElement();

    protected override void Cleanup() {
        base.Cleanup();
        Data.Dispose();
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

    protected void OnTargetDeath(IMortalTarget deadTarget) {
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

    public override void TakeDamage(float damage) {
        bool isCmdAlive = ApplyDamage(damage);
        D.Assert(isCmdAlive, "{0} should never die as a result of being hit.".Inject(Data.Name));
    }

    #endregion

    #region IUnitTarget Members

    public float MaxWeaponsRange { get { return Data.UnitMaxWeaponsRange; } }

    #endregion

    #region ICommandTarget Members

    public IEnumerable<IElementTarget> ElementTargets {
        get { return Elements.Cast<IElementTarget>(); }
    }

    #endregion

}

