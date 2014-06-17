// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FormationGenerator.cs
// Generates formations of Elements for Commands.
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
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Generates formations of Elements for Commands.
/// </summary>
public class FormationGenerator {

    // NOTE: don't replace with ICommandModel as this will force addition of 2 little used methods to the interface
    private AUnitCommandModel _unitCmd;
    private int _maxElementCount;

    public FormationGenerator(AUnitCommandModel unitCmd) {
        _unitCmd = unitCmd;
        if (unitCmd is IFleetCmdModel) {
            _maxElementCount = TempGameValues.MaxShipsPerFleet;
        }
        else {
            _maxElementCount = TempGameValues.MaxFacilitiesPerBase;
        }
    }

    /// <summary>
    /// Generates a new formation based on the formation selected and the
    /// number of elements present in the Unit.
    /// </summary>
    /// <param name="minimumSeparation">The minimum separation between elements. TODO implement so ship formationStations do not overlap.</param>
    /// <exception cref="System.NotImplementedException"></exception>
    public void RegenerateFormation(float minimumSeparation = Constants.ZeroF) {
        D.Assert(_unitCmd.HQElement != null, "{0} does not have a HQ Element needed to generate a formation.".Inject(_unitCmd.FullName), true);
        D.Log("{0} is about to regenerate its formation to {1}.", _unitCmd.Data.ParentName, _unitCmd.Data.UnitFormation.GetName());
        switch (_unitCmd.Data.UnitFormation) {
            case Formation.Circle:
                PositionElementsEquidistantInCircle();
                break;
            case Formation.Globe:
                PositionElementsRandomlyInSphere();
                break;
            case Formation.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_unitCmd.Data.UnitFormation));
        }
        _unitCmd.CleanupAfterFormationGeneration();
    }

    /// <summary>
    /// Randomly positions the elements of the unit in a spherical globe around the HQ Element.
    /// </summary>
    private void PositionElementsRandomlyInSphere() {
        float globeRadius = (float)Math.Pow(_maxElementCount * 0.2F, 0.33F);  // ~ 1.7

        IElementModel hqElement = _unitCmd.HQElement;
        var elementsToPositionAroundHQ = _unitCmd.Elements.Except(hqElement).ToArray();
        if (!TryPositionRandomWithinSphere(hqElement, globeRadius, elementsToPositionAroundHQ)) {
            // try again with a larger radius
            D.Assert(TryPositionRandomWithinSphere(_unitCmd.HQElement, globeRadius * 1.5F, elementsToPositionAroundHQ),
                "{0} Formation Positioning Error.".Inject(_unitCmd.Data.Name));
        }
    }

    /// <summary>
    /// Positions the provided elements randomly inside a sphere surrounding the HQ element in such a way that the meshes
    /// are not in contact.
    /// </summary>
    /// <param name="hqElement">The hq element.</param>
    /// <param name="radius">The radius of the sphere in units.</param>
    /// <param name="elementsToPositionAroundHQ">The non-HQ elements to position.</param>
    /// <returns>
    ///   <c>true</c> if all elements were successfully positioned without overlap.
    /// </returns>
    private bool TryPositionRandomWithinSphere(IElementModel hqElement, float radius, IElementModel[] elementsToPositionAroundHQ) {
        IList<Bounds> allElementBounds = new List<Bounds>();

        Bounds hqElementBounds = new Bounds();
        bool toEncapsulateHqElement = false;
        D.Assert(UnityUtility.GetBoundWithChildren(hqElement.Transform, ref hqElementBounds, ref toEncapsulateHqElement),
            "{0} unable to construct a Bound for HQ Element {1}.".Inject(_unitCmd.FullName, hqElement.FullName));
        allElementBounds.Add(hqElementBounds);

        int iterateCount = 0;
        Vector3[] formationStationOffsets = new Vector3[elementsToPositionAroundHQ.Length];
        for (int i = 0; i < elementsToPositionAroundHQ.Length; i++) {
            bool toEncapsulate = false;
            Vector3 candidateStationOffset = UnityEngine.Random.insideUnitSphere * radius;
            Bounds elementBounds = new Bounds();
            IElementModel element = elementsToPositionAroundHQ[i];
            if (UnityUtility.GetBoundWithChildren(element.Transform, ref elementBounds, ref toEncapsulate)) {
                elementBounds.center = candidateStationOffset;
                //D.Log("Bounds = {0}.", elementBounds.ToString());
                if (allElementBounds.All(eb => !eb.Intersects(elementBounds))) {
                    allElementBounds.Add(elementBounds);
                    formationStationOffsets[i] = candidateStationOffset;
                    iterateCount = 0;
                }
                else {
                    i--;
                    iterateCount++;
                    if (iterateCount >= 10) {
                        D.Warn("{0} had a formation positioning iteration error.", _unitCmd.FullName);
                        return false;
                    }
                }
            }
            else {
                D.Error("{0} unable to construct a Bound for {1}.", _unitCmd.FullName, element.FullName);
                return false;
            }
        }


        _unitCmd.PositionElementInFormation(hqElement, Vector3.zero);
        for (int i = 0; i < elementsToPositionAroundHQ.Length; i++) {
            _unitCmd.PositionElementInFormation(elementsToPositionAroundHQ[i], formationStationOffsets[i]);
            //elementsToPosition[i].transform.localPosition = localFormationPositions[i];   // won't work as the position of the Element's parent is arbitrary
        }
        return true;
    }

    /// <summary>
    /// Positions the elements equidistant in a circle around the HQ Element.
    /// </summary>
    protected void PositionElementsEquidistantInCircle() {
        float circleRadius = (float)Math.Pow(_maxElementCount * 0.2F, 0.5F);  // ~ 2.2

        IElementModel hqElement = _unitCmd.HQElement;
        _unitCmd.PositionElementInFormation(hqElement, Vector3.zero);

        var elementsToPositionInCircle = _unitCmd.Elements.Except(hqElement);
        D.Log("{0}.elementsCount = {1}.", GetType().Name, elementsToPositionInCircle.Count());
        Stack<Vector3> formationStationOffsets = new Stack<Vector3>(Mathfx.UniformPointsOnCircle(circleRadius, elementsToPositionInCircle.Count()));
        foreach (var element in elementsToPositionInCircle) {
            Vector3 stationOffset = formationStationOffsets.Pop();
            _unitCmd.PositionElementInFormation(element, stationOffset);
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

