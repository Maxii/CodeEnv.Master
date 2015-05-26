// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlayerPrefsManager1.cs
// Singleton. Manages saving and acquiring player preference values via UnityEngine.PlayerPrefs.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// Singleton. Manages saving and acquiring player preference values via UnityEngine.PlayerPrefs. Save default location on disk:
    /// Windows Standalone players: HKCU\Software[company name][product name] key, where company and product names are the names set up in Project Settings.
    /// Windows Web players: %APPDATA%\Unity\WebPlayerPrefs
    /// </summary>
    public class PlayerPrefsManager1 : AGenericSingleton<PlayerPrefsManager1>, IInstanceCount {

        /// <summary>
        /// Fires when a preference value changes.
        /// Note: This onChanged event DOES NOT fire when the values are first retrieved from disk as it occurs too early for anyone to be listening.
        /// Accordingly, clients must acquire the values themselves when they first wake up.
        /// </summary>
        public event Action onChanged;

        private IDictionary<PlayerPrefsElementID, Type> _valueTypeLookup = new Dictionary<PlayerPrefsElementID, Type>() {
            {PlayerPrefsElementID.UniverseSize, typeof(UniverseSizeGuiSelection)},
            {PlayerPrefsElementID.UserPlayerSpecies, typeof(SpeciesGuiSelection)},
            {PlayerPrefsElementID.UserPlayerColor, typeof(GameColor)},
            {PlayerPrefsElementID.GameSpeedOnLoad, typeof(GameSpeed)},
            {PlayerPrefsElementID.IsPauseOnLoadEnabled, typeof(bool)},
            {PlayerPrefsElementID.IsZoomOutOnCursorEnabled, typeof(bool)},
            {PlayerPrefsElementID.IsCameraRollEnabled, typeof(bool)},
            {PlayerPrefsElementID.IsResetOnFocusEnabled, typeof(bool)},
            {PlayerPrefsElementID.IsElementIconsEnabled, typeof(bool)},
            {PlayerPrefsElementID.QualitySetting, typeof(string)},
        };

        private bool _isPauseOnLoadEnabled;
        private bool _isZoomOutOnCursorEnabled;
        private bool _isCameraRollEnabled;
        private bool _isResetOnFocusEnabled;
        private bool _isElementIconsEnabled;

        /// <summary>
        /// Returns the value of the boolean preference associated with the preferenceID.
        /// This method works only for boolean preference types.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public bool GetBooleanValue(PlayerPrefsElementID id) {
            ValidateType<bool>(id);
            switch (id) {
                case PlayerPrefsElementID.IsPauseOnLoadEnabled:
                    return _isPauseOnLoadEnabled;
                case PlayerPrefsElementID.IsZoomOutOnCursorEnabled:
                    return _isZoomOutOnCursorEnabled;
                case PlayerPrefsElementID.IsCameraRollEnabled:
                    return _isCameraRollEnabled;
                case PlayerPrefsElementID.IsResetOnFocusEnabled:
                    return _isResetOnFocusEnabled;
                case PlayerPrefsElementID.IsElementIconsEnabled:
                    return _isElementIconsEnabled;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(id));
            }
        }

        private string _qualitySetting;

        /// <summary>
        /// Returns the value of the preference associated with the preferenceID as a string. 
        /// This method works for all preference types.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public string GetValueAsString(PlayerPrefsElementID id) {
            switch (id) {
                case PlayerPrefsElementID.IsPauseOnLoadEnabled:
                case PlayerPrefsElementID.IsZoomOutOnCursorEnabled:
                case PlayerPrefsElementID.IsCameraRollEnabled:
                case PlayerPrefsElementID.IsResetOnFocusEnabled:
                case PlayerPrefsElementID.IsElementIconsEnabled:
                    return GetBooleanValue(id).ToString();
                case PlayerPrefsElementID.QualitySetting:
                    return _qualitySetting;
                case PlayerPrefsElementID.UniverseSize:
                    return UniverseSizeSelection.GetName();
                case PlayerPrefsElementID.UserPlayerSpecies:
                    return GetSpeciesValue(id).GetName();
                case PlayerPrefsElementID.UserPlayerColor:
                    return GetColorValue(id).GetName();
                case PlayerPrefsElementID.GameSpeedOnLoad:
                    return GameSpeedOnLoad.GetName();
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(id));
            }
        }

        private GameColor _userPlayerColor;

        /// <summary>
        /// Returns the value of the GameColor preference associated with the preferenceID.
        /// This method works only for GameColor preference types.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public GameColor GetColorValue(PlayerPrefsElementID id) {
            ValidateType<GameColor>(id);
            return _userPlayerColor;
        }

        private SpeciesGuiSelection _userPlayerSpecies;

        /// <summary>
        /// Returns the value of the SpeciesGuiSelection preference associated with the preferenceID.
        /// This method works only for SpeciesGuiSelection preference types.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public SpeciesGuiSelection GetSpeciesValue(PlayerPrefsElementID id) {
            ValidateType<SpeciesGuiSelection>(id);
            return _userPlayerSpecies;
        }


        public UniverseSizeGuiSelection UniverseSizeSelection { get; private set; }

        public GameSpeed GameSpeedOnLoad { get; private set; }

        private IGameManager _gameMgr;
        private GeneralSettings _generalSettings;

        /// <summary>
        /// Private constructor that prevents the creation of another externally requested instance of <see cref="PlayerPrefsManager"/>.
        /// </summary>
        private PlayerPrefsManager1() {
            Initialize();
        }

        ///<summary>
        /// Called once from the constructor, this does all required initialization
        /// </summary>
        protected override void Initialize() {
            IncrementInstanceCounter();
            _generalSettings = GeneralSettings.Instance;
            _gameMgr = References.GameManager;
            Retrieve();
        }

        public void RecordGamePlayOptions(GamePlayOptionSettings settings) {
            GameSpeedOnLoad = settings.GameSpeedOnLoad;
            _isZoomOutOnCursorEnabled = settings.IsZoomOutOnCursorEnabled;
            _isResetOnFocusEnabled = settings.IsResetOnFocusEnabled;
            _isCameraRollEnabled = settings.IsCameraRollEnabled;
            _isPauseOnLoadEnabled = settings.IsPauseOnLoadEnabled;
            ValidateState();
            OnChanged();
        }

        public void RecordGraphicsOptions(GraphicsOptionSettings settings) {
            _qualitySetting = settings.QualitySetting;
            _isElementIconsEnabled = settings.IsElementIconsEnabled;
            OnChanged();
        }

        public void RecordNewGameSettings(GameSettings gameSettings) {
            UniverseSizeSelection = gameSettings.UniverseSizeSelection;
            _userPlayerSpecies = gameSettings.UserPlayerSpeciesSelection;
            _userPlayerColor = gameSettings.UserPlayerColor;
            //OnChanged();  // not needed as this method will never be called from a game instance that is continuing
        }

        /// <summary>
        /// Stores all PlayerPrefs to disk.
        /// </summary>
        public void Store() {
            // if variable not null/empty or None, convert the value to a string, encrypt it, and using the key, set it 

            StoreEnumPref<UniverseSizeGuiSelection>(PlayerPrefsElementID.UniverseSize, UniverseSizeSelection);
            StoreEnumPref<GameSpeed>(PlayerPrefsElementID.GameSpeedOnLoad, GameSpeedOnLoad);
            StoreEnumPref<SpeciesGuiSelection>(PlayerPrefsElementID.UserPlayerSpecies, _userPlayerSpecies);
            StoreEnumPref<GameColor>(PlayerPrefsElementID.UserPlayerColor, _userPlayerColor);

            StoreBooleanPref(PlayerPrefsElementID.IsZoomOutOnCursorEnabled, _isZoomOutOnCursorEnabled);
            StoreBooleanPref(PlayerPrefsElementID.IsCameraRollEnabled, _isCameraRollEnabled);
            StoreBooleanPref(PlayerPrefsElementID.IsResetOnFocusEnabled, _isResetOnFocusEnabled);
            StoreBooleanPref(PlayerPrefsElementID.IsPauseOnLoadEnabled, _isPauseOnLoadEnabled);
            StoreBooleanPref(PlayerPrefsElementID.IsElementIconsEnabled, _isElementIconsEnabled);

            StoreStringPref(PlayerPrefsElementID.QualitySetting, _qualitySetting);
            PlayerPrefs.Save();
        }

        private void StoreBooleanPref(PlayerPrefsElementID id, bool value) {
            ValidateType<bool>(id);
            PlayerPrefs.SetString(GetKey(id), Encrypt(value));
        }

        private void StoreStringPref(PlayerPrefsElementID id, string value) {
            ValidateType<string>(id);
            Arguments.ValidateForContent(value);
            PlayerPrefs.SetString(GetKey(id), Encrypt(value));
        }

        private void StoreIntPref(PlayerPrefsElementID id, int value) {
            ValidateType<int>(id);
            PlayerPrefs.SetInt(GetKey(id), Encrypt(value));
        }

        private void StoreFloatPref(PlayerPrefsElementID id, float value) {
            ValidateType<float>(id);
            PlayerPrefs.SetFloat(GetKey(id), Encrypt(value));
        }

        private void StoreEnumPref<T>(PlayerPrefsElementID id, T value) where T : struct {
            ValidateType<T>(id);
            if (!Enums<T>.IsDefined(value)) {
                D.Error("Undefined value {0} of EnumType {1}.", value.ToString(), typeof(T));
            }
            if (!value.Equals(default(T))) {
                PlayerPrefs.SetString(GetKey(id), Encrypt(value.ToString()));
            }
        }

        private string Encrypt(string value) { return value; }
        private float Encrypt(float value) { return value; }
        private int Encrypt(int value) { return value; }
        private string Encrypt(bool value) { return value.ToString(); }

        /// <summary>
        /// Retrieves all PlayerPrefs and makes them accessible from this instance. This is where
        /// we set the value at initial startup (there is no preference recorded to disk yet) rather than relying on the default value.
        /// </summary>
        /// <remarks>I considered externalizing these initial startup values, but as they are only used once, there is little value to the modder
        /// having access to them.
        /// </remarks>
        public void Retrieve() {
            D.Log("{0}.Retrieve() called.", GetType().Name);
            UniverseSizeSelection = RetrieveEnumPref<UniverseSizeGuiSelection>(PlayerPrefsElementID.UniverseSize, UniverseSizeGuiSelection.Normal);
            GameSpeedOnLoad = RetrieveEnumPref<GameSpeed>(PlayerPrefsElementID.GameSpeedOnLoad, GameSpeed.Normal);

            _userPlayerSpecies = RetrieveEnumPref<SpeciesGuiSelection>(PlayerPrefsElementID.UserPlayerSpecies, SpeciesGuiSelection.Human);
            _userPlayerColor = RetrieveEnumPref<GameColor>(PlayerPrefsElementID.UserPlayerColor, GameColor.Blue);

            _isPauseOnLoadEnabled = RetrieveBooleanPref(PlayerPrefsElementID.IsPauseOnLoadEnabled, false);

            // the initial change notification sent out by these Properties occur so early they won't be heard by anyone so clients must initialize by calling the Properties directly
            _isZoomOutOnCursorEnabled = RetrieveBooleanPref(PlayerPrefsElementID.IsPauseOnLoadEnabled, true);
            _isCameraRollEnabled = RetrieveBooleanPref(PlayerPrefsElementID.IsPauseOnLoadEnabled, false);
            _isResetOnFocusEnabled = RetrieveBooleanPref(PlayerPrefsElementID.IsPauseOnLoadEnabled, true);
            _isElementIconsEnabled = RetrieveBooleanPref(PlayerPrefsElementID.IsPauseOnLoadEnabled, true);
            _qualitySetting = RetrieveStringPref(PlayerPrefsElementID.QualitySetting, QualitySettings.names[QualitySettings.GetQualityLevel()]);
        }

        private bool RetrieveBooleanPref(PlayerPrefsElementID id, bool defaultValue) {
            ValidateType<bool>(id);
            string key = GetKey(id);
            return PlayerPrefs.HasKey(key) ? DecryptToBool(PlayerPrefs.GetString(key)) : defaultValue;
        }

        private string RetrieveStringPref(PlayerPrefsElementID id, string defaultValue) {
            ValidateType<string>(id);
            string key = GetKey(id);
            return PlayerPrefs.HasKey(key) ? DecryptToString(PlayerPrefs.GetString(key)) : defaultValue;
        }

        private float RetrieveFloatPref(PlayerPrefsElementID id, float defaultValue) {
            ValidateType<float>(id);
            string key = GetKey(id);
            return PlayerPrefs.HasKey(key) ? DecryptToFloat(PlayerPrefs.GetFloat(key)) : defaultValue;
        }

        private int RetrieveIntPref(PlayerPrefsElementID id, int defaultValue) {
            ValidateType<int>(id);
            string key = GetKey(id);
            return PlayerPrefs.HasKey(key) ? DecryptToInt(PlayerPrefs.GetInt(key)) : defaultValue;
        }

        private T RetrieveEnumPref<T>(PlayerPrefsElementID id, T defaultValue) where T : struct {
            ValidateType<T>(id);
            var key = GetKey(id);
            if (PlayerPrefs.HasKey(key)) {
                string decryptedStringValue = DecryptToString(PlayerPrefs.GetString(key));
                T pref;
                if (!Enums<T>.TryParse(decryptedStringValue, out pref)) {
                    D.Error("Unable to parse Preference {0} of Type {1}.", decryptedStringValue, typeof(T));
                }
                return pref;
            }
            return defaultValue;
        }

        private string DecryptToString(string value) { return value; }
        private float DecryptToFloat(float value) { return value; }
        private int DecryptToInt(int value) { return value; }
        private bool DecryptToBool(string value) { return bool.Parse(value); }

        private void OnChanged() {
            if (onChanged != null) { onChanged(); }
        }

        private void ValidateType<T>(PlayerPrefsElementID id) {
            D.Assert(_valueTypeLookup[id] == typeof(T));
        }

        private string GetKey(PlayerPrefsElementID id) { return id.GetName(); }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Debug

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void ValidateState() {
            // Grab the name of the calling method
            System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
            string callerIdMessage = " Called by {0}.{1}().".Inject(stackFrame.GetFileName(), stackFrame.GetMethod().Name);

            D.Assert(UniverseSizeSelection != UniverseSizeGuiSelection.None, callerIdMessage + "UniverseSize selection cannot be None.", true);
            D.Assert(GameSpeedOnLoad != GameSpeed.None, callerIdMessage + "GameSpeedOnLoad cannot be None.", true);
        }

        #endregion

        #region IInstanceIdentity Members

        private static int _instanceCounter = 0;
        public int InstanceCount { get; private set; }

        private void IncrementInstanceCounter() {
            InstanceCount = System.Threading.Interlocked.Increment(ref _instanceCounter);
        }

        #endregion

        #region Nested Classes

        public enum PlayerPrefsElementID {

            None,

            UniverseSize,

            UserPlayerSpecies,
            UserPlayerColor,

            GameSpeedOnLoad,

            IsZoomOutOnCursorEnabled,
            IsCameraRollEnabled,
            IsResetOnFocusEnabled,
            IsPauseOnLoadEnabled,
            IsElementIconsEnabled,

            QualitySetting

        }
        #endregion
    }
}


