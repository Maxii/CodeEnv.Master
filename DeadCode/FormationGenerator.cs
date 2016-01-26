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
[System.Obsolete]
public class FormationGenerator {

    private static float _oneThird = 1F / 3F;
    private static float _fourThirds = 4F / 3F;

    private AUnitCmdItem _unitCmd;

    public FormationGenerator(AUnitCmdItem unitCmd) {
        _unitCmd = unitCmd;
    }

    /// <summary>
    /// Repositions all elements in the Unit in the formation specified by the Unit.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public void RepositionElementsInFormation() {
        D.Assert(_unitCmd.HQElement != null, "{0} does not have a HQ Element needed to generate a formation.", _unitCmd.FullName);
        D.Log("{0} is about to reposition its elements in formation {1}.", _unitCmd.Data.ParentName, _unitCmd.Data.UnitFormation.GetValueName());

        int maxElementsPerUnit = TempGameValues.MaxFacilitiesPerBase;
        float maxElementCollisionAvoidanceZoneRadius = TempGameValues.LargestFacilityObstacleZoneRadius;
        if (_unitCmd is IFleetCmdItem) {
            maxElementsPerUnit = TempGameValues.MaxShipsPerFleet;
            maxElementCollisionAvoidanceZoneRadius = TempGameValues.LargestShipCollisionDetectionZoneRadius;
        }
        float unitFormationRadius;
        switch (_unitCmd.Data.UnitFormation) {
            case Formation.Circle:
                unitFormationRadius = PositionElementsInCircleFormation(maxElementCollisionAvoidanceZoneRadius, maxElementsPerUnit);
                break;
            case Formation.Globe:
                unitFormationRadius = PositionElementsRandomlyInGlobe(maxElementCollisionAvoidanceZoneRadius, maxElementsPerUnit);
                break;
            case Formation.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_unitCmd.Data.UnitFormation));
        }
        _unitCmd.Data.UnitFormationRadius = unitFormationRadius;
        D.Log("{0} UnitFormationRadius set to {1:0.#}.", _unitCmd.FullName, unitFormationRadius);
        _unitCmd.CleanupAfterFormationGeneration();
    }

    /// <summary>
    /// Randomly positions the elements of the Unit in a spherical globe around the HQ Element. Returns the
    /// radius of the UnitFormation making sure all keepout zones are contained within that radius when the
    /// element is OnStation.
    /// </summary>
    /// <param name="maxKeepoutRadius">The maximum 'keepout' radius of the element type. For a ship, this is the
    /// radius of the CollisionDetectionZone around the largest ship. For a facility, it is the radius of the
    /// AvoidableObstacleZone around the largest facility.</param>
    /// <param name="maxElements">The maximum number of elements allowed in a Unit.</param>
    /// <returns></returns>
    private float PositionElementsRandomlyInGlobe(float maxKeepoutRadius, int maxElements) {
        float maxElementSphereVolume = _fourThirds * Mathf.PI * Mathf.Pow(maxKeepoutRadius, 3F);
        float minPossibleGlobeVolume = maxElements * maxElementSphereVolume / 0.6F; // from marbles in a spherical bowl, aka UnitSizeAndDistance.txt
        float minPossibleGlobeRadius = Mathf.Pow(minPossibleGlobeVolume / (_fourThirds * Mathf.PI), _oneThird);

        float estGlobeRadius = minPossibleGlobeRadius * 1.5F;

        var hqElement = _unitCmd.HQElement;
        var elementsToPositionAroundHQ = _unitCmd.Elements.Except(hqElement).ToArray();
        if (!TryPositionRandomWithinSphere(estGlobeRadius, hqElement, elementsToPositionAroundHQ)) {
            // try again with a larger radius
            estGlobeRadius *= 1.5F;
            D.Assert(TryPositionRandomWithinSphere(estGlobeRadius, hqElement, elementsToPositionAroundHQ),
                "{0}.{1} Positioning Error.", _unitCmd.FullName, GetType().Name);
        }
        return estGlobeRadius + maxKeepoutRadius;
    }

    /// <summary>
    /// Positions the provided elements randomly inside a sphere surrounding the HQ element 
    /// in such a way that the CollisionAvoidanceZones are not in contact.
    /// </summary>
    /// <param name="radius">The radius of the sphere in units.</param>
    /// <param name="hqElement">The hq element.</param>
    /// <param name="elementsToPositionAroundHQ">The non-HQ elements to position.</param>
    /// <returns>
    ///   <c>true</c> if all elements were successfully positioned without overlap.
    /// </returns>
    private bool TryPositionRandomWithinSphere(float radius, AUnitElementItem hqElement, AUnitElementItem[] elementsToPositionAroundHQ) {
        IList<ElementSphere> allElementSpheres = new List<ElementSphere>();

        ElementSphere hqElementSphere = new ElementSphere(hqElement);
        allElementSpheres.Add(hqElementSphere);

        int iterateCount = 0;
        Vector3[] formationStationOffsets = new Vector3[elementsToPositionAroundHQ.Length];
        for (int i = 0; i < elementsToPositionAroundHQ.Length; i++) {
            Vector3 candidateStationOffset = UnityEngine.Random.insideUnitSphere * radius;
            AUnitElementItem elementCandidate = elementsToPositionAroundHQ[i];
            ElementSphere elementCandidateSphere = new ElementSphere(elementCandidate);
            elementCandidateSphere.Center = candidateStationOffset;
            if (allElementSpheres.All(es => !Intersects(es, elementCandidateSphere))) {
                // candidate doesn't intersect with any spheres already present
                allElementSpheres.Add(elementCandidateSphere);
                formationStationOffsets[i] = candidateStationOffset;
                iterateCount = Constants.Zero;
            }
            else {
                i--;
                iterateCount++;
                if (iterateCount >= 10) {    // HACK
                    D.Warn("{0}.{1} had a positioning iteration error.", _unitCmd.FullName, GetType().Name);
                    return false;
                }
            }
        }

        _unitCmd.PositionElementInFormation(hqElement, Vector3.zero);
        for (int i = 0; i < elementsToPositionAroundHQ.Length; i++) {
            _unitCmd.PositionElementInFormation(elementsToPositionAroundHQ[i], formationStationOffsets[i]);
            //elementsToPosition[i].transform.localPosition = localFormationPositions[i];   // won't work as the position of the Element's parent is arbitrary
        }
        return true;
    }

    #region Bounds of the Rendered Hull Archive

    //private bool TryPositionRandomWithinSphere(AUnitElementItem hqElement, float radius, AUnitElementItem[] elementsToPositionAroundHQ) {
    //    IList<Bounds> allElementBounds = new List<Bounds>();

    //    Bounds hqElementBounds = new Bounds();
    //    bool toEncapsulateHqElement = false;
    //    D.Assert(UnityUtility.GetBoundWithChildren(hqElement.transform, ref hqElementBounds, ref toEncapsulateHqElement),
    //        "{0} unable to construct a Bound for HQ Element {1}.".Inject(_unitCmd.FullName, hqElement.FullName));
    //    allElementBounds.Add(hqElementBounds);

    //    int iterateCount = 0;
    //    Vector3[] formationStationOffsets = new Vector3[elementsToPositionAroundHQ.Length];
    //    for (int i = 0; i < elementsToPositionAroundHQ.Length; i++) {
    //        bool toEncapsulate = false;
    //        Vector3 candidateStationOffset = UnityEngine.Random.insideUnitSphere * radius;
    //        Bounds elementBounds = new Bounds();
    //        AUnitElementItem element = elementsToPositionAroundHQ[i];
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
    //                    D.Warn("{0} had a formation positioning iteration error.", _unitCmd.FullName);
    //                    return false;
    //                }
    //            }
    //        }
    //        else {
    //            D.Error("{0} unable to construct a Bound for {1}.", _unitCmd.FullName, element.FullName);
    //            return false;
    //        }
    //    }

    //    _unitCmd.PositionElementInFormation(hqElement, Vector3.zero);
    //    for (int i = 0; i < elementsToPositionAroundHQ.Length; i++) {
    //        _unitCmd.PositionElementInFormation(elementsToPositionAroundHQ[i], formationStationOffsets[i]);
    //        //elementsToPosition[i].transform.localPosition = localFormationPositions[i];   // won't work as the position of the Element's parent is arbitrary
    //    }
    //    return true;
    //}

    #endregion

    /// <summary>
    /// Positions the unit's elements in a circle formation, returning the resulting UnitRadius.
    /// <remarks>The UnitRadius for both fleets and bases is a fixed value, calculated in this method
    /// from the specified max number of elements allowed in a unit and the max UnavoidableObstacleZone radius
    /// for the element type, aka ship vs facility. This value is not the same as the radius
    /// of the circle as the UnitRadius accounts for the UnavoidableObstacleZone of each element.</remarks>
    /// </summary>
    /// <param name="maxCollisionAvoidanceZoneRadius">The maximum collision avoidance zone radius of the element type.</param>
    /// <param name="maxElements">The maximum number of allowed elements per Unit.</param>
    /// <returns>The resulting UnitFormationRadius</returns>
    private float PositionElementsInCircleFormation(float maxCollisionAvoidanceZoneRadius, int maxElements) {
        var hqElement = _unitCmd.HQElement;
        _unitCmd.PositionElementInFormation(hqElement, Vector3.zero);

        float maxCollisionAvoidanceZoneDiameter = maxCollisionAvoidanceZoneRadius * 2F;
        int reqdNumberOfPositionsOnCircle = maxElements - Constants.One;    // excluding HQ
        float circleCircumferenceFromLineSeqments = reqdNumberOfPositionsOnCircle * maxCollisionAvoidanceZoneDiameter;
        float estReqdCircumference = circleCircumferenceFromLineSeqments * 1.2F;    // HACK
        float estCircleRadius = estReqdCircumference / (Mathf.PI * 2F);
        float minCircleRadius = maxCollisionAvoidanceZoneRadius * 2F;   // HQ and an element on circle
        float reqdCircleRadius = Mathf.Max(minCircleRadius, estCircleRadius);
        float unitFormationRadius = reqdCircleRadius + maxCollisionAvoidanceZoneRadius;

        IEnumerable<AUnitElementItem> elementsToPositionOnCircle = _unitCmd.Elements.Except(hqElement);
        if (elementsToPositionOnCircle.Count() == Constants.Zero) {
            return unitFormationRadius;
        }

        Vector3[] localPositionsOnCircle = MyMath.UniformPointsOnCircle(reqdCircleRadius, reqdNumberOfPositionsOnCircle);
        var randomlyOrderedLocalPositionsOnCircle = localPositionsOnCircle.Shuffle();
        Stack<Vector3> formationStationOffsets = new Stack<Vector3>(randomlyOrderedLocalPositionsOnCircle);
        foreach (var element in elementsToPositionOnCircle) {
            Vector3 stationOffset = formationStationOffsets.Pop();
            _unitCmd.PositionElementInFormation(element, stationOffset);
        }
        return unitFormationRadius;
    }

    private bool Intersects(ElementSphere sphereA, ElementSphere sphereB) {
        return MyMath.DoSpheresIntersect(sphereA.Center, sphereA.Radius, sphereB.Center, sphereB.Radius);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Nested Classes

    private class ElementSphere {

        public Vector3 Center { get; set; }

        public float Radius { get; private set; }

        public ElementSphere(AUnitElementItem element) {
            Center = element.Position;
            Radius = element.KeepoutZoneRadius;
        }
    }


    #endregion

}

