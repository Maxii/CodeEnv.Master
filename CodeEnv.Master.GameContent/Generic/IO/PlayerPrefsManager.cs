// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlayerPrefsManager.cs
// Singleton. Manages saving and acquiring player preference values via UnityEngine.PlayerPrefs. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Singleton. Manages saving and acquiring player preference values via UnityEngine.PlayerPrefs. 
    /// Save default location on disk:
    /// Windows Standalone players: HKCU\Software[company name][product name] key, 
    /// where company and product names are the names set up in Project Settings.
    /// Windows Web players: %APPDATA%\Unity\WebPlayerPrefs
    /// </summary>
    public class PlayerPrefsManager : AGenericSingleton<PlayerPrefsManager>, IInstanceCount {

        private static readonly GameColor[] _defaultPlayerColors = TempGameValues.AllPlayerColors.Except(GameColor.Green).ToArray();

        private string _universeSizeKey = "Universe Size";
        private string _systemDensityKey = "System Density";
        private string _usernameKey = "Username";

        private string _tinyPlayerCountKey = "Player Count_Tiny";
        private string _smallPlayerCountKey = "Player Count_Small";
        private string _normalPlayerCountKey = "Player Count_Normal";
        private string _largePlayerCountKey = "Player Count_Large";
        private string _enormousPlayerCountKey = "Player Count_Enormous";
        private string _giganticPlayerCountKey = "Player Count_Gigantic";

        private string _userPlayerSpeciesKey = "User Player Species";
        private string _aiPlayer1SpeciesKey = "AIPlayer1 Species";
        private string _aiPlayer2SpeciesKey = "AIPlayer2 Species";
        private string _aiPlayer3SpeciesKey = "AIPlayer3 Species";
        private string _aiPlayer4SpeciesKey = "AIPlayer4 Species";
        private string _aiPlayer5SpeciesKey = "AIPlayer5 Species";
        private string _aiPlayer6SpeciesKey = "AIPlayer6 Species";
        private string _aiPlayer7SpeciesKey = "AIPlayer7 Species";

        private string _userPlayerColorKey = "User Player Color";
        private string _aiPlayer1ColorKey = "AIPlayer1 Color";
        private string _aiPlayer2ColorKey = "AIPlayer2 Color";
        private string _aiPlayer3ColorKey = "AIPlayer3 Color";
        private string _aiPlayer4ColorKey = "AIPlayer4 Color";
        private string _aiPlayer5ColorKey = "AIPlayer5 Color";
        private string _aiPlayer6ColorKey = "AIPlayer6 Color";
        private string _aiPlayer7ColorKey = "AIPlayer7 Color";

        private string _aiPlayer1IQKey = "AIPlayer1 IQ";
        private string _aiPlayer2IQKey = "AIPlayer2 IQ";
        private string _aiPlayer3IQKey = "AIPlayer3 IQ";
        private string _aiPlayer4IQKey = "AIPlayer4 IQ";
        private string _aiPlayer5IQKey = "AIPlayer5 IQ";
        private string _aiPlayer6IQKey = "AIPlayer6 IQ";
        private string _aiPlayer7IQKey = "AIPlayer7 IQ";

        private string _userPlayerTeamKey = "User Player Team";
        private string _aiPlayer1TeamKey = "AIPlayer1 Team";
        private string _aiPlayer2TeamKey = "AIPlayer2 Team";
        private string _aiPlayer3TeamKey = "AIPlayer3 Team";
        private string _aiPlayer4TeamKey = "AIPlayer4 Team";
        private string _aiPlayer5TeamKey = "AIPlayer5 Team";
        private string _aiPlayer6TeamKey = "AIPlayer6 Team";
        private string _aiPlayer7TeamKey = "AIPlayer7 Team";

        private string _gameSpeedOnLoadKey = "Game Speed On Load";
        private string _isZoomOutOnCursorEnabledKey = "Zoom Out On Cursor";
        private string _isCameraRollEnabledKey = "Camera Roll";
        private string _isResetOnFocusEnabledKey = "Reset On Focus";
        private string _isPauseOnLoadEnabledKey = "Paused On Load";
        private string _isElementIconsEnabledKey = "Element Icons";
        private string _qualitySettingKey = "Quality Setting";

        //********************************************************************************************
        // WARNING: Changing the name of a Property here requires a commensurate change in the name returned by GuiMenuElementIDExtensions
        // Note: Notifications are not needed for properties that cannot change during a game instance
        public UniverseSizeGuiSelection UniverseSizeSelection { get; private set; }
        public SystemDensityGuiSelection SystemDensitySelection { get; private set; }
        public string Username { get; set; }

        public int TinyPlayerCount { get; private set; }
        public int SmallPlayerCount { get; private set; }
        public int NormalPlayerCount { get; private set; }
        public int LargePlayerCount { get; private set; }
        public int EnormousPlayerCount { get; private set; }
        public int GiganticPlayerCount { get; private set; }

        public SpeciesGuiSelection UserPlayerSpeciesSelection { get; private set; }
        public SpeciesGuiSelection AIPlayer1SpeciesSelection { get; private set; }
        public SpeciesGuiSelection AIPlayer2SpeciesSelection { get; private set; }
        public SpeciesGuiSelection AIPlayer3SpeciesSelection { get; private set; }
        public SpeciesGuiSelection AIPlayer4SpeciesSelection { get; private set; }
        public SpeciesGuiSelection AIPlayer5SpeciesSelection { get; private set; }
        public SpeciesGuiSelection AIPlayer6SpeciesSelection { get; private set; }
        public SpeciesGuiSelection AIPlayer7SpeciesSelection { get; private set; }

        public GameColor UserPlayerColor { get; private set; }
        public GameColor AIPlayer1Color { get; private set; }
        public GameColor AIPlayer2Color { get; private set; }
        public GameColor AIPlayer3Color { get; private set; }
        public GameColor AIPlayer4Color { get; private set; }
        public GameColor AIPlayer5Color { get; private set; }
        public GameColor AIPlayer6Color { get; private set; }
        public GameColor AIPlayer7Color { get; private set; }

        public IQ AIPlayer1IQ { get; private set; }
        public IQ AIPlayer2IQ { get; private set; }
        public IQ AIPlayer3IQ { get; private set; }
        public IQ AIPlayer4IQ { get; private set; }
        public IQ AIPlayer5IQ { get; private set; }
        public IQ AIPlayer6IQ { get; private set; }
        public IQ AIPlayer7IQ { get; private set; }

        public TeamID UserPlayerTeam { get; private set; }
        public TeamID AIPlayer1Team { get; private set; }
        public TeamID AIPlayer2Team { get; private set; }
        public TeamID AIPlayer3Team { get; private set; }
        public TeamID AIPlayer4Team { get; private set; }
        public TeamID AIPlayer5Team { get; private set; }
        public TeamID AIPlayer6Team { get; private set; }
        public TeamID AIPlayer7Team { get; private set; }


        //********************************************************************************************

        //*******************************************************************************************
        // Note: Notifications are not needed for properties that can change during a game instance
        // but require no action to be taken when they change

        public GameSpeed GameSpeedOnLoad { get; private set; }

        public bool IsPauseOnLoadEnabled { get; private set; }

        //*******************************************************************************************

        private bool _isZoomOutOnCursorEnabled;
        public bool IsZoomOutOnCursorEnabled {
            get { return _isZoomOutOnCursorEnabled; }
            private set { SetProperty<bool>(ref _isZoomOutOnCursorEnabled, value, "IsZoomOutOnCursorEnabled"); }
        }

        private bool _isCameraRollEnabled;
        public bool IsCameraRollEnabled {
            get { return _isCameraRollEnabled; }
            private set { SetProperty<bool>(ref _isCameraRollEnabled, value, "IsCameraRollEnabled"); }
        }

        private bool _isResetOnFocusEnabled;
        public bool IsResetOnFocusEnabled {
            get { return _isResetOnFocusEnabled; }
            private set { SetProperty<bool>(ref _isResetOnFocusEnabled, value, "IsResetOnFocusEnabled"); }
        }

        private bool _isElementIconsEnabled;
        public bool IsElementIconsEnabled {
            get { return _isElementIconsEnabled; }
            private set { SetProperty<bool>(ref _isElementIconsEnabled, value, "IsElementIconsEnabled"); }
        }

        private string _qualitySetting;
        public string QualitySetting {
            get { return _qualitySetting; }
            private set { SetProperty<string>(ref _qualitySetting, value, "QualitySetting"); }
        }

        private IGameManager _gameMgr;
        private GeneralSettings _generalSettings;

        /// <summary>
        /// Private constructor that prevents the creation of another externally requested instance of <see cref="PlayerPrefsManager"/>.
        /// </summary>
        private PlayerPrefsManager() {
            Initialize();
        }

        ///<summary>
        /// Called once from the constructor, this does all required initialization
        /// </summary>
        protected sealed override void Initialize() {
            IncrementInstanceCounter();
            _generalSettings = GeneralSettings.Instance;
            _gameMgr = References.GameManager;
            Retrieve();
        }

        public void RecordGamePlayOptions(GamePlayOptionSettings settings) {
            GameSpeedOnLoad = settings.GameSpeedOnLoad;
            IsZoomOutOnCursorEnabled = settings.IsZoomOutOnCursorEnabled;
            //D.Log("{0}: At OptionChangeEvent, PlayerPrefsMgr.IsZoomOutOnCursorEnabled = {1}.", Name, IsZoomOutOnCursorEnabled);
            IsResetOnFocusEnabled = settings.IsResetOnFocusEnabled;
            IsCameraRollEnabled = settings.IsCameraRollEnabled;
            IsPauseOnLoadEnabled = settings.IsPauseOnLoadEnabled;
            ValidateState();
        }

        public void RecordGraphicsOptions(GraphicsOptionSettings settings) {
            if (!QualitySetting.Equals(settings.QualitySetting)) {  // HACK avoids property equal warning
                QualitySetting = settings.QualitySetting;
            }
            IsElementIconsEnabled = settings.IsElementIconsEnabled;
        }

        public void RecordNewGameSettings(NewGamePreferenceSettings settings) {
            UniverseSizeSelection = settings.UniverseSizeGuiSelection;
            SystemDensitySelection = settings.SystemDensitySelection;

            RecordPlayerCount(settings.UniverseSize, settings.PlayerCount);

            UserPlayerSpeciesSelection = settings.UserPlayerSpeciesSelection;
            UserPlayerColor = settings.UserPlayerColor;
            UserPlayerTeam = settings.UserPlayerTeam;

            for (int i = Constants.Zero; i < TempGameValues.MaxAIPlayers; i++) {
                var species = settings.AIPlayerSpeciesSelections[i];
                var color = settings.AIPlayerColors[i];
                var iq = settings.AIPlayerIQs[i];
                var team = settings.AIPlayersTeams[i];
                switch (i) {
                    case 0:
                        AIPlayer1SpeciesSelection = species;
                        AIPlayer1Color = color;
                        AIPlayer1IQ = iq;
                        AIPlayer1Team = team;
                        break;
                    case 1:
                        AIPlayer2SpeciesSelection = species;
                        AIPlayer2Color = color;
                        AIPlayer2IQ = iq;
                        AIPlayer2Team = team;
                        break;
                    case 2:
                        AIPlayer3SpeciesSelection = species;
                        AIPlayer3Color = color;
                        AIPlayer3IQ = iq;
                        AIPlayer3Team = team;
                        break;
                    case 3:
                        AIPlayer4SpeciesSelection = species;
                        AIPlayer4Color = color;
                        AIPlayer4IQ = iq;
                        AIPlayer4Team = team;
                        break;
                    case 4:
                        AIPlayer5SpeciesSelection = species;
                        AIPlayer5Color = color;
                        AIPlayer5IQ = iq;
                        AIPlayer5Team = team;
                        break;
                    case 5:
                        AIPlayer6SpeciesSelection = species;
                        AIPlayer6Color = color;
                        AIPlayer6IQ = iq;
                        AIPlayer6Team = team;
                        break;
                    case 6:
                        AIPlayer7SpeciesSelection = species;
                        AIPlayer7Color = color;
                        AIPlayer7IQ = iq;
                        AIPlayer7Team = team;
                        break;
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(i));
                }
            }
        }

        private void RecordPlayerCount(UniverseSize universeSize, int playerCount) {
            switch (universeSize) {
                case UniverseSize.Tiny:
                    TinyPlayerCount = playerCount;
                    break;
                case UniverseSize.Small:
                    SmallPlayerCount = playerCount;
                    break;
                case UniverseSize.Normal:
                    NormalPlayerCount = playerCount;
                    break;
                case UniverseSize.Large:
                    LargePlayerCount = playerCount;
                    break;
                case UniverseSize.Enormous:
                    EnormousPlayerCount = playerCount;
                    break;
                case UniverseSize.Gigantic:
                    GiganticPlayerCount = playerCount;
                    break;
                case UniverseSize.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(universeSize));
            }
        }

        /// <summary>
        /// Stores all PlayerPrefs to disk.
        /// </summary>
        public void Store() {
            StoreEnumPref<UniverseSizeGuiSelection>(_universeSizeKey, UniverseSizeSelection);
            StoreEnumPref<SystemDensityGuiSelection>(_systemDensityKey, SystemDensitySelection);
            StoreEnumPref<GameSpeed>(_gameSpeedOnLoadKey, GameSpeedOnLoad);

            StoreEnumPref<SpeciesGuiSelection>(_userPlayerSpeciesKey, UserPlayerSpeciesSelection);
            StoreEnumPref<SpeciesGuiSelection>(_aiPlayer1SpeciesKey, AIPlayer1SpeciesSelection);
            StoreEnumPref<SpeciesGuiSelection>(_aiPlayer2SpeciesKey, AIPlayer2SpeciesSelection);
            StoreEnumPref<SpeciesGuiSelection>(_aiPlayer3SpeciesKey, AIPlayer3SpeciesSelection);
            StoreEnumPref<SpeciesGuiSelection>(_aiPlayer4SpeciesKey, AIPlayer4SpeciesSelection);
            StoreEnumPref<SpeciesGuiSelection>(_aiPlayer5SpeciesKey, AIPlayer5SpeciesSelection);
            StoreEnumPref<SpeciesGuiSelection>(_aiPlayer6SpeciesKey, AIPlayer6SpeciesSelection);
            StoreEnumPref<SpeciesGuiSelection>(_aiPlayer7SpeciesKey, AIPlayer7SpeciesSelection);

            StoreEnumPref<GameColor>(_userPlayerColorKey, UserPlayerColor);
            StoreEnumPref<GameColor>(_aiPlayer1ColorKey, AIPlayer1Color);
            StoreEnumPref<GameColor>(_aiPlayer2ColorKey, AIPlayer2Color);
            StoreEnumPref<GameColor>(_aiPlayer3ColorKey, AIPlayer3Color);
            StoreEnumPref<GameColor>(_aiPlayer4ColorKey, AIPlayer4Color);
            StoreEnumPref<GameColor>(_aiPlayer5ColorKey, AIPlayer5Color);
            StoreEnumPref<GameColor>(_aiPlayer6ColorKey, AIPlayer6Color);
            StoreEnumPref<GameColor>(_aiPlayer7ColorKey, AIPlayer7Color);

            StoreEnumPref<IQ>(_aiPlayer1IQKey, AIPlayer1IQ);
            StoreEnumPref<IQ>(_aiPlayer2IQKey, AIPlayer2IQ);
            StoreEnumPref<IQ>(_aiPlayer3IQKey, AIPlayer3IQ);
            StoreEnumPref<IQ>(_aiPlayer4IQKey, AIPlayer4IQ);
            StoreEnumPref<IQ>(_aiPlayer5IQKey, AIPlayer5IQ);
            StoreEnumPref<IQ>(_aiPlayer6IQKey, AIPlayer6IQ);
            StoreEnumPref<IQ>(_aiPlayer7IQKey, AIPlayer7IQ);

            StoreEnumPref<TeamID>(_userPlayerTeamKey, UserPlayerTeam);
            StoreEnumPref<TeamID>(_aiPlayer1TeamKey, AIPlayer1Team);
            StoreEnumPref<TeamID>(_aiPlayer2TeamKey, AIPlayer2Team);
            StoreEnumPref<TeamID>(_aiPlayer3TeamKey, AIPlayer3Team);
            StoreEnumPref<TeamID>(_aiPlayer4TeamKey, AIPlayer4Team);
            StoreEnumPref<TeamID>(_aiPlayer5TeamKey, AIPlayer5Team);
            StoreEnumPref<TeamID>(_aiPlayer6TeamKey, AIPlayer6Team);
            StoreEnumPref<TeamID>(_aiPlayer7TeamKey, AIPlayer7Team);

            StoreBooleanPref(_isZoomOutOnCursorEnabledKey, IsZoomOutOnCursorEnabled);
            StoreBooleanPref(_isCameraRollEnabledKey, IsCameraRollEnabled);
            StoreBooleanPref(_isResetOnFocusEnabledKey, IsResetOnFocusEnabled);
            StoreBooleanPref(_isPauseOnLoadEnabledKey, IsPauseOnLoadEnabled);
            StoreBooleanPref(_isElementIconsEnabledKey, IsElementIconsEnabled);

            StoreStringPref(_qualitySettingKey, QualitySetting);
            StoreStringPref(_usernameKey, Username);

            StoreIntPref(_tinyPlayerCountKey, TinyPlayerCount);
            StoreIntPref(_smallPlayerCountKey, SmallPlayerCount);
            StoreIntPref(_normalPlayerCountKey, NormalPlayerCount);
            StoreIntPref(_largePlayerCountKey, LargePlayerCount);
            StoreIntPref(_enormousPlayerCountKey, EnormousPlayerCount);
            StoreIntPref(_giganticPlayerCountKey, GiganticPlayerCount);

            PlayerPrefs.Save();
            //var retrievedPlayerCount = RetrieveIntPref(_playerCountKey, UniverseSize.Normal.DefaultPlayerCount());
            //D.Log("{0} confirming PlayerCount value stored is {1}.", Name, retrievedPlayerCount);
        }

        private void StoreBooleanPref(string key, bool value) {
            PlayerPrefs.SetString(key, Encrypt(value));
        }

        private void StoreStringPref(string key, string value) {
            Utility.ValidateForContent(value);
            PlayerPrefs.SetString(key, Encrypt(value));
        }

        private void StoreIntPref(string key, int value) {
            PlayerPrefs.SetInt(key, Encrypt(value));
            D.Log("{0} is storing value {1} for key {2}.", Name, value, key);
        }

        private void StoreFloatPref(string key, float value) {
            PlayerPrefs.SetFloat(key, Encrypt(value));
        }

        private void StoreEnumPref<T>(string key, T value) where T : struct {
            if (!Enums<T>.IsDefined(value)) {
                D.Error("{0}: Undefined value {1} of EnumType {2}.", Name, value.ToString(), typeof(T));
            }
            if (!value.Equals(default(T))) {
                PlayerPrefs.SetString(key, Encrypt(value.ToString()));
            }
        }

        private string Encrypt(string value) { return value; }
        private float Encrypt(float value) { return value; }
        private int Encrypt(int value) { return value; }
        private string Encrypt(bool value) { return value.ToString(); }

        /// <summary>
        /// Retrieves all PlayerPrefs and makes them accessible as Properties from this instance. This is where
        /// we set the value at initial startup (there is no preference recorded to disk yet) rather than relying on the default value.
        /// </summary>
        /// <remarks>I considered externalizing these initial startup values, 
        /// but as they are only used once, there is little value to the modder having access to them.
        /// </remarks>
        public void Retrieve() {
            UniverseSizeSelection = RetrieveEnumPref<UniverseSizeGuiSelection>(_universeSizeKey, UniverseSizeGuiSelection.Small);
            SystemDensitySelection = RetrieveEnumPref<SystemDensityGuiSelection>(_systemDensityKey, SystemDensityGuiSelection.Sparse);
            GameSpeedOnLoad = RetrieveEnumPref<GameSpeed>(_gameSpeedOnLoadKey, GameSpeed.Normal);

            UserPlayerSpeciesSelection = RetrieveEnumPref<SpeciesGuiSelection>(_userPlayerSpeciesKey, SpeciesGuiSelection.Human);
            AIPlayer1SpeciesSelection = RetrieveEnumPref<SpeciesGuiSelection>(_aiPlayer1SpeciesKey, SpeciesGuiSelection.Random);
            AIPlayer2SpeciesSelection = RetrieveEnumPref<SpeciesGuiSelection>(_aiPlayer2SpeciesKey, SpeciesGuiSelection.Random);
            AIPlayer3SpeciesSelection = RetrieveEnumPref<SpeciesGuiSelection>(_aiPlayer3SpeciesKey, SpeciesGuiSelection.Random);
            AIPlayer4SpeciesSelection = RetrieveEnumPref<SpeciesGuiSelection>(_aiPlayer4SpeciesKey, SpeciesGuiSelection.Random);
            AIPlayer5SpeciesSelection = RetrieveEnumPref<SpeciesGuiSelection>(_aiPlayer5SpeciesKey, SpeciesGuiSelection.Random);
            AIPlayer6SpeciesSelection = RetrieveEnumPref<SpeciesGuiSelection>(_aiPlayer6SpeciesKey, SpeciesGuiSelection.Random);
            AIPlayer7SpeciesSelection = RetrieveEnumPref<SpeciesGuiSelection>(_aiPlayer7SpeciesKey, SpeciesGuiSelection.Random);

            UserPlayerColor = RetrieveEnumPref<GameColor>(_userPlayerColorKey, _defaultPlayerColors[0]);
            AIPlayer1Color = RetrieveEnumPref<GameColor>(_aiPlayer1ColorKey, _defaultPlayerColors[1]);
            AIPlayer2Color = RetrieveEnumPref<GameColor>(_aiPlayer2ColorKey, _defaultPlayerColors[2]);
            AIPlayer3Color = RetrieveEnumPref<GameColor>(_aiPlayer3ColorKey, _defaultPlayerColors[3]);
            AIPlayer4Color = RetrieveEnumPref<GameColor>(_aiPlayer4ColorKey, _defaultPlayerColors[4]);
            AIPlayer5Color = RetrieveEnumPref<GameColor>(_aiPlayer5ColorKey, _defaultPlayerColors[5]);
            AIPlayer6Color = RetrieveEnumPref<GameColor>(_aiPlayer6ColorKey, _defaultPlayerColors[6]);
            AIPlayer7Color = RetrieveEnumPref<GameColor>(_aiPlayer7ColorKey, _defaultPlayerColors[7]);
            __ValidatePlayerColorPreferences();

            AIPlayer1IQ = RetrieveEnumPref<IQ>(_aiPlayer1IQKey, IQ.Normal);
            AIPlayer2IQ = RetrieveEnumPref<IQ>(_aiPlayer2IQKey, IQ.Normal);
            AIPlayer3IQ = RetrieveEnumPref<IQ>(_aiPlayer3IQKey, IQ.Normal);
            AIPlayer4IQ = RetrieveEnumPref<IQ>(_aiPlayer4IQKey, IQ.Normal);
            AIPlayer5IQ = RetrieveEnumPref<IQ>(_aiPlayer5IQKey, IQ.Normal);
            AIPlayer6IQ = RetrieveEnumPref<IQ>(_aiPlayer6IQKey, IQ.Normal);
            AIPlayer7IQ = RetrieveEnumPref<IQ>(_aiPlayer7IQKey, IQ.Normal);

            UserPlayerTeam = RetrieveEnumPref<TeamID>(_userPlayerTeamKey, TeamID.Team_1);
            AIPlayer1Team = RetrieveEnumPref<TeamID>(_aiPlayer1TeamKey, TeamID.Team_2);
            AIPlayer2Team = RetrieveEnumPref<TeamID>(_aiPlayer2TeamKey, TeamID.Team_3);
            AIPlayer3Team = RetrieveEnumPref<TeamID>(_aiPlayer3TeamKey, TeamID.Team_4);
            AIPlayer4Team = RetrieveEnumPref<TeamID>(_aiPlayer4TeamKey, TeamID.Team_5);
            AIPlayer5Team = RetrieveEnumPref<TeamID>(_aiPlayer5TeamKey, TeamID.Team_6);
            AIPlayer6Team = RetrieveEnumPref<TeamID>(_aiPlayer6TeamKey, TeamID.Team_7);
            AIPlayer7Team = RetrieveEnumPref<TeamID>(_aiPlayer7TeamKey, TeamID.Team_8);

            IsPauseOnLoadEnabled = RetrieveBooleanPref(_isPauseOnLoadEnabledKey, false);

            // the initial change notification sent out by these Properties occur so early they won't be heard by anyone so clients must initialize by calling the Properties directly
            IsZoomOutOnCursorEnabled = RetrieveBooleanPref(_isZoomOutOnCursorEnabledKey, true);
            IsCameraRollEnabled = RetrieveBooleanPref(_isCameraRollEnabledKey, false);
            IsResetOnFocusEnabled = RetrieveBooleanPref(_isResetOnFocusEnabledKey, true);
            IsElementIconsEnabled = RetrieveBooleanPref(_isElementIconsEnabledKey, true);

            QualitySetting = RetrieveStringPref(_qualitySettingKey, QualitySettings.names[QualitySettings.GetQualityLevel()]);

            TinyPlayerCount = RetrieveIntPref(_tinyPlayerCountKey, UniverseSize.Tiny.DefaultPlayerCount());
            SmallPlayerCount = RetrieveIntPref(_smallPlayerCountKey, UniverseSize.Small.DefaultPlayerCount());
            NormalPlayerCount = RetrieveIntPref(_normalPlayerCountKey, UniverseSize.Normal.DefaultPlayerCount());
            LargePlayerCount = RetrieveIntPref(_largePlayerCountKey, UniverseSize.Large.DefaultPlayerCount());
            EnormousPlayerCount = RetrieveIntPref(_enormousPlayerCountKey, UniverseSize.Enormous.DefaultPlayerCount());
            GiganticPlayerCount = RetrieveIntPref(_giganticPlayerCountKey, UniverseSize.Gigantic.DefaultPlayerCount());

            Username = RetrieveStringPref(_usernameKey, "Default Username");
        }

        private bool RetrieveBooleanPref(string key, bool defaultValue) {
            return PlayerPrefs.HasKey(key) ? DecryptToBool(PlayerPrefs.GetString(key)) : defaultValue;
        }

        private string RetrieveStringPref(string key, string defaultValue) {
            return PlayerPrefs.HasKey(key) ? DecryptToString(PlayerPrefs.GetString(key)) : defaultValue;
        }

        private float RetrieveFloatPref(string key, float defaultValue) {
            return PlayerPrefs.HasKey(key) ? DecryptToFloat(PlayerPrefs.GetFloat(key)) : defaultValue;
        }

        private int RetrieveIntPref(string key, int defaultValue) {
            return PlayerPrefs.HasKey(key) ? DecryptToInt(PlayerPrefs.GetInt(key)) : defaultValue;
        }

        private T RetrieveEnumPref<T>(string key, T defaultValue) where T : struct {
            if (PlayerPrefs.HasKey(key)) {
                string decryptedStringValue = DecryptToString(PlayerPrefs.GetString(key));
                T pref;
                if (!Enums<T>.TryParse(decryptedStringValue, out pref)) {
                    D.Error("{0}: Unable to parse Preference {1} of Type {2}.", Name, decryptedStringValue, typeof(T).Name);
                }
                return pref;
            }
            return defaultValue;
        }

        private string DecryptToString(string value) { return value; }
        private float DecryptToFloat(float value) { return value; }
        private int DecryptToInt(int value) { return value; }
        private bool DecryptToBool(string value) { return bool.Parse(value); }

        private void __ValidatePlayerColorPreferences() {
            var playerColorPrefs = new GameColor[] {UserPlayerColor, AIPlayer1Color, AIPlayer2Color, AIPlayer3Color, AIPlayer4Color,
            AIPlayer5Color, AIPlayer6Color, AIPlayer7Color };
            playerColorPrefs.ForAll(pref => {
                D.Assert(pref.EqualsAnyOf(TempGameValues.AllPlayerColors), "{0}: PlayerColorPref {1} is invalid.", Name, pref.GetValueName());
            });
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Debug

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void ValidateState() {
            // Grab the name of the calling method
            System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
            string callerIdMessage = " Called by {0}.{1}().".Inject(stackFrame.GetFileName(), stackFrame.GetMethod().Name);

            D.Assert(UniverseSizeSelection != UniverseSizeGuiSelection.None, callerIdMessage + "UniverseSize selection cannot be None.");
            D.Assert(GameSpeedOnLoad != GameSpeed.None, callerIdMessage + "GameSpeedOnLoad cannot be None.");
        }

        #endregion

        #region IInstanceCount Members

        private static int _instanceCounter = 0;
        public int InstanceCount { get; private set; }

        private void IncrementInstanceCounter() {
            InstanceCount = System.Threading.Interlocked.Increment(ref _instanceCounter);
        }

        #endregion

    }
}


