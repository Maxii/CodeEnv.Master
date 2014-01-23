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
public abstract class ACreator<ElementType, CommandType, CompositionType> : AMonoBase, IDisposable
    where ElementType : AElement
    where CommandType : ACommandItem<ElementType>
    where CompositionType : class, new() {

    protected static bool __isHumanOwnedCreated;

    public int maxElements = 8;

    protected string _pieceName;

    protected CompositionType _composition;
    protected IList<ElementType> _elements;
    protected CommandType _command;
    private IList<IDisposable> _subscribers;
    protected bool _isPreset;

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

    protected abstract void CreateCompositionFromChildren();

    protected abstract void CreateRandomComposition();



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

    protected abstract void BuildElements();

    protected abstract void InitializeElements();

    private void AcquireCommand() {
        if (_isPreset) {
            _command = gameObject.GetSafeMonoBehaviourComponentInChildren<CommandType>();
        }
        else {
            GameObject commandGoClone = UnityUtility.AddChild(gameObject, GetCommandPrefab());
            _command = commandGoClone.GetSafeMonoBehaviourComponent<CommandType>();
        }
    }

    protected abstract GameObject GetCommandPrefab();

    protected abstract void InitializePiece();

    protected abstract void MarkHQElement();

    protected abstract void PositionElements();

    /// <summary>
    /// Randomly positions the ships of the fleet in a spherical globe around this location.
    /// </summary>
    /// <param name="radius">The radius of the globe within which to deploy the fleet.</param>
    /// <returns></returns>
    protected bool PositionElementsRandomlyInSphere(float radius) {  // FIXME need to set FormationPosition
        GameObject[] elementGos = _elements.Select(s => s.gameObject).ToArray();
        Vector3 pieceCenter = _transform.position;
        D.Log("Radius of Sphere occupied by {0} of count {1} is {2}.", _pieceName, elementGos.Length, radius);
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


    protected abstract void AssignFormationPositions();

    protected abstract void __InitializeCommandIntel();

    private void EnablePiece() {
        // elements need to run their Start first to initialize and assign the designated HQElement to the Command before Command is enabled and runs its Start
        _elements.ForAll(element => element.enabled = true);
        _command.enabled = true;
        EnableViews();
    }

    protected abstract void EnableViews();

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

