// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCreator.cs
// Initialization class that deploys a Starbase at the location of this StarbaseCreator.
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
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Initialization class that deploys a Starbase at the location of this StarbaseCreator. 
/// </summary>
public class StarbaseCreator : ACreator<FacilityItem, StarbaseItem, StarbaseComposition> {

    //private static bool __isHumanVersionCreated;

    //public int maxElements = 8;

    //private string _pieceName;

    //private StarbaseComposition _composition;
    //private IList<FacilityItem> _elements;
    //private StarbaseItem _command;
    //private IList<IDisposable> _subscribers;
    //private bool _isExisting;

    //protected override void Awake() {
    //    base.Awake();
    //    _pieceName = gameObject.name;   // the name of the fleet is carried by the name of the FleetMgr gameobject
    //    _isExisting = _transform.childCount > 0;
    //    CreateComposition();
    //    Subscribe();
    //}

    //private void Subscribe() {
    //    if (_subscribers == null) {
    //        _subscribers = new List<IDisposable>();
    //    }
    //    _subscribers.Add(GameManager.Instance.SubscribeToPropertyChanged<GameManager, GameState>(gm => gm.CurrentState, OnGameStateChanged));
    //}

    //private void OnGameStateChanged() {
    //    if (GameManager.Instance.CurrentState == GameState.RunningCountdown_2) {
    //        DeployPiece();
    //        EnablePiece();  // must make View operational before starting state changes within it
    //    }
    //    if (GameManager.Instance.CurrentState == GameState.RunningCountdown_1) {
    //        __SetIntelLevel();
    //    }
    //}

    //private void CreateComposition() {
    //    if (_isExisting) {
    //        CreateCompositionFromChildren();
    //    }
    //    else {
    //        CreateRandomComposition();
    //    }
    //}

    protected override void CreateCompositionFromChildren() {
        _elements = gameObject.GetSafeMonoBehaviourComponentsInChildren<FacilityItem>();
        IPlayer owner = GameManager.Instance.HumanPlayer;
        __isHumanOwnedCreated = true;
        _composition = new StarbaseComposition();
        foreach (var element in _elements) {
            FacilityCategory category = DeriveCategory(element);
            string elementName = element.gameObject.name;
            FacilityData elementData = CreateElementData(category, elementName, owner);
            _composition.Add(elementData);
        }
    }

    //private void CreateCompositionFromChildren() {
    //    _elements = gameObject.GetSafeMonoBehaviourComponentsInChildren<FacilityItem>();
    //    IPlayer owner = GameManager.Instance.HumanPlayer;
    //    __isHumanVersionCreated = true;
    //    _composition = new StarbaseComposition();
    //    foreach (var ship in _elements) {
    //        FacilityCategory hull = DeriveCategory(ship);
    //        string shipName = ship.gameObject.name;
    //        FacilityData data = CreateElementData(hull, shipName, owner);
    //        _composition.Add(data);
    //    }
    //}

    protected override void CreateRandomComposition() {
        IPlayer owner;
        if (!__isHumanOwnedCreated) {
            owner = GameManager.Instance.HumanPlayer;
            __isHumanOwnedCreated = true;
        }
        else {
            owner = new Player(new Race(Enums<Races>.GetRandom(excludeDefault: true)), IQ.Normal);
        }
        _composition = new StarbaseComposition();

        FacilityCategory[] validCategories = new FacilityCategory[] { FacilityCategory.Construction, FacilityCategory.Economic, FacilityCategory.Science, FacilityCategory.Defense };

        int elementCount = RandomExtended<int>.Range(1, maxElements);
        for (int i = 0; i < elementCount; i++) {
            FacilityCategory elementCategory = (i == 0) ? FacilityCategory.CentralHub : RandomExtended<FacilityCategory>.Choice(validCategories);
            int elementInstanceIndex = GetCurrentCount(elementCategory) + 1;
            string elementInstanceName = elementCategory.GetName() + Constants.Underscore + elementInstanceIndex;
            FacilityData elementData = CreateElementData(elementCategory, elementInstanceName, owner);
            _composition.Add(elementData);
        }

    }

    //private void CreateRandomComposition() {
    //    IPlayer owner;
    //    if (!__isHumanVersionCreated) {
    //        owner = GameManager.Instance.HumanPlayer;
    //        __isHumanVersionCreated = true;
    //    }
    //    else {
    //        owner = new Player(new Race(Enums<Races>.GetRandom(excludeDefault: true)), IQ.Normal);
    //    }
    //    _composition = new StarbaseComposition();

    //    FacilityCategory[] __hullsToConsider = new FacilityCategory[] { FacilityCategory.CentralHub, FacilityCategory.Construction, FacilityCategory.Economic, FacilityCategory.Science, FacilityCategory.Defense };

    //    //determine how many ships of what hull for the fleet, then build shipdata and add to composition
    //    int shipCount = RandomExtended<int>.Range(1, maxElements);
    //    for (int i = 0; i < shipCount; i++) {
    //        FacilityCategory hull = RandomExtended<FacilityCategory>.Choice(__hullsToConsider);
    //        int shipHullIndex = GetShipHullIndex(hull);
    //        string shipName = hull.GetName() + Constants.Underscore + shipHullIndex;
    //        FacilityData shipData = CreateElementData(hull, shipName, owner);
    //        _composition.Add(shipData);
    //    }
    //}

    private FacilityData CreateElementData(FacilityCategory elementCategory, string elementInstanceName, IPlayer owner) {
        FacilityData elementData = new FacilityData(elementCategory, elementInstanceName, maxHitPoints: 50F, mass: 10000F) {   // TODO mass variation
            // optionalParentName gets set when it gets attached to a command
            Strength = new CombatStrength(),
            CurrentHitPoints = UnityEngine.Random.Range(25F, 50F),
            Owner = owner,
        };
        return elementData;
    }

    //private void DeployPiece() {
    //    if (_isExisting) {
    //        DeployExistingPiece();
    //    }
    //    else {
    //        DeployRandomPiece();
    //    }
    //}

    //private void DeployExistingPiece() {
    //    InitializeElements();
    //    _command = gameObject.GetSafeMonoBehaviourComponentInChildren<StarbaseItem>();
    //    InitializePiece();
    //    SelectHQElement();
    //    AssignFormationPositions();
    //}

    //private void DeployRandomPiece() {
    //    BuildElements();
    //    InitializeElements();
    //    BuildCommand();
    //    InitializePiece();
    //    SelectHQElement();
    //    float fleetGlobeRadius = 1F * (float)Math.Pow(_elements.Count * 0.2F, 0.33F);  // cube root of number of groups of 5 ships
    //    //if (!RandomlyPositionFleetElements(fleetGlobeRadius)) {
    //    //    // try again with a larger radius
    //    //    D.Assert(RandomlyPositionFleetElements(fleetGlobeRadius * 1.5F), "Fleet Positioning Error");
    //    //}
    //    PositionElementsInCircle(fleetGlobeRadius);
    //    AssignFormationPositions();
    //}

    protected override GameObject GetCommandPrefab() {
        return RequiredPrefabs.Instance.starbaseCmd.gameObject;
    }

    protected override void BuildElements() {
        _elements = new List<FacilityItem>();
        foreach (var elementCategory in _composition.Categories) {
            GameObject elementPrefabGo = RequiredPrefabs.Instance.facilities.First(f => f.gameObject.name == elementCategory.GetName()).gameObject;
            string elementCategoryName = elementPrefabGo.name;

            _composition.GetData(elementCategory).ForAll(data => {
                GameObject elementGoClone = UnityUtility.AddChild(gameObject, elementPrefabGo);
                elementGoClone.name = elementCategoryName; // get rid of (Clone) in name
                FacilityItem element = elementGoClone.GetSafeMonoBehaviourComponent<FacilityItem>();
                _elements.Add(element);
            });
        }
    }

    //private void BuildElements() {
    //    _elements = new List<FacilityItem>();
    //    foreach (var hull in _composition.Categories) {
    //        GameObject hullPrefab = RequiredPrefabs.Instance.ships.First(p => p.gameObject.name == hull.GetName()).gameObject;
    //        string hullName = hullPrefab.name;

    //        _composition.GetData(hull).ForAll(sd => {
    //            GameObject shipGo = UnityUtility.AddChild(gameObject, hullPrefab);
    //            shipGo.name = hullName; // get rid of (Clone) in name
    //            FacilityItem ship = shipGo.GetSafeMonoBehaviourComponent<FacilityItem>();
    //            _elements.Add(ship);

    //        });
    //    }
    //}

    protected override void InitializeElements() {
        IDictionary<FacilityCategory, Stack<FacilityData>> dataStackLookup = new Dictionary<FacilityCategory, Stack<FacilityData>>();
        foreach (var element in _elements) {
            FacilityCategory elementCategory = DeriveCategory(element);
            Stack<FacilityData> elementDataStack;
            if (!dataStackLookup.TryGetValue(elementCategory, out elementDataStack)) {
                elementDataStack = new Stack<FacilityData>(_composition.GetData(elementCategory));
                dataStackLookup.Add(elementCategory, elementDataStack);
            }
            element.Data = elementDataStack.Pop();  // automatically adds the element's transform to Data when set
        }
    }

    //private void InitializeElements() {
    //    IDictionary<FacilityCategory, Stack<FacilityData>> typeLookup = new Dictionary<FacilityCategory, Stack<FacilityData>>();
    //    foreach (var ship in _elements) {
    //        FacilityCategory hull = DeriveCategory(ship);
    //        Stack<FacilityData> dataStack;
    //        if (!typeLookup.TryGetValue(hull, out dataStack)) {
    //            dataStack = new Stack<FacilityData>(_composition.GetData(hull));
    //            typeLookup.Add(hull, dataStack);
    //        }
    //        ship.Data = dataStack.Pop();  // automatically adds the ship's transform to Data when set
    //    }
    //}

    //private void BuildCommand() {
    //    GameObject fleetCmdGo = UnityUtility.AddChild(gameObject, RequiredPrefabs.Instance.fleetCmd.gameObject);
    //    _command = fleetCmdGo.GetSafeMonoBehaviourComponent<StarbaseItem>();
    //}

    protected override void InitializePiece() {
        _command.Data = new StarbaseData(_pieceName);    // automatically adds the command transform to Data when set
        _elements.ForAll(element => _command.AddElement(element));
        // include command as a target in each element's CameraLOSChangedRelay
        _elements.ForAll(element => element.gameObject.GetSafeMonoBehaviourComponentInChildren<CameraLOSChangedRelay>().AddTarget(_command.transform));
    }

    //private void InitializePiece() {
    //    _command.Data = new StarbaseData(_pieceName);    // automatically adds the fleetCmd transform to Data when set

    //    // add each ship to the fleet
    //    _elements.ForAll(ship => _command.AddElement(ship));
    //    // include FleetCmd as a target in each ship's CameraLOSChangedRelay
    //    _elements.ForAll(ship => ship.gameObject.GetSafeMonoBehaviourComponentInChildren<CameraLOSChangedRelay>().AddTarget(_command.transform));
    //}

    protected override void MarkHQElement() {
        _elements.Single(e => (e.Data as FacilityData).Category == FacilityCategory.CentralHub).IsHQElement = true;
    }

    /// <summary>
    /// Selects and marks the flagship so formation creation knows which ship it is.
    /// Once enabled, the flagship will assign itself to its FleetCmd once it has initialized
    /// its Navigator to receive the immediate callback from FleetCmd.
    /// </summary>
    //private void SelectHQElement() {
    //    RandomExtended<FacilityItem>.Choice(_elements).IsHQElement = true;
    //}

    protected override void PositionElements() {
        float globeRadius = 1F * (float)Math.Pow(_elements.Count * 0.2F, 0.33F);  // cube root of number of groups of 5 elements
        PositionElementsEquidistantInCircle(globeRadius);

        //if (!PositionElementsRandomlyInSphere(globeRadius)) {
        //    // try again with a larger radius
        //    D.Assert(PositionElementsRandomlyInSphere(globeRadius * 1.5F), "{0} Positioning Error.".Inject(_pieceName));
        //}
    }

    ///// <summary>
    ///// Randomly positions the ships of the fleet in a spherical globe around this location.
    ///// </summary>
    ///// <param name="radius">The radius of the globe within which to deploy the fleet.</param>
    ///// <returns></returns>
    //private bool PositionElementsRandomlyInSphere(float radius) {  // FIXME need to set FormationPosition
    //    GameObject[] elementGos = _elements.Select(s => s.gameObject).ToArray();
    //    Vector3 pieceCenter = _transform.position;
    //    D.Log("Radius of Sphere occupied by {0} of count {1} is {2}.", _pieceName, elementGos.Length, radius);
    //    return UnityUtility.PositionRandomWithinSphere(pieceCenter, radius, elementGos);
    //    // fleetCmd will relocate itsef once it selects its flagship
    //}

    //private void PositionElementsEquidistantInCircle(float radius) {
    //    Vector3 pieceCenter = _transform.position;
    //    Stack<Vector3> localFormationPositions = new Stack<Vector3>(Mathfx.UniformPointsOnCircle(radius, _elements.Count - 1));
    //    foreach (var element in _elements) {
    //        if (element.IsHQElement) {
    //            element.transform.position = pieceCenter;
    //        }
    //        else {
    //            Vector3 localFormationPosition = localFormationPositions.Pop();
    //            element.transform.position = pieceCenter + localFormationPosition;
    //        }
    //    }
    //}

    protected override void AssignFormationPositions() {
        FacilityItem hqElement = _elements.Single(s => s.IsHQElement);
        Vector3 hqPosition = hqElement.transform.position;
        foreach (var element in _elements) {
            if (element.IsHQElement) {
                element.Data.FormationPosition = Vector3.zero;
                D.Log("HQ Element is {0}.", element.Data.Name);
                continue;
            }
            element.Data.FormationPosition = element.transform.position - hqPosition;
            //D.Log("{0}.FormationPosition = {1}.", ship.Data.Name, ship.Data.FormationPosition);
        }
    }

    //private void AssignFormationPositions() {
    //    FacilityItem flagship = _elements.Single(s => s.IsHQElement);
    //    Vector3 flagshipPosition = flagship.transform.position;
    //    foreach (var ship in _elements) {
    //        if (ship.IsHQElement) {
    //            ship.Data.FormationPosition = Vector3.zero;
    //            D.Log("Flagship is {0}.", ship.Data.Name);
    //            continue;
    //        }
    //        ship.Data.FormationPosition = ship.transform.position - flagshipPosition;
    //        //D.Log("{0}.FormationPosition = {1}.", ship.Data.Name, ship.Data.FormationPosition);
    //    }
    //}

    protected override void __InitializeCommandIntel() {
        _command.gameObject.GetSafeInterface<ICommandViewable>().PlayerIntel = new Intel(IntelScope.Comprehensive, IntelSource.InfoNet);
    }

    //private void __SetIntelLevel() {
    //    _command.gameObject.GetSafeInterface<ICommandViewable>().PlayerIntel = new Intel(IntelScope.Comprehensive, IntelSource.InfoNet);
    //    //_fleetCmd.gameObject.GetSafeInterface<IStarbaseViewable>().PlayerIntel = new Intel(IntelScope.Comprehensive, IntelSource.InfoNet);
    //    //RandomExtended<IntelLevel>.Choice(Enums<IntelLevel>.GetValues().Except(default(IntelLevel), IntelLevel.Nil).ToArray());
    //}

    protected override void EnableViews() {
        _elements.ForAll(e => e.gameObject.GetSafeMonoBehaviourComponent<FacilityView>().enabled = true);
        _command.gameObject.GetSafeMonoBehaviourComponent<StarbaseView>().enabled = true;
    }

    //private void EnablePiece() {
    //    // ships need to run their Start first to initialize their navigators and assign the flagship to fleetCmd before fleetCmd is enabled and runs its Start
    //    _elements.ForAll(ship => ship.enabled = true);
    //    _command.enabled = true;
    //    _elements.ForAll(ship => ship.gameObject.GetSafeMonoBehaviourComponent<FacilityView>().enabled = true);
    //    _command.gameObject.GetSafeMonoBehaviourComponent<StarbaseView>().enabled = true;
    //}

    private FacilityCategory DeriveCategory(FacilityItem element) {
        return Enums<FacilityCategory>.Parse(element.gameObject.name);
    }

    private int GetCurrentCount(FacilityCategory elementCategory) {
        if (!_composition.Categories.Contains(elementCategory)) {
            return 0;
        }
        return _composition.GetData(elementCategory).Count;
    }

    //protected override void OnDestroy() {
    //    base.OnDestroy();
    //    Dispose();
    //}

    //private void Cleanup() {
    //    Unsubscribe();
    //    // other cleanup here including any tracking Gui2D elements
    //}

    //private void Unsubscribe() {
    //    _subscribers.ForAll(d => d.Dispose());
    //    _subscribers.Clear();
    //}

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    //#region IDisposable
    //[DoNotSerialize]
    //private bool alreadyDisposed = false;

    ///// <summary>
    ///// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    ///// </summary>
    //public void Dispose() {
    //    Dispose(true);
    //    GC.SuppressFinalize(this);
    //}

    ///// <summary>
    ///// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    ///// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    ///// </summary>
    ///// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    //protected virtual void Dispose(bool isDisposing) {
    //    // Allows Dispose(isDisposing) to be called more than once
    //    if (alreadyDisposed) {
    //        return;
    //    }

    //    if (isDisposing) {
    //        // free managed resources here including unhooking events
    //        Cleanup();
    //    }
    //    // free unmanaged resources here

    //    alreadyDisposed = true;
    //}

    //// Example method showing check for whether the object has been disposed
    ////public void ExampleMethod() {
    ////    // throw Exception if called on object that is already disposed
    ////    if(alreadyDisposed) {
    ////        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    ////    }

    ////    // method content here
    ////}
    //#endregion

}

