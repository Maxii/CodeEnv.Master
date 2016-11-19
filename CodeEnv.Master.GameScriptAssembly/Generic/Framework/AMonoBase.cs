// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMonoBase.cs
// Abstract Base class for types that are derived from MonoBehaviour.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract Base class for types that are derived from MonoBehaviour.
/// NOTE: Unity will never call the 'overridable' Awake(), Start(), Update(), LateUpdate(), FixedUpdate(), OnGui(), etc. methods when 
/// there is a higher derived class in the chain. Unity only calls the method (if implemented) of the highest derived class.
/// </summary>
public abstract class AMonoBase : MonoBehaviour, IChangeTracking, INotifyPropertyChanged, INotifyPropertyChanging {

    public static bool IsApplicationQuiting { get; private set; }

    #region MonoBehaviour Event Methods

    // Note: When declared in a base class this way, ALL of these events are called by Unity
    // even if the derived class doesn't declare them and call base.Event()

    protected virtual void Awake() {
        useGUILayout = false;    // OPTIMIZE docs suggest = false for better performance
        _debugSettings = DebugSettings.Instance;
        //LogEvent();
    }

    protected virtual void Start() {
        //LogEvent();
    }

    /// <summary>
    /// Called when enabled set to true after the script has been loaded, including after DeSerialization.
    /// </summary>
    protected virtual void OnEnable() {
        //LogEvent();
    }

    /// <summary>
    /// Called when enabled set to false. It is also called immediately after OnApplicationQuit() and 
    /// prior to OnDestroy() and when scripts are reloaded after compilation has finished.
    /// </summary>
    protected virtual void OnDisable() {
        //LogEvent();
        //D.Log("{0} now disabled.", GetType().Name);
    }

    #region OccasionalUpdate Archive

    /// <value>
    ///  The rate at which ToUpdate() returns true, calling OccasionalUpdate(). Default is Never.
    /// </value>
    [Obsolete]
    private FrameUpdateFrequency _updateRate;
    [Obsolete]
    protected FrameUpdateFrequency UpdateRate {
        get { return _updateRate; }
        set {
            _updateRate = value;
            _updateCounter = (int)value; // makes sure ToUpdate() returns true immediately the first time
        }
    }

    [Obsolete]
    private int _updateCounter = Constants.Zero;

    /// <summary>
    /// Optionally used inside Update() or LateUpdate() to determine the frequency with which the containing code should be processed. 
    /// If using inside Update(), consider using the convenience method OccasionalUpdate().
    /// </summary>
    /// <returns>true on a pace set by the UpdateRate property</returns>
    [Obsolete]
    private bool ToUpdate() {
        if (UpdateRate == FrameUpdateFrequency.Never) {
            return false;
        }
        if (UpdateRate == FrameUpdateFrequency.Continuous) {
            return true;
        }
        if (_updateCounter >= (int)UpdateRate - 1) {    // >= in case UpdateRate gets changed after initialization
            _updateCounter = Constants.Zero;
            return true;
        }
        _updateCounter++;
        return false;
    }

    /// <summary>
    /// Called at a frequency determined by the setting of UpdateRate.
    /// </summary>
    [Obsolete]
    protected virtual void OccasionalUpdate() {
        //LogEvent();
    }

    #endregion

    /// <summary>
    /// Called when the Application is quiting, followed by OnDisable() and then OnDestroy().
    /// </summary>
    protected virtual void OnApplicationQuit() {
        //LogEvent();
        IsApplicationQuiting = true;
    }

    /// <summary>
    /// Called as a result of Destroy(gameObject) and following OnDisable() which follows
    /// OnApplicationQuit(). Clients except AMonoSingleton should not override this method.
    /// </summary>
    protected virtual void OnDestroy() {
        //LogEvent();
        Cleanup();
    }

    protected abstract void Cleanup();

    #endregion

    #region UnitySerializer Archive

    ///// <summary>
    ///// Called by UnitySerializer the same way normal Unity methods (above) are called,
    ///// it occurs after the level has been loaded but before it starts to run.
    ///// It is used to do any final initialization.
    ///// </summary>
    //protected virtual void OnDeserialized() {
    //    D.Log("{0}.OnDeserialized().", this.GetType().Name);

    //}

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
    /// <param name="minTime">The minimum amount of delay time.</param>
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
    /// <param name="minTime">The minimum amount of delay time between invocations.</param>
    /// <param name="maxTime">The maximum amount of delay time between invocations.</param>
    /// <returns></returns>
    public IEnumerator InvokeRandomRepeating(Action task, float minTime, float maxTime) {
        while (true) {
            float time = UnityEngine.Random.Range(minTime, maxTime);
            yield return Yielders.GetWaitForSeconds(time);
            Invoke(task.Method.Name, 0);
        }
    }

    /// <summary>
    /// Cancels any invocation of the specified method.
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
    //public T Instantiate<T>(T original, Vector3 position, Quaternion rotation) where T : UnityEngine.Object {
    //    return (T)UnityEngine.Object.Instantiate(original, position, rotation);
    //}

    /// <summary>
    /// Instantiates a clone of the original object at the original objects position and rotation.
    /// </summary>
    /// <typeparam name="T">The Type of the original to be cloned.</typeparam>
    /// <param name="original">The original.</param>
    /// <returns>A clone of original named [original.Name](Clone)</returns>
    //public T Instantiate<T>(T original) where T : UnityEngine.Object {
    //    return (T)UnityEngine.Object.Instantiate(original);
    //}

    #endregion

    #region Coroutine

    // Based on Func<IEnumerator> and Func<object, IEnumerator> Delegates that encapsulate methods with zero or one parameter that return IEnumerator, aka 'a task'.
    //This approach has the advantage of enabling the ability to stop the specific coroutine using the task.
    // Usage when method is on the AMonoBehaviour running the Coroutine: 
    // Func<float, IEnumerator> delegateMethod = MethodName;  StartCoroutine(MethodName, floatValue); StopCoroutine(MethodName); or
    // Func<float, IEnumerator> delegateMethod = MethodName;  StartCoroutine<float>(MethodName, floatValue); StopCoroutine(MethodName);
    //
    //When method is on another object that can't launch its own (aka it is not a MonoBehaviour), then the following usage is the only one that will work.  Initiated from a MonoBehaviour
    // with a reference to the object with the IEnumeratorMethod, this has the advantage of allowing multiple parameters, but you must stop all coroutines on this object to stop it, unless of 
    // course, you stop it internally by continuously testing against a boolean.
    // Usage:
    // StartCoroutine(objectWithIEnumeratorMethod.IEnumeratorMethod(value1, value2, etc.);
    // StopAllCoroutines();

    /// <summary>
    /// Starts a Coroutine executing the method contained in task.
    /// Note: Coroutines run on the object that StartCoroutine() is called on.  So if you are starting a coroutine on a different object it makes a lot of sense to use StartCoroutine 
    /// on that object, not the one you are currently executing.
    /// </summary>
    /// <param name="task">The method to run as a Coroutine encapsulated as a Func&lt;IEnumerator&gt; delegate. The method can have no parameters and must return IEnumerator.</param>
    /// <returns>The Coroutine started.</returns>
    protected Coroutine StartCoroutine(Func<IEnumerator> task) {
        return StartCoroutine(task.Method.Name);
    }

    /// <summary>
    /// Starts a Coroutine executing the method contained in task.
    /// Note: Coroutines run on the object that StartCoroutine() is called on.  So if you are starting a coroutine on a different object it makes a lot of sense to use StartCoroutine 
    /// on that object, not the one you are currently executing.
    /// </summary>
    /// <typeparam name="T">The Type of value.</typeparam>
    /// <param name="task">The method to run as a Coroutine encapsulated as a Func&lt;IEnumerator&gt; delegate. The method must have one parameter and return IEnumerator.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    protected Coroutine StartCoroutine<T>(Func<T, IEnumerator> task, T value) {
        return StartCoroutine(task.Method.Name, value);
    }

    /// <summary>
    /// Stops ALL the Coroutines on this behaviour executing the method contained in task.
    /// </summary>
    /// <param name="task">The method currently running as a Coroutine encapsulated as a Func&lt;IEnumerator&gt; delegate. The method can have no parameters and must return IEnumerator.</param>
    protected void StopCoroutine(Func<IEnumerator> task) {
        StopCoroutine(task.Method.Name);
    }

    /// <summary>
    /// Stops ALL the Coroutines on this behaviour executing the method contained in task.
    /// </summary>
    /// <typeparam name="T">The Type of the value original provided to the task in StartCoroutine.</typeparam>
    /// <param name="task">The method currently running as a Coroutine encapsulated as a Func&lt;IEnumerator&gt; delegate. The method must have one parameter and return IEnumerator..</param>
    protected void StopCoroutine<T>(Func<T, IEnumerator> task) {
        StopCoroutine(task.Method.Name);
    }

    #endregion

    #region PropertyChangeTracking

    /// <summary>
    /// Sets the properties backing field to the new value if it has changed and raises PropertyChanged and PropertyChanging
    /// events to any subscribers. Also provides local method access for doing any additional processing work that should be
    /// done outside the setter. This is useful when you have dependent properties in the same object that should change as a 
    /// result of the initial property change.
    /// </summary>
    /// <typeparam name="T">Property Type</typeparam>
    /// <param name="backingStore">The backing store field.</param>
    /// <param name="value">The proposed new value.</param>
    /// <param name="propertyName">Name of the property.</param>
    /// <param name="onChanged">Optional local method to call when the property is changed.</param>
    /// <param name="onChanging">Optional local method to call before the property is changed. The proposed new value is provided as the parameter.</param>
    protected void SetProperty<T>(ref T backingStore, T value, string propertyName, Action onChanged = null, Action<T> onChanging = null) {
        __VerifyCallerIsProperty(propertyName);
        if (EqualityComparer<T>.Default.Equals(backingStore, value)) {
            if (!CheckForDestroyedMonobehaviourInterface(backingStore)) {
                __TryWarn<T>(backingStore, value, propertyName);
                return;
            }
        }
        //D.Log("SetProperty called. {0} changing to {1}.", propertyName, value);

        if (onChanging != null) { onChanging(value); }
        OnPropertyChanging<T>(propertyName, value);

        backingStore = value;

        if (onChanged != null) { onChanged(); }
        _isChanged = true;
        OnPropertyChanged(propertyName);
    }

    /// <summary>
    /// Returns <c>true</c> if the backingStore of Type T is an interface for a MonoBehaviour that
    /// has been (or is about to be) destroyed, <c>false</c> otherwise.
    /// <remarks>Interfaces for MonoBehaviours have an unusual behaviour when it comes to equality comparisons to null.
    /// If the MonoBehaviour underlying the interface of Type T is slated for destruction, T's DefaultEqualityComparer will return
    /// <c>true</c> when compared to null due to UnityEngine.Object's override of Equals(). However, T <c>backingStore</c> == null
    /// will return <c>false</c> under the same circumstances. As a result, when <c>backingStore</c> is an Interface for a MonoBehaviour
    /// slated for destruction and <c>value</c> is null, the default equality check in SetProperty() sees them as equal and
    /// therefore DOES NOT set <c>backingStore</c> to null. This unexpected behaviour can lead to errors that are hard
    /// to diagnose. This method checks for that condition. If found, SetProperty allows the null <c>value</c> to be assigned
    /// to <c>backingStore</c> resulting in the expected outcome, aka <c>backingStore</c> == null returning <c>true</c>.</remarks>
    /// <see cref="http://answers.unity3d.com/questions/586144/destroyed-monobehaviour-not-comparing-to-null.html"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="backingStore">The backing store.</param>
    /// <returns></returns>
    private bool CheckForDestroyedMonobehaviourInterface<T>(T backingStore) {
        Type tType = typeof(T);
        if (tType.IsInterface) {
            //D.Log("{0} found Interface of Type {1}.", GetType().Name, tType.Name);
            if (backingStore != null && backingStore.Equals(null)) {
                // backingStore is a destroyed MonoBehaviour Interface and value is null since backingStore.Equals(value) to get here
                //D.Log("{0} found MonoBehaviour Interface of Type {1} slated for destruction.", GetType().Name, tType.Name);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Warns of equality if T is a Reference type that is not a string.
    /// <remarks>11.9.16 Discontinued use of DebugSettings.EnableVerboseDebugLog 
    /// as ToString's ObjectAnalyzer isn't really useful.</remarks>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="backingStore">The backing store.</param>
    /// <param name="value">The value.</param>
    /// <param name="propertyName">Name of the property.</param>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private static void __TryWarn<T>(T backingStore, T value, string propertyName) {
        Type tType = typeof(T);
        if (!tType.IsValueType) {
            if (value != null) {
                if (tType == typeof(string)) {
                    D.Warn("{0} BackingStore {1} and value {2} are equal. Property not changed.", propertyName, backingStore, value);
                }
                else {
                    D.Warn("{0} BackingStore and value of Type {1} are equal. Property not changed.", propertyName, tType.Name);
                }
            }
        }
    }

    protected void OnPropertyChanging<T>(string propertyName, T newValue) {
        var handler = PropertyChanging; // thread safe approach
        if (handler != null) {
            handler(this, new PropertyChangingValueEventArgs<T>(propertyName, newValue));   // My custom modification to provide the newValue
        }
    }

    protected void OnPropertyChanged(string propertyName) {
        var handler = PropertyChanged; // thread safe approach
        if (handler != null) {
            handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Verifies the caller is a property.
    /// <remarks>Expensive both in time and allocations.
    /// 11.9.16 Reduced allocation from 800 to 100 bytes per use by having only one local ReferenceType variable and not using Inject().
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    /// <exception cref="System.InvalidOperationException">Called SetProperty {0} from {1}. Check spelling of Property.".Inject(propertyName, caller.Name)</exception>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void __VerifyCallerIsProperty(string propertyName) {
        string callerName = new System.Diagnostics.StackFrame(2).GetMethod().Name;
        if (callerName.Equals("set_" + propertyName, StringComparison.InvariantCulture)) {
            return;
        }
        throw new InvalidOperationException("Called SetProperty {0} from {1}. Check spelling of Property.".Inject(propertyName, callerName));
    }

    /// <summary>
    /// My addition that sets _isChanged to true.
    /// </summary>
    public void MarkAsChanged() {
        _isChanged = true;
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

    #region Debug

    // WARNING: KeyDuplication compile error in Unity with overriding optional parameters in MonoBehaviours   

    private const string AMonoBaseDebugLogEventMethodNameFormat = "{0}(from transform).{1}.{2}()";
    protected DebugSettings _debugSettings;

    protected System.DateTime __durationStartTime;

    protected void __RecordDurationStartTime() {
        __durationStartTime = Utility.SystemTime;
    }

    /// <summary>
    /// Clients should override this if they have multiple durations to log
    /// with the same intro text.
    /// </summary>
    protected virtual string __DurationLogIntroText { get { return "[No intro text]"; } }

    /// <summary>
    /// Logs the duration. "[introText] took x seconds to execute.".
    /// </summary>
    /// <param name="introText">The intro text.</param>
    protected void __LogDuration(string introText = null) {
        if (_debugSettings.EnableDurationLogging) {
            string text = !introText.IsNullOrEmpty() ? introText : __DurationLogIntroText;
            Debug.Log("{0} took {1:0.##} seconds to execute.".Inject(text, (Utility.SystemTime - __durationStartTime).TotalSeconds));
        }
    }

    /// <summary>
    /// Logs a warning statement that the method that calls this has been called.
    /// <remarks>Typically used to ID method calls that I don't expect to occur.</remarks>
    /// </summary>
    public virtual void LogEventWarning() {
        string methodName = GetCallingMethodName();
        string fullMethodName = AMonoBaseDebugLogEventMethodNameFormat.Inject(transform.name, GetType().Name, methodName);
        Debug.LogWarning("Unclear why {0} was called.".Inject(fullMethodName));
    }

    /// <summary>
    /// Logs a statement that the method that calls this has been called.
    /// Logging only occurs if DebugSettings.EnableEventLogging is true.
    /// </summary>
    public virtual void LogEvent() {
        if (_debugSettings.EnableEventLogging) {
            string methodName = GetCallingMethodName();
            string fullMethodName = AMonoBaseDebugLogEventMethodNameFormat.Inject(transform.name, GetType().Name, methodName);
            Debug.Log("{0} beginning execution. Frame {1}, UnityTime {2:0.0}.".Inject(fullMethodName, Time.frameCount, Time.time));
        }
    }

    /// <summary>
    /// Returns the name of the method that called the method using this method.
    /// </summary>
    /// <returns></returns>
    protected string GetCallingMethodName() {
        var stackFrame = new System.Diagnostics.StackFrame(2);
        string methodName = stackFrame.GetMethod().ReflectedType.Name;
        if (methodName.Contains(Constants.LessThan)) {
            string coroutineMethodName = methodName.Substring(methodName.IndexOf(Constants.LessThan) + 1, methodName.IndexOf(Constants.GreaterThan) - 1);
            methodName = coroutineMethodName;
        }
        else {
            methodName = stackFrame.GetMethod().Name;
        }
        return methodName;
    }

    /// <summary>
    /// Returns the name of the method using this method.
    /// </summary>
    /// <returns></returns>
    protected string GetMethodName() {
        var stackFrame = new System.Diagnostics.StackFrame(1);
        string methodName = stackFrame.GetMethod().ReflectedType.Name;
        if (methodName.Contains(Constants.LessThan)) {
            string coroutineMethodName = methodName.Substring(methodName.IndexOf(Constants.LessThan) + 1, methodName.IndexOf(Constants.GreaterThan) - 1);
            methodName = coroutineMethodName;
        }
        else {
            methodName = stackFrame.GetMethod().Name;
        }
        return methodName;
    }

    #endregion

    #region Nested Classes

    /// <summary>
    /// Enum used to define how many frames will be
    /// skipped before ToUpdate or the CoroutineScheduler 
    /// will run.
    /// </summary>
    [Serializable]
    public enum FrameUpdateFrequency {
        /// <summary>
        /// Default.
        /// </summary>
        Never = 0,
        /// <summary>
        ///Every frame.
        /// </summary>
        Continuous = 1,
        /// <summary>
        /// Every other frame.
        /// </summary>
        Frequent = 2,
        /// <summary>
        /// Every fourth frame.
        /// </summary>
        Normal = 4,
        /// <summary>
        /// Every eighth frame.
        /// </summary>
        Infrequent = 8,
        /// <summary>
        /// Every sixteenth frame.
        /// </summary>
        Seldom = 16,
        /// <summary>
        /// Every thirty second frame.
        /// </summary>
        Rare = 32,
        /// <summary>
        /// Every sixty fourth frame.
        /// </summary>
        VeryRare = 64,
        /// <summary>
        /// Every one hundred twenty eighth frame.
        /// </summary>
        HardlyEver = 128
    }

    #endregion

    // An abstract Base has no need for a ToString() method as its derived children have one.
    // ObjectAnalyzer DOES include analysis of base classes in its scope.

}


