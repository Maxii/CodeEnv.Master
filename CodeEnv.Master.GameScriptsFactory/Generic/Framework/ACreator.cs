// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACreator.cs
//  Abstract, generic base class for Element/Command Creators.
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
/// Abstract, generic base class for Element/Command Creators.
/// </summary>
/// <typeparam name="ElementType">The Type of Element contained in the Command.</typeparam>
/// <typeparam name="ElementCategoryType">The Type holding the Element's Categories.</typeparam>
/// <typeparam name="ElementDataType">The Type of the Element's Data.</typeparam>
/// <typeparam name="CommandType">The Type of the Command.</typeparam>
/// <typeparam name="CompositionType">The Type of the Composition within the Command.</typeparam>
public abstract class ACreator<ElementType, ElementCategoryType, ElementDataType, CommandType, CompositionType> : AMonoBase, IDisposable
    where ElementType : AElement
    where ElementCategoryType : struct
    where ElementDataType : AElementData
    where CommandType : ACommandItem<ElementType>
    where CompositionType : class, new() {

    public bool IsCompleted { get; private set; }

    protected static bool __isHumanOwnedCreated;

    public int maxElements = 8;

    public string PieceName { get; private set; }

    protected CompositionType _composition;
    protected IList<ElementType> _elements;
    protected CommandType _command;
    private IList<IDisposable> _subscribers;
    protected bool _isPreset;

    protected override void Awake() {
        base.Awake();
        PieceName = GetPieceName();
        _isPreset = _transform.childCount > 0;
        if (!GameStatus.Instance.IsRunning) {
            Subscribe();
        }
        else {
            Initiate();
        }
    }

    private void Subscribe() {
        _subscribers = new List<IDisposable>();
        _subscribers.Add(GameManager.Instance.SubscribeToPropertyChanged<GameManager, GameState>(gm => gm.CurrentState, OnGameStateChanged));
    }

    private void Initiate() {
        CreateComposition();
        DeployPiece();
        EnablePiece();
        OnCompleted();
        __InitializeCommandIntel();
    }

    private void OnGameStateChanged() {
        if (GameManager.Instance.CurrentState == GetCreationGameState()) {
            CreateComposition();
            DeployPiece();
            EnablePiece();  // must make View operational before starting state changes within it
            OnCompleted();
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
        _elements = gameObject.GetSafeMonoBehaviourComponentsInChildren<ElementType>();
        IPlayer owner = GameManager.Instance.HumanPlayer;
        __isHumanOwnedCreated = true;
        _composition = Activator.CreateInstance<CompositionType>();
        foreach (var element in _elements) {
            ElementCategoryType category = DeriveCategory(element);
            string elementName = element.gameObject.name;
            ElementDataType elementData = CreateElementData(category, elementName, owner);
            AddDataToComposition(elementData);
        }
    }

    private void CreateRandomComposition() {
        IPlayer owner;
        if (!__isHumanOwnedCreated) {
            owner = GameManager.Instance.HumanPlayer;
            __isHumanOwnedCreated = true;
        }
        else {
            owner = GetNonHumanOwner();
        }
        _composition = Activator.CreateInstance<CompositionType>();

        ElementCategoryType[] validHQCategories = GetValidHQElementCategories();

        ElementCategoryType[] validCategories = GetValidElementCategories();

        int elementCount = RandomExtended<int>.Range(1, maxElements);
        for (int i = 0; i < elementCount; i++) {
            ElementCategoryType elementCategory = (i == 0) ? RandomExtended<ElementCategoryType>.Choice(validHQCategories) : RandomExtended<ElementCategoryType>.Choice(validCategories);
            int elementInstanceIndex = GetCurrentCount(elementCategory) + 1;
            string elementInstanceName = elementCategory.ToString() + Constants.Underscore + elementInstanceIndex;
            ElementDataType elementData = CreateElementData(elementCategory, elementInstanceName, owner);
            AddDataToComposition(elementData);
        }
    }

    protected abstract ElementDataType CreateElementData(ElementCategoryType category, string elementName, IPlayer owner);

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
        AcquireCommand();
        InitializePiece();
        MarkHQElement();
        AssignFormationPositions();
    }

    private void DeployRandomPiece() {
        BuildElements();
        InitializeElements();
        AcquireCommand();
        InitializePiece();
        MarkHQElement();
        PositionElements();
        AssignFormationPositions();
    }

    private void BuildElements() {
        _elements = new List<ElementType>();
        foreach (var elementCategory in GetCompositionCategories()) {
            GameObject elementPrefabGo = GetElementPrefabs().First(go => go.name == elementCategory.ToString());
            string elementCategoryName = elementPrefabGo.name;

            GetCompositionData(elementCategory).ForAll(data => {
                GameObject elementGoClone = UnityUtility.AddChild(gameObject, elementPrefabGo);
                elementGoClone.name = elementCategoryName; // get rid of (Clone) in name
                ElementType element = elementGoClone.GetSafeMonoBehaviourComponent<ElementType>();
                _elements.Add(element);
            });
        }
    }

    private void InitializeElements() {
        IDictionary<ElementCategoryType, Stack<ElementDataType>> dataStackLookup = new Dictionary<ElementCategoryType, Stack<ElementDataType>>();
        foreach (var element in _elements) {
            ElementCategoryType elementCategory = DeriveCategory(element);
            Stack<ElementDataType> elementDataStack;
            if (!dataStackLookup.TryGetValue(elementCategory, out elementDataStack)) {
                elementDataStack = new Stack<ElementDataType>(GetCompositionData(elementCategory));
                dataStackLookup.Add(elementCategory, elementDataStack);
            }
            element.Data = elementDataStack.Pop();  // automatically adds the element's transform to Data when set
            // this is not really necessary as Element's prefab should already have ElementItem as its Mesh's CameraLOSChangedRelay target
            element.gameObject.GetSafeMonoBehaviourComponentInChildren<CameraLOSChangedRelay>().AddTarget(element.transform);
        }
    }

    private void AcquireCommand() {
        if (_isPreset) {
            _command = gameObject.GetSafeMonoBehaviourComponentInChildren<CommandType>();
        }
        else {
            GameObject commandGoClone = UnityUtility.AddChild(gameObject, GetCommandPrefab());
            _command = commandGoClone.GetSafeMonoBehaviourComponent<CommandType>();
        }
    }

    private void InitializePiece() {
        AddCommandDataToCommand();    // automatically adds the command transform to Data when set
        _elements.ForAll(element => _command.AddElement(element));
        // command IS NOT assigned as a target of each element's CameraLOSChangedRelay as that would make the CommandIcon disappear when the elements disappear

        // this is not really necessary as Command's prefab should already have CommandItem as its Icon's CameraLOSChangedRelay target
        _command.gameObject.GetSafeMonoBehaviourComponentInChildren<CameraLOSChangedRelay>().AddTarget(_command.transform);
    }

    protected abstract void PositionElements();

    /// <summary>
    /// Randomly positions the ships of the fleet in a spherical globe around this location.
    /// </summary>
    /// <param name="radius">The radius of the globe within which to deploy the fleet.</param>
    /// <returns></returns>
    protected bool PositionElementsRandomlyInSphere(float radius) {  // FIXME need to set FormationPosition
        GameObject[] elementGos = _elements.Select(s => s.gameObject).ToArray();
        Vector3 pieceCenter = _transform.position;
        D.Log("Radius of Sphere occupied by {0} of count {1} is {2}.", PieceName, elementGos.Length, radius);
        return UnityUtility.PositionRandomWithinSphere(pieceCenter, radius, elementGos);
        // fleetCmd will relocate itsef once it selects its flagship
    }

    protected void PositionElementsEquidistantInCircle(float radius) {
        Vector3 pieceCenter = _transform.position;
        Stack<Vector3> localFormationPositions = new Stack<Vector3>(Mathfx.UniformPointsOnCircle(radius, _elements.Count - 1));
        foreach (var element in _elements) {
            if (element.IsHQElement) {
                element.transform.position = pieceCenter;
            }
            else {
                Vector3 localFormationPosition = localFormationPositions.Pop();
                element.transform.position = pieceCenter + localFormationPosition;
            }
        }
    }

    protected abstract void MarkHQElement();

    private void AssignFormationPositions() {
        ElementType hqElement = _elements.Single(s => s.IsHQElement);
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

    private void EnablePiece() {
        // elements need to run their Start first to initialize and assign the designated HQElement to the Command before Command is enabled and runs its Start
        _elements.ForAll(element => element.enabled = true);
        _command.enabled = true;
        EnableViews();
    }

    protected abstract void EnableViews();

    protected abstract void __InitializeCommandIntel();

    private ElementCategoryType DeriveCategory(ElementType element) {
        return Enums<ElementCategoryType>.Parse(element.gameObject.name);
    }

    private int GetCurrentCount(ElementCategoryType elementCategory) {
        if (!GetCompositionCategories().Contains(elementCategory)) {
            return 0;
        }
        return GetCompositionData(elementCategory).Count;
    }

    protected virtual IPlayer GetNonHumanOwner() {
        return new Player(new Race(Enums<Races>.GetRandom(excludeDefault: true)), IQ.Normal);
    }

    protected virtual string GetPieceName() {
        // usually, the name of the piece is carried by the name of the gameobject where this creator is located
        return _transform.name;
    }

    protected virtual void OnCompleted() {
        IsCompleted = true;
    }

    /// <summary>
    /// Returns the GameState defining when creation should occur.
    /// </summary>
    /// <returns>The GameState that triggers creation.</returns>
    protected abstract GameState GetCreationGameState();
    protected abstract void AddDataToComposition(ElementDataType elementData);
    protected abstract IList<ElementDataType> GetCompositionData(ElementCategoryType elementCategory);
    protected abstract IList<ElementCategoryType> GetCompositionCategories();
    protected abstract IEnumerable<GameObject> GetElementPrefabs();
    protected abstract GameObject GetCommandPrefab();
    protected abstract void AddCommandDataToCommand();
    protected abstract ElementCategoryType[] GetValidHQElementCategories();
    protected abstract ElementCategoryType[] GetValidElementCategories();

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        Unsubscribe();
        // other cleanup here including any tracking Gui2D elements
    }

    private void Unsubscribe() {
        if (_subscribers != null) {
            _subscribers.ForAll(d => d.Dispose());
            _subscribers.Clear();
        }
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

