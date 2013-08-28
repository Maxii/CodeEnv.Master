// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiEnumSliderBase.cs
// Base class for  Gui Sliders built with NGUI.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using System.Reflection;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// Generic base class for Gui Sliders that select enum values built with NGUI.
/// </summary>
/// <typeparam name="T">The enum type.</typeparam>
public abstract class AGuiEnumSliderBase<T> : GuiTooltip where T : struct {

    protected GameEventManager _eventMgr;
    private UISlider _slider;
    private float[] _orderedSliderStepValues;
    private T[] _orderedTValues;

    protected override void Awake() {
        base.Awake();
        _eventMgr = GameEventManager.Instance;
        _slider = gameObject.GetSafeMonoBehaviourComponent<UISlider>();
        InitializeSlider();
        InitializeSliderValue();
        // don't receive events until initializing is complete
        _slider.onValueChange += OnSliderValueChange;
    }

    private void InitializeSlider() {
        T[] tValues = Enums<T>.GetValues().Except<T>(default(T)).ToArray<T>();
        int numberOfSliderSteps = tValues.Length;
        _slider.numberOfSteps = numberOfSliderSteps;
        _orderedTValues = tValues.OrderBy(tv => tv).ToArray<T>();    // assumes T has assigned values in ascending order
        _orderedSliderStepValues = MyNguiUtilities.GenerateOrderedSliderStepValues(numberOfSliderSteps);
    }

    private void InitializeSliderValue() {
        PropertyInfo[] propertyInfos = typeof(PlayerPrefsManager).GetProperties();
        PropertyInfo propertyInfo = propertyInfos.SingleOrDefault<PropertyInfo>(p => p.PropertyType == typeof(T));
        if (propertyInfo != null) {
            Func<T> propertyGet = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), PlayerPrefsManager.Instance, propertyInfo.GetGetMethod());
            T tPrefsValue = propertyGet();
            int tPrefsValueIndex = _orderedTValues.FindIndex<T>(tValue => (tValue.Equals(tPrefsValue)));
            float sliderValueAtTPrefsValueIndex = _orderedSliderStepValues[tPrefsValueIndex];
            _slider.sliderValue = sliderValueAtTPrefsValueIndex;
        }
        else {
            _slider.sliderValue = _orderedSliderStepValues[_orderedSliderStepValues.Length - 1];
            D.Warn("No PlayerPrefsManager property found for {0}, so initializing slider to : {1}.".Inject(typeof(T), _slider.sliderValue));
        }
    }

    // Note: UISlider automatically sends out an event to this method on Start()
    private void OnSliderValueChange(float sliderValue) {
        float tolerance = 0.05F;
        int index = _orderedSliderStepValues.FindIndex<float>(v => Mathfx.Approx(sliderValue, v, tolerance));
        Arguments.ValidateNotNegative(index);
        T tValue = _orderedTValues[index];
        OnSliderValueChange(tValue);
    }

    protected abstract void OnSliderValueChange(T value);

    // IDisposable Note: No reason to remove Ngui event currentListeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to this same GameObject that is being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.

}

