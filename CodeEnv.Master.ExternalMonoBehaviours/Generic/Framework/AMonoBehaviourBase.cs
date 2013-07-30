// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMonoBehaviourBase.cs
// Abstract Base class for types that are derived from MonoBehaviour.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Abstract Base class for types that are derived from MonoBehaviour.
/// NOTE: Unity will never call the 'overrideable' Awake(), Start(), Update(), LateUpdate(), FixedUpdate(), OnGui(), etc. methods when 
/// there is a higher derived class in the chain. Unity only calls the method (if implemented) of the highest derived class.
/// </summary>
public abstract class AMonoBehaviourBase : MonoBehaviour, IInstanceIdentity, IChangeTracking, INotifyPropertyChanged, INotifyPropertyChanging {

    #region IInstanceIdentity Members
    private static int _instanceCounter = 0;
    public int InstanceID { get; set; }

    protected void IncrementInstanceCounter() {
        InstanceID = System.Threading.Interlocked.Increment(ref _instanceCounter);
    }

    #endregion

    #region ToUpdate

    /// <summary>
    /// Enum holding settings governing how frequently ToUpdate() will
    /// return true when called. Used for cutting down the number of times
    /// Update() or LateUpdate() has to be processed.
    /// </summary>
    [Serializable]
    protected enum UpdateFrequency {
        None = 0,
        /// <summary>
        /// Default. ToUpdate() returns true every time.
        /// </summary>
        Continuous = 1,
        /// <summary>
        /// ToUpdate() returns true every other time.
        /// </summary>
        Frequent = 2,
        /// <summary>
        /// ToUpdate() returns true every fourth time.
        /// </summary>
        Normal = 4,
        /// <summary>
        /// ToUpdate() returns true every eighth time.
        /// </summary>
        Infrequent = 8,
        /// <summary>
        /// ToUpdate() returns true every sixteenth time.
        /// </summary>
        Seldom = 16,
        Rare = 32,
        VeryRare = 64,
        HardlyEver = 128
    }

    /// <value>
    ///  The rate at which ToUpdate() returns true. Default is Continuous.
    /// </value>
    private UpdateFrequency _updateRate = UpdateFrequency.Continuous;
    protected UpdateFrequency UpdateRate {
        get { return _updateRate; }
        set {
            _updateRate = value;
            _updateCounter = (int)value; // makes sure ToUpdate() returns true immediately the first time
        }
    }

    private int _updateCounter = Constants.Zero;

    /// <summary>
    /// Optionally used inside Update() or LateUpdate() to determine the frequency with which the containing code should be processed.
    /// </summary>
    /// <returns>true on a pace set by the UpdateRate property</returns>
    protected bool ToUpdate() {
        bool toUpdate = false;
        if (GameManager.Instance.IsGameRunning) {
            if (UpdateRate == UpdateFrequency.Continuous) {
                toUpdate = true;
            }
            else if (_updateCounter >= (int)UpdateRate) {    // >= in case UpdateRate gets changed after initialization
                _updateCounter = Constants.Zero;
                toUpdate = true;
            }
            else {
                _updateCounter++;
            }
        }
        return toUpdate;
    }

    #endregion

    #region Invoke
    // Based on an Action Delegate that encapsulates methods with no parameters that return void, aka 'a task'.

    /// <summary>
    /// Invokes the specified method after the specified time delay without using the Error-prone method name string.
    /// </summary>
    /// <param name="task">The method to invoke encapsulated as an Action delegate. The method must be parameterless and return void.</param>
    /// <param name="time">The time delay in seconds until the method is invoked.</param>
    public void Invoke(Action task, float time) {
        Invoke(task.Method.Name, time);
    }

    /// <summary>
    /// Repeatedly invokes the specified method after the specified time delay without using the Error-prone method name string. Can only be terminated 
    /// with CancelInvoke() or CancelInvoke(Task).
    /// </summary>
    /// <param name="task">The method to invoke encapsulated as a Action delegate. The method must be parameterless and return void.</param>
    /// <param name="time">The time delay in seconds until the method is invoked.</param>
    /// <param name="repeatRate">The repeat rate in seconds.</param>
    public void InvokeRepeating(Action task, float time, float repeatRate) {
        InvokeRepeating(task.Method.Name, time, repeatRate);
    }

    /// <summary>
    /// Invokes the specified method after a random time delay without using the Error-prone method name string.
    /// </summary>
    /// <param name="task">The method to invoke encapsulated as a Action delegate. The method must be parameterless and return void.</param>
    /// <param name="minTime">The minimun amount of delay time.</param>
    /// <param name="maxTime">The maximum amount of delay time.</param>
    public void InvokeRandom(Action task, float minTime, float maxTime) {
        float time = UnityEngine.Random.Range(minTime, maxTime);
        Invoke(task.Method.Name, time);
    }

    /// <summary>
    /// Repeatedly invokes the specified method after a random time delay without using the Error-prone method name string. Can only be terminated 
    /// with CancelInvoke() or CancelInvoke(Task).
    /// </summary>
    /// <param name="task">The method to invoke encapsulated as a Action delegate. The method must be parameterless and return void.</param>
    /// <param name="minTime">The minimun amount of delay time between invokations.</param>
    /// <param name="maxTime">The maximum amount of delay time between invokations.</param>
    /// <returns></returns>
    public IEnumerator InvokeRandomRepeating(Action task, float minTime, float maxTime) {
        while (true) {
            float time = UnityEngine.Random.Range(minTime, maxTime);
            yield return new WaitForSeconds(time);
            Invoke(task.Method.Name, 0);
        }
    }

    /// <summary>
    /// Cancels any invokation of the specified method.
    /// </summary>
    /// <param name="task">The method to cancel encapsulated as a Action delegate. The method must be parameterless and return void.</param>
    public void CancelInvoke(Action task) {
        CancelInvoke(task.Method.Name);
    }

    /// <summary>
    /// Determines whether the specified method is scheduled to be invoked.
    /// </summary>
    /// <param name="task">The method to invoke encapsulated as a Action delegate. The method must be parameterless and return void.</param>
    /// <returns>
    ///   <c>true</c> if the specified method is scheduled to be invoked; otherwise, <c>false</c>.
    /// </returns>
    public bool IsInvoking(Action task) {
        return IsInvoking(task.Method.Name);
    }
    #endregion

    #region Instantiate

    /// <summary>
    /// Instantiates a clone of the original object at position and rotation.
    /// </summary>
    /// <typeparam name="T">The Type of the original to be cloned.</typeparam>
    /// <param name="original">The original.</param>
    /// <param name="position">The position.</param>
    /// <param name="rotation">The rotation.</param>
    /// <returns>A clone of original named [original.Name](Clone)</returns>
    public T Instantiate<T>(T original, Vector3 position, Quaternion rotation) where T : UnityEngine.Object {
        return (T)UnityEngine.Object.Instantiate(original, position, rotation);
    }

    /// <summary>
    /// Instantiates a clone of the original object at the original objects position and rotation.
    /// </summary>
    /// <typeparam name="T">The Type of the original to be cloned.</typeparam>
    /// <param name="original">The original.</param>
    /// <returns>A clone of original named [original.Name](Clone)</returns>

    public T Instantiate<T>(T original) where T : UnityEngine.Object {
        return (T)UnityEngine.Object.Instantiate(original);
    }

    #endregion

    #region Coroutine
    // Based on Func<IEnumerator> and Func<object, IEnumerator> Delegates that encapsulate methods with zero or one parameter that return IEnumerator, aka 'a task'.

    /// <summary>
    /// Starts a Coroutine executing the method contained in task.
    /// Note: Coroutines run on the object that StartCoroutine() is called on.  So if you are starting a coroutine on a different object it makes a lot of sense to use StartCoroutine 
    /// on that object, not the one you are currently executing.
    /// </summary>
    /// <param name="task">The method to run as a Coroutine encapsulated as a Func&lt;IEnumerator&gt; delegate. The method can have no parameters and must return IEnumerator.</param>
    /// <returns>The Coroutine started.</returns>
    public Coroutine StartCoroutine(Func<IEnumerator> task) {
        return StartCoroutine(task.Method.Name);
    }

    /// <summary>
    /// Starts a Coroutine executing the method contained in task.
    /// Note: Coroutines run on the object that StartCoroutine() is called on.  So if you are starting a coroutine on a different object it makes a lot of sense to use StartCoroutine 
    /// on that object, not the one you are currently executing.
    /// </summary>
    /// <param name="task">The method to run as a Coroutine encapsulated as a Func&lt;IEnumerator&gt; delegate. The method must have one parameter and return IEnumerator.</param>
    /// <param name="value">A single optional value to use as the method's parameter, if any.</param>
    /// <returns>The Coroutine started.</returns>
    public Coroutine StartCoroutine(Func<object, IEnumerator> task, object value) {
        return StartCoroutine(task.Method.Name, value);
    }

    /// <summary>
    /// Stops the specific Coroutine executing the method contained in task.
    /// </summary>
    /// <param name="task">The method currently running as a Coroutine encapsulated as a Func&lt;IEnumerator&gt; delegate. The method can have no parameters and must return IEnumerator.</param>
    public void StopCoroutine(Func<IEnumerator> task) {
        StopCoroutine(task.Method.Name);
    }
    /// <summary>
    /// Stops the specific Coroutine executing the method contained in task.
    /// </summary>
    /// <param name="task">The method currently running as a Coroutine encapsulated as a Func&lt;IEnumerator&gt; delegate. The method must have one parameter and return IEnumerator.</param>
    public void StopCoroutine(Func<IEnumerator, object> task) {
        StopCoroutine(task.Method.Name);
    }
    #endregion

    #region GetComponent

    /// <summary>
    /// Gets the single component of Type T that belongs to one of the immediate children of the GameObject.
    /// Returns null if none found. Throws an exception if more than one is found.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <exception cref="InvalidOperationException" />
    /// <returns></returns>
    public T GetComponentInImmediateChildren<T>() where T : UnityEngine.Component {
        T[] tComponents = GetComponentsInImmediateChildren<T>();
        if (tComponents.IsNullOrEmpty<T>()) {
            return null;
        }
        if (tComponents.Length >= 2) {
            throw new InvalidOperationException("More than one component found.");
        }
        return tComponents[0];
    }

    /// <summary>
    /// Gets all the components of Type T that belong to the immediate children of the GameObject. Can be empty.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T[] GetComponentsInImmediateChildren<T>() where T : UnityEngine.Component {
        T[] tComponentsInAllChildren = GetComponentsInChildren<T>();
        var tComponentsInImmediateChildren = from t in tComponentsInAllChildren where t.transform.parent == gameObject.transform select t;
        return tComponentsInImmediateChildren.ToArray<T>();
    }

    /// <summary>
    /// Like GetComponent&lt;T&gt;(), this returns the script component that implements Interface I if the game object has one attached, null if it doesn't. 
    /// </summary>
    /// <typeparam name="I">The Interface Type.</typeparam>
    /// <returns>The script component that implements the Interface I.</returns>
    public I GetInterfaceComponent<I>() where I : class {
        return GetComponent(typeof(I)) as I;
    }

    /// <summary>
    /// Untested. Like GetComponents&lt;T&gt;(), this returns the script components that implement Interface I if the game object has one or more attached, empty if not. 
    /// </summary>
    /// <typeparam name="I">The Type of Interface.</typeparam>
    /// <returns></returns>
    public I[] GetInterfaceComponents<I>() where I : class {
        return Utility.ConvertToArray<I>(GetComponents(typeof(I)));
    }

    #endregion

    /// <summary>
    /// Returns a list of all active loaded MonoBehaviour scripts that implement Interface I. It will return no inactive scripts.
    /// Please note that this function is very slow.
    /// </summary>
    /// <typeparam name="I">The Type of Interface.</typeparam>
    /// <returns></returns>
    public static List<I> FindObjectsOfInterface<I>() where I : class {
        MonoBehaviour[] monoBehaviours = FindObjectsOfType(typeof(MonoBehaviour)) as MonoBehaviour[];
        List<I> list = new List<I>();

        foreach (MonoBehaviour behaviour in monoBehaviours) {
            UnityEngine.Component[] components = behaviour.GetComponents<UnityEngine.Component>();
            var iComponents = from c in components where c is I select c;
            foreach (var c in iComponents) {
                list.Add(c as I);
            }
        }
        return list;
    }

    /// <summary>
    /// Returns a list of all active loaded objects of Type T. It will return no assets (meshes, textures, prefabs, ...) or inactive objects.
    ///Please note that this function is very slow.
    /// </summary>
    /// <typeparam name="T">The Type of Object.</typeparam>
    /// <returns></returns>
    public static T[] FindObjectsOfType<T>() where T : UnityEngine.Object {
        T[] objects = FindObjectsOfType(typeof(T)) as T[];
        return objects;
    }

    #region PropertyChangeTracking

    /// <summary>
    /// Sets the properties backing field to the new value if it has changed and raises PropertyChanged and PropertyChanging
    /// events to any subscribers. Also provides local method access for doing any additional processing work that should be
    /// done outside the setter. This is useful when you have dependant properties in the same object that should change as a 
    /// result of the initial property change.
    /// </summary>
    /// <typeparam name="T">Property Type</typeparam>
    /// <param name="backingStore">The backing store field.</param>
    /// <param name="value">The proposed new value.</param>
    /// <param name="propertyName">Name of the property.</param>
    /// <param name="onChanged">Optional local method to call when the property is changed.</param>
    /// <param name="onChanging">Optional local method to call before the property is changed. The proposed new value is provided as the parameter.</param>
    protected void SetProperty<T>(ref T backingStore, T value, string propertyName, Action onChanged = null, Action<T> onChanging = null) {
        VerifyCallerIsProperty(propertyName);
        D.Log("SetProperty called. {0} changed to {1}.", propertyName, value);
        if (EqualityComparer<T>.Default.Equals(backingStore, value)) {
            return;
        }
        if (onChanging != null) { onChanging(value); }
        OnPropertyChanging(propertyName);

        backingStore = value;

        if (onChanged != null) { onChanged(); }
        _isChanged = true;
        OnPropertyChanged(propertyName);
    }

    protected void OnPropertyChanging(string propertyName) {
        var handler = PropertyChanging; // threadsafe approach
        if (handler != null) {
            handler(this, new PropertyChangingEventArgs(propertyName));
        }
    }

    protected void OnPropertyChanged(string propertyName) {
        var handler = PropertyChanged; // threadsafe approach
        if (handler != null) {
            handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private void VerifyCallerIsProperty(string propertyName) {
        var stackTrace = new System.Diagnostics.StackTrace();
        var frame = stackTrace.GetFrames()[2];
        var caller = frame.GetMethod();
        if (!caller.Name.Equals("set_" + propertyName, StringComparison.InvariantCulture)) {
            throw new InvalidOperationException(string.Format("Called SetProperty {0} from {1}", propertyName, caller.Name));
        }
    }

    #region IChangeTracking Members

    public void AcceptChanges() {
        _isChanged = false;
    }

    private bool _isChanged;
    public bool IsChanged {
        get { return _isChanged; }
    }

    #endregion

    #region INotifyPropertyChanged Members

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion

    #region INotifyPropertyChanging Members

    public event PropertyChangingEventHandler PropertyChanging;

    #endregion

    #endregion

    // No need for IDisposable as there are no resources here to clean up

    // An abstract Base has no need for a ToString() method as its derived children have one.
    // ObjectAnalyzer DOES include analysis of base classes in its scope.

}


