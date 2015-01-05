// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiEnumSlider.cs
// Abstract generic base class that uses Enums to populate sliders in the Gui. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using System.Reflection;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract generic base class that uses Enums to populate sliders in the Gui. 
/// </summary>
/// <typeparam name="E">The enum type.</typeparam>
public abstract class AGuiEnumSlider<E> : AGuiTooltip where E : struct {

    private UISlider _slider;
    private float[] _orderedSliderStepValues;
    private E[] _orderedEnumValues;

    protected override void Awake() {
        base.Awake();
        _slider = gameObject.GetSafeMonoBehaviourComponent<UISlider>();
        InitializeSlider();
        InitializeSliderValue();
        GameManager.Instance.onIsRunningOneShot += delegate {
            // Note: UIProgressBar automatically sends a value change event on Start() if the delegate isn't null
            EventDelegate.Add(_slider.onChange, OnSliderValueChange);
        };
    }

    private void InitializeSlider() {
        var enumValues = Enums<E>.GetValues().Except(default(E)).ToArray();
        int numberOfSliderSteps = enumValues.Length;
        _slider.numberOfSteps = numberOfSliderSteps;
        _orderedEnumValues = enumValues.OrderBy(e => e).ToArray();    // assumes E has assigned values in ascending order
        //D.Log("T is {0}. OrderedTValues = {1}.", typeof(T).Name, _orderedTValues.Concatenate());
        _orderedSliderStepValues = MyNguiUtilities.GenerateOrderedSliderStepValues(numberOfSliderSteps);
        //D.Log("OrderedSliderSteps = {0}.", _orderedSliderStepValues.Concatenate());
    }

    private void InitializeSliderValue() {
        PropertyInfo[] propertyInfos = typeof(PlayerPrefsManager).GetProperties();
        PropertyInfo propertyInfo = propertyInfos.SingleOrDefault<PropertyInfo>(p => p.PropertyType == typeof(E));
        if (propertyInfo != null) {
            Func<E> propertyGet = (Func<E>)Delegate.CreateDelegate(typeof(Func<E>), PlayerPrefsManager.Instance, propertyInfo.GetGetMethod());
            E enumPrefsValue = propertyGet();
            int enumPrefsValueIndex = _orderedEnumValues.FindIndex<E>(enumValue => (enumValue.Equals(enumPrefsValue)));
            float sliderValueAtEnumPrefsValueIndex = _orderedSliderStepValues[enumPrefsValueIndex];
            _slider.value = sliderValueAtEnumPrefsValueIndex;
            //D.Log("{0}.sliderValue initialized to {1}.", GetType().Name, _slider.value);
        }
        else {
            _slider.value = _orderedSliderStepValues[_orderedSliderStepValues.Length - 1];
            D.Warn("No PlayerPrefsManager property found for {0}, so initializing slider to : {1}.".Inject(typeof(E), _slider.value));
        }
    }

    private void OnSliderValueChange() {
        float tolerance = 0.05F;
        float sliderValue = UISlider.current.value;
        int index = _orderedSliderStepValues.FindIndex<float>(v => Mathfx.Approx(sliderValue, v, tolerance));
        Arguments.ValidateNotNegative(index);
        E enumValue = _orderedEnumValues[index];
        //D.Log("{0}.index = {1}, TValue = {2}.", GetType().Name, index, tValue);
        OnSliderEnumValueChange(enumValue);
    }

    protected abstract void OnSliderEnumValueChange(E value);

    // IDisposable Note: No reason to remove Ngui event currentListeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to this same GameObject that is being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.

}

