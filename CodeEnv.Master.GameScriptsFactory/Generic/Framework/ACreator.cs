// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACreator.cs
// COMMENT - one line to give a brief idea of what this file does.
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
/// COMMENT 
/// </summary>
[Obsolete]
public abstract class ACreator : AMonoBase, IDisposable {

    private static bool __isHumanOwnedCreated;

    public int maxElements = 8;

    private string _pieceName;

    private FleetComposition _composition;
    protected IList<ShipItem> _elements;
    protected FleetItem _command;
    private IList<IDisposable> _subscribers;
    private bool _isPreset;

    protected override void Awake() {
        base.Awake();
        _pieceName = gameObject.name;   // the name of the fleet is carried by the name of the FleetMgr gameobject
        _isPreset = _transform.childCount > 0;
        CreateComposition();
        Subscribe();
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(GameManager.Instance.SubscribeToPropertyChanged<GameManager, GameState>(gm => gm.CurrentState, OnGameStateChanged));
    }

    private void OnGameStateChanged() {
        if (GameManager.Instance.CurrentState == GameState.RunningCountdown_2) {
            DeployPiece();
            EnablePiece();  // must make View operational before starting state changes within it
        }
        if (GameManager.Instance.CurrentState == GameState.RunningCountdown_1) {
            __InitializeCommandIntel();
        }
    }

    private void CreateComposition() {
        if (_isPreset) {
            CreateCompositionFromChildren();
        }
        else {
            CreateRandomComposition();
        }
    }

    private void CreateCompositionFromChildren() {
        _elements = gameObject.GetSafeMonoBehaviourComponentsInChildren<ShipItem>();
        IPlayer owner = GameManager.Instance.HumanPlayer;
        __isHumanOwnedCreated = true;
        _composition = new FleetComposition();
        foreach (var element in _elements) {
            ShipCategory elementCategory = DeriveCategory(element);
            string elementName = element.gameObject.name;
            ShipData data = CreateElementData(elementCategory, elementName, owner);
            _composition.Add(data);
        }
    }

    private void CreateRandomComposition() {
        IPlayer owner;
        if (!__isHumanOwnedCreated) {
            owner = GameManager.Instance.HumanPlayer;
            __isHumanOwnedCreated = true;
        }
        else {
            owner = new Player(new Race(Enums<Races>.GetRandom(excludeDefault: true)), IQ.Normal);
        }
        _composition = new FleetComposition();

        ShipCategory[] __elementCategoriesToPickFrom = new ShipCategory[] { ShipCategory.Carrier, ShipCategory.Cruiser, ShipCategory.Destroyer, ShipCategory.Dreadnaught, ShipCategory.Frigate };

        //determine how many ships of what hull for the fleet, then build shipdata and add to composition
        int elementCount = RandomExtended<int>.Range(1, maxElements);
        for (int i = 0; i < elementCount; i++) {
            ShipCategory elementCategory = RandomExtended<ShipCategory>.Choice(__elementCategoriesToPickFrom);
            int nextIndex = GetExistingCount(elementCategory) + 1;
            string uniqueElementName = elementCategory.GetName() + Constants.Underscore + nextIndex;
            ShipData elementData = CreateElementData(elementCategory, uniqueElementName, owner);
            _composition.Add(elementData);
        }
    }

    protected virtual ShipData CreateElementData(ShipCategory elementCategory, string elementInstanceName, IPlayer owner) {
        float mass = TempGameValues.__GetMass(elementCategory);
        float drag = 0.1F;
        ShipData elementData = new ShipData(elementCategory, elementInstanceName, 50F, mass, drag) {
            // Ship's optionalParentName gets set when it gets attached to a fleet
            Strength = new CombatStrength(),
            CurrentHitPoints = UnityEngine.Random.Range(25F, 50F),
            MaxTurnRate = UnityEngine.Random.Range(45F, 315F),
            Owner = owner,
            MaxThrust = mass * drag * UnityEngine.Random.Range(2F, 5F)  // MaxThrust = Mass * Drag * MaxSpeed;
        };
        return elementData;
    }

    private void DeployPiece() {
        if (_isPreset) {
            DeployPresetPiece();
        }
        else {
            DeployRandomPiece();
        }
    }

    private void DeployPresetPiece() {
        InitializeElements();
        _command = gameObject.GetSafeMonoBehaviourComponentInChildren<FleetItem>();
        InitializePiece();
        SelectHQElement();
        AssignFormationPositions();
    }

    private void DeployRandomPiece() {
        BuildElements();
        InitializeElements();
        BuildCommand();
        InitializePiece();
        SelectHQElement();
        //float pieceRadius = 1F * (float)Math.Pow(_elements.Count * 0.2F, 0.33F);  // cube root of number of groups of 5 ships
        //if (!RandomlyPositionFleetElements(fleetGlobeRadius)) {
        //    // try again with a larger radius
        //    D.Assert(RandomlyPositionFleetElements(fleetGlobeRadius * 1.5F), "Fleet Positioning Error");
        //}
        //PositionFleetElementsInCircle(pieceRadius);
        PositionElements();
        AssignFormationPositions();
    }

    private void BuildElements() {
        _elements = new List<ShipItem>();
        foreach (var elementCategory in _composition.ElementCategories) {
            //GameObject elementPrefab = RequiredPrefabs.Instance.ships.First(p => p.gameObject.name == elementCategory.GetName()).gameObject;
            GameObject elementPrefab = GetElementPrefab(elementCategory);
            string prefabName = elementPrefab.name;

            _composition.GetData(elementCategory).ForAll(data => {
                GameObject elementInstanceGo = UnityUtility.AddChild(gameObject, elementPrefab);
                elementInstanceGo.name = prefabName; // get rid of (Clone) in name
                ShipItem elementInstance = elementInstanceGo.GetSafeMonoBehaviourComponent<ShipItem>();
                _elements.Add(elementInstance);

            });
        }
    }

    private void InitializeElements() {
        IDictionary<ShipCategory, Stack<ShipData>> lookup = new Dictionary<ShipCategory, Stack<ShipData>>();
        foreach (var element in _elements) {
            ShipCategory elementCategory = DeriveCategory(element);
            Stack<ShipData> dataStack;
            if (!lookup.TryGetValue(elementCategory, out dataStack)) {
                dataStack = new Stack<ShipData>(_composition.GetData(elementCategory));
                lookup.Add(elementCategory, dataStack);
            }
            element.Data = dataStack.Pop();  // automatically adds the ship's transform to Data when set
        }
    }

    private void BuildCommand() {
        GameObject cmdPrefab = GetCommandPrefab();

        //GameObject commandGo = UnityUtility.AddChild(gameObject, RequiredPrefabs.Instance.fleetCmd.gameObject);
        GameObject commandGo = UnityUtility.AddChild(gameObject, cmdPrefab);
        _command = commandGo.GetSafeMonoBehaviourComponent<FleetItem>();
    }

    private void InitializePiece() {
        _command.Data = new FleetData(_pieceName);    // automatically adds the fleetCmd transform to Data when set

        // add each ship to the fleet
        _elements.ForAll(element => _command.AddElement(element));
        // include FleetCmd as a target in each ship's CameraLOSChangedRelay
        _elements.ForAll(element => element.gameObject.GetSafeMonoBehaviourComponentInChildren<CameraLOSChangedRelay>().AddTarget(_command.transform));
    }

    /// <summary>
    /// Selects and marks the flagship so formation creation knows which ship it is.
    /// Once enabled, the flagship will assign itself to its FleetCmd once it has initialized
    /// its Navigator to receive the immediate callback from FleetCmd.
    /// </summary>
    //private void SelectHQElement() {
    //    RandomExtended<ShipItem>.Choice(_elements).IsHQElement = true;
    //}

    protected abstract void SelectHQElement();

    protected abstract void PositionElements();


    ///// <summary>
    ///// Randomly positions the ships of the fleet in a spherical globe around this location.
    ///// </summary>
    ///// <param name="radius">The radius of the globe within which to deploy the fleet.</param>
    ///// <returns></returns>
    //private bool RandomlyPositionFleetElements(float radius) {  // FIXME need to set FormationPosition
    //    GameObject[] shipGos = _elements.Select(s => s.gameObject).ToArray();
    //    Vector3 fleetCenter = _transform.position;
    //    D.Log("Radius of Sphere occupied by Fleet of {0} is {1}.", shipGos.Length, radius);
    //    return UnityUtility.PositionRandomWithinSphere(fleetCenter, radius, shipGos);
    //    // fleetCmd will relocate itsef once it selects its flagship
    //}

    //private void PositionFleetElementsInCircle(float radius) {
    //    Vector3 fleetCenter = _transform.position;
    //    Stack<Vector3> localFormationPositions = new Stack<Vector3>(Mathfx.UniformPointsOnCircle(radius, _elements.Count - 1));
    //    foreach (var ship in _elements) {
    //        if (ship.IsHQElement) {
    //            ship.transform.position = fleetCenter;
    //        }
    //        else {
    //            Vector3 localFormationPosition = localFormationPositions.Pop();
    //            ship.transform.position = fleetCenter + localFormationPosition;
    //        }
    //    }
    //}

    private void AssignFormationPositions() {
        ShipItem hqElement = _elements.Single(s => s.IsHQElement);
        Vector3 hqElementPosition = hqElement.transform.position;
        foreach (var element in _elements) {
            if (element.IsHQElement) {
                element.Data.FormationPosition = Vector3.zero;
                D.Log("{0} HQ Element is {1}.", _pieceName, element.Data.Name);
                continue;
            }
            element.Data.FormationPosition = element.transform.position - hqElementPosition;
            //D.Log("{0}.FormationPosition = {1}.", ship.Data.Name, ship.Data.FormationPosition);
        }
    }


    //private void __InitializeCommandIntel() {
    //    _command.gameObject.GetSafeInterface<IFleetViewable>().PlayerIntel = new Intel(IntelScope.Comprehensive, IntelSource.InfoNet);
    //    //RandomExtended<IntelLevel>.Choice(Enums<IntelLevel>.GetValues().Except(default(IntelLevel), IntelLevel.Nil).ToArray());
    //}

    protected abstract void __InitializeCommandIntel();

    private void EnablePiece() {
        // ships need to run their Start first to initialize their navigators and assign the flagship to fleetCmd before fleetCmd is enabled and runs its Start
        _elements.ForAll(ship => ship.enabled = true);
        _command.enabled = true;
        EnableViews();
        //_elements.ForAll(ship => ship.gameObject.GetSafeMonoBehaviourComponent<ShipView>().enabled = true);
        //_command.gameObject.GetSafeMonoBehaviourComponent<FleetView>().enabled = true;
    }

    protected abstract void EnableViews();

    private ShipCategory DeriveCategory(ShipItem element) {
        return Enums<ShipCategory>.Parse(element.gameObject.name);
    }

    private int GetExistingCount(ShipCategory elementCategory) {
        if (!_composition.ElementCategories.Contains(elementCategory)) {
            return 0;
        }
        return _composition.GetData(elementCategory).Count;
    }

    protected abstract GameObject GetElementPrefab(ShipCategory elementCategory);

    protected abstract GameObject GetCommandPrefab();

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        Unsubscribe();
        // other cleanup here including any tracking Gui2D elements
    }

    private void Unsubscribe() {
        _subscribers.ForAll(d => d.Dispose());
        _subscribers.Clear();
    }

    #region IDisposable
    [DoNotSerialize]
    private bool alreadyDisposed = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    /// </summary>
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            Cleanup();
        }
        // free unmanaged resources here

        alreadyDisposed = true;
    }

    // Example method showing check for whether the object has been disposed
    //public void ExampleMethod() {
    //    // throw Exception if called on object that is already disposed
    //    if(alreadyDisposed) {
    //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    //    }

    //    // method content here
    //}
    #endregion

}

