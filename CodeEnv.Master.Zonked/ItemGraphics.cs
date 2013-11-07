﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ItemGraphics.cs
//  Instantiable, general purpose graphics manager for StationaryItem and FollowableItem celestial objects like planets and moons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Instantiable, general purpose graphics manager for StationaryItem and FollowableItem celestial objects like planets and moons.
/// Assumes location on the same game object as the Stationary or FollowableItem.
/// </summary>
[System.Obsolete]
public class ItemGraphics : AGraphics, IDisposable {

    public float circleScaleFactor = 1.5F;

    private HighlightCircle _circles;
    private StationaryItem _item;
    private IList<IDisposable> _subscribers;

    protected override void Awake() {
        base.Awake();
        Target = _transform;
        _item = gameObject.GetSafeMonoBehaviourComponent<StationaryItem>();
        maxAnimateDistance = Mathf.RoundToInt(AnimationSettings.Instance.MaxCelestialObjectAnimateDistanceFactor * _item.Size);
        //D.Log("MaxAnimateDistanceFactor = {1}, {2}.Size = " + Constants.FormatFloat_4DpMax, _item.Size, AnimationSettings.Instance.MaxCelestialObjectAnimateDistanceFactor, _item.name);
        Subscribe();
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(_item.SubscribeToPropertyChanged<StationaryItem, bool>(i => i.IsFocus, OnItemIsFocusChanged));
    }


    protected override void RegisterComponentsToDisable() {
        // disable the Animation in the item's mesh, but no other animations
        disableComponentOnCameraDistance = gameObject.GetComponentsInChildren<Animation>().Where(a => a.transform.parent == Target).ToArray();
    }

    private void OnItemIsFocusChanged() {
        AssessHighlighting();
    }

    public override void AssessHighlighting() {
        if (!IsVisible) {
            Highlight(Highlights.None);
            return;
        }
        if (_item.IsFocus) {
            Highlight(Highlights.Focused);
            return;
        }
        Highlight(Highlights.None);
    }

    private void Highlight(Highlights highlight) {
        switch (highlight) {
            case Highlights.Focused:
                ShowCircle(true, Highlights.Focused);
                break;
            case Highlights.None:
                ShowCircle(false, Highlights.Focused);
                break;
            case Highlights.Selected:
            case Highlights.SelectedAndFocus:
            case Highlights.General:
            case Highlights.FocusAndGeneral:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
    }

    private void ShowCircle(bool toShow, Highlights highlight) {
        D.Assert(highlight == Highlights.Focused);
        if (!toShow && _circles == null) {
            return;
        }
        if (_circles == null) {
            float normalizedRadius = Screen.height * circleScaleFactor * _item.Size;
            _circles = new HighlightCircle("ItemCircle", Target, normalizedRadius, parent: DynamicObjects.Folder, isRadiusDynamic: true, maxCircles: 1, width: 3F, color: UnityDebugConstants.FocusedColor);
        }
        if (toShow) {
            //D.Log("Item attempting to show circle {0}.", highlight.GetName());
            if (!_circles.IsShowing) {
                StartCoroutine(_circles.ShowCircles((int)highlight));
            }
            else {
                _circles.AddCircle((int)highlight);
            }
        }
        else if (_circles.IsShowing) {
            _circles.RemoveCircle((int)highlight);
        }
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        Unsubscribe();
        if (_circles != null) {
            _circles.Dispose();
        }
    }

    private void Unsubscribe() {
        _subscribers.ForAll(s => s.Dispose());
        _subscribers.Clear();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
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

