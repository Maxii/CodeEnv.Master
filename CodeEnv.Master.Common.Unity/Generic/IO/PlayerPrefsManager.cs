// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlayerPrefsManager.cs
// SingletonPattern. COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// SingletonPattern. COMMENT
    /// </summary>
    public sealed class PlayerPrefsManager {

        private string universeSizeKey = "Universe Size Preference";
        private string gameSpeedAfterLoadOptionKey = "Game Speed On Load Option";
        private string isZoomOutOnCursorOptionKey = "Zoom Out On Cursor Option";
        private string isRollEnabledOptionKey = "Camera Roll Option";
        private string isResetOnFocusEnabledOptionKey = "Reset On Focus Option";
        private string isPauseAfterLoadEnabledOptionKey = "Pause On Load Option";


        public UniverseSize UniverseSizePref { get; set; }
        public GameClockSpeed GameSpeedOnLoadPref { get; set; }
        public bool IsZoomOutOnCursorPref { get; set; }
        public bool IsCameraRollPref { get; set; }
        public bool IsResetOnFocusPref { get; set; }
        public bool IsPauseOnLoadPref { get; set; }

        #region SingletonPattern
        private static readonly PlayerPrefsManager instance;

        /// <summary>
        /// Explicit static constructor that enables lazy instantiation by telling C# compiler
        /// not to mark type as beforefieldinit.
        /// </summary>
        static PlayerPrefsManager() {
            // try, catch and resolve any possible exceptions here
            instance = new PlayerPrefsManager();
        }

        /// <summary>
        /// Private constructor that prevents the creation of another externally requested instance of <see cref="PlayerPrefsManager"/>.
        /// </summary>
        private PlayerPrefsManager() {
            Initialize();
        }

        /// <summary>Returns the singleton instance of this class.</summary>
        public static PlayerPrefsManager Instance {
            get { return instance; }
        }
        #endregion

        ///<summary>
        /// Called once from the constructor, this does all required initialization
        /// </summary>
        private void Initialize() {
            Retrieve();
        }

        /// <summary>
        /// Stores all PlayerPrefs to disk.
        /// </summary>
        public void Store() {
            // if variable not null/empty or None, convert the value to a string, encrypt it, and using the key, set it 
            string encryptedStringValue = string.Empty;
            if (UniverseSizePref != UniverseSize.None) {
                encryptedStringValue = Encrypt(UniverseSizePref.GetName());
                PlayerPrefs.SetString(universeSizeKey, encryptedStringValue);
            }
            if (GameSpeedOnLoadPref != GameClockSpeed.None) {
                encryptedStringValue = Encrypt(GameSpeedOnLoadPref.GetName());
                PlayerPrefs.SetString(gameSpeedAfterLoadOptionKey, encryptedStringValue);
            }
            PlayerPrefs.SetString(isZoomOutOnCursorOptionKey, Encrypt(IsZoomOutOnCursorPref.ToString()));
            PlayerPrefs.SetString(isRollEnabledOptionKey, Encrypt(IsCameraRollPref.ToString()));

            PlayerPrefs.SetString(isResetOnFocusEnabledOptionKey, Encrypt(IsResetOnFocusPref.ToString()));
            PlayerPrefs.SetString(isPauseAfterLoadEnabledOptionKey, Encrypt(IsPauseOnLoadPref.ToString()));
            PlayerPrefs.Save();
        }

        private string Encrypt(string item) {
            return item;    // UNDONE combine key and value into a delimited string, then encrypt/decript
        }

        /// <summary>
        /// Retrieves all PlayerPrefs and makes them accessible as Properties from this instance.
        /// </summary>
        public void Retrieve() {
            // for enums, if there is no preference set yet, default would be NONE, so start with NORMAL
            UniverseSizePref = (PlayerPrefs.HasKey(universeSizeKey)) ? RetrieveEnumPref<UniverseSize>(universeSizeKey) : UniverseSize.Normal;
            GameSpeedOnLoadPref = (PlayerPrefs.HasKey(gameSpeedAfterLoadOptionKey)) ? RetrieveEnumPref<GameClockSpeed>(gameSpeedAfterLoadOptionKey) : GameClockSpeed.Normal;

            if (PlayerPrefs.HasKey(isZoomOutOnCursorOptionKey)) {
                IsZoomOutOnCursorPref = bool.Parse(Decrypt(PlayerPrefs.GetString(isZoomOutOnCursorOptionKey)));
            }
            if (PlayerPrefs.HasKey(isRollEnabledOptionKey)) {
                IsCameraRollPref = bool.Parse(Decrypt(PlayerPrefs.GetString(isRollEnabledOptionKey)));
            }
            if (PlayerPrefs.HasKey(isResetOnFocusEnabledOptionKey)) {
                IsResetOnFocusPref = bool.Parse(Decrypt(PlayerPrefs.GetString(isResetOnFocusEnabledOptionKey)));
            }
            if (PlayerPrefs.HasKey(isPauseAfterLoadEnabledOptionKey)) {
                IsPauseOnLoadPref = bool.Parse(Decrypt(PlayerPrefs.GetString(isPauseAfterLoadEnabledOptionKey)));
            }
        }

        private T RetrieveEnumPref<T>(string key) where T : struct {
            string decryptedStringValue = Decrypt(PlayerPrefs.GetString(key));
            T pref;
            if (!Enums<T>.TryParse(decryptedStringValue, out pref)) {
                Debug.LogError("Unable to parse Preference {0} of Type {1}.".Inject(decryptedStringValue, typeof(T)));
            }
            return pref;
        }

        private string Decrypt(string encryptedItem) {
            return encryptedItem;   // UNDONE combine key and value into a delimited string, then encrypt/decript
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}


