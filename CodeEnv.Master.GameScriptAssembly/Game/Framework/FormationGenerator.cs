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

#define DEBUG_LOG
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

    public FormationGenerator(AUnitCommandModel unitCmd) {
        _unitCmd = unitCmd;
    }

    /// <summary>
    /// Generates a new formation based on the formation selected and the 
    /// number of elements present in the Unit.
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    public void RegenerateFormation() {
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
        float globeRadius = 1F * (float)Math.Pow(_unitCmd.Elements.Count * 0.2F, 0.33F);  // cube root of number of groups of 5 elements

        var elementsToPosition = _unitCmd.Elements.Except(_unitCmd.HQElement).ToArray();
        if (!TryPositionRandomWithinSphere(_unitCmd.HQElement, globeRadius, ref elementsToPosition)) {
            // try again with a larger radius
            D.Assert(TryPositionRandomWithinSphere(_unitCmd.HQElement, globeRadius * 1.5F, ref elementsToPosition),
                "{0} Formation Positioning Error.".Inject(_unitCmd.Data.Name));
        }
    }

    /// <summary>
    /// Positions the provided game objects randomly inside a sphere in such a way that the meshes
    /// are not in contact.
    /// </summary>
    /// <param name="hqElement">The hq element with FormationPosition fixed at Vector3.zero.</param>
    /// <param name="radius">The radius of the sphere in units.</param>
    /// <param name="elementsToPosition">The non-HQ elements to position.</param>
    /// <returns>
    ///   <c>true</c> if all elements were successfully positioned without overlap.
    /// </returns>
    private bool TryPositionRandomWithinSphere(IElementModel hqElement, float radius, ref IElementModel[] elementsToPosition) {
        IList<Bounds> allElementBounds = new List<Bounds>();

        Bounds hqElementBounds = new Bounds();
        bool toEncapsulateHqElement = false;
        D.Assert(UnityUtility.GetBoundWithChildren(hqElement.Transform, ref hqElementBounds, ref toEncapsulateHqElement),
            "{0} unable to construct a Bound for HQ Element {1}.".Inject(_unitCmd.Name, hqElement.Name));
        allElementBounds.Add(hqElementBounds);

        int iterateCount = 0;
        Vector3[] formationStationOffsets = new Vector3[elementsToPosition.Length];
        for (int i = 0; i < elementsToPosition.Length; i++) {
            bool toEncapsulate = false;
            Vector3 candidateStationOffset = UnityEngine.Random.insideUnitSphere * radius;
            Bounds elementBounds = new Bounds();
            IElementModel element = elementsToPosition[i];
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
                        D.Warn("{0} had a formation positioning iteration error.", _unitCmd.Name);
                        return false;
                    }
                }
            }
            else {
                D.Error("{0} unable to construct a Bound for {1}.", _unitCmd.Name, element.Name);
                return false;
            }
        }
        for (int i = 0; i < elementsToPosition.Length; i++) {
            _unitCmd.PositionElementInFormation(elementsToPosition[i], formationStationOffsets[i]);
            //elementsToPosition[i].transform.localPosition = localFormationPositions[i];   // won't work as the position of the Element's parent is arbitrary
        }
        return true;
    }

    /// <summary>
    /// Positions the elements equidistant in a circle around the HQ Element.
    /// </summary>
    protected void PositionElementsEquidistantInCircle() {
        float globeRadius = 1F * (float)Math.Pow(_unitCmd.Elements.Count * 0.2F, 0.33F);  // cube root of number of groups of 5 elements

        Vector3 hqElementPosition = _unitCmd.HQElement.Data.Position;
        var elementsToPosition = _unitCmd.Elements.Except(_unitCmd.HQElement);
        //D.Log("{0}.elementsCount = {1}.", GetType().Name, _elements.Count);
        Stack<Vector3> formationStationOffsets = new Stack<Vector3>(Mathfx.UniformPointsOnCircle(globeRadius, elementsToPosition.Count()));
        foreach (var element in elementsToPosition) {
            Vector3 stationOffset = formationStationOffsets.Pop();
            _unitCmd.PositionElementInFormation(element, stationOffset);
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

