// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugSettings.cs
// Acquires from XML and holds all Debug settings.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR
//#define DEBUG_LOG

namespace CodeEnv.Master.Common {

    using System.Collections.Generic;
    using System.Xml;
    //using System.Xml.Linq;

    public class DebugSettings {

        // XPath criteria needed with XmlDocument
        private string booleanTagXPath = "/Debug/Boolean";
        private string intTagXPath = "/Debug/Integer";
        private string floatTagXPath = "/Debug/Float";

        private string settingTagName = "Setting";

        //private string booleanTagName = "Boolean";
        //private string intTagName = "Integer";
        //private string floatTagName = "Float";
        //private string propertyNameTagName = "PropertyName";
        //private string propertyValueTagName = "PropertyValue";

        private static Dictionary<string, bool> booleanDebugSettings = new Dictionary<string, bool>();
        private static Dictionary<string, int> integerDebugSettings = new Dictionary<string, int>();
        private static Dictionary<string, float> floatDebugSettings = new Dictionary<string, float>();

        private static bool isFpsReadoutInitialized;
        private static bool enableFpsReadout;
        public static bool EnableFpsReadout {
            get {
                if (!isFpsReadoutInitialized) {
                    string propertyName = Utility.GetPropertyName(() => EnableFpsReadout);
                    enableFpsReadout = booleanDebugSettings[propertyName];
                    isFpsReadoutInitialized = true;
                }
                return enableFpsReadout;
            }
        }

        private static bool isUnlockAllItemsInitialized;
        private static bool unlockAllItems;
        public static bool UnlockAllItems {
            get {
                if (!isUnlockAllItemsInitialized) {
                    string propertyName = Utility.GetPropertyName(() => UnlockAllItems);
                    unlockAllItems = booleanDebugSettings[propertyName];
                    isUnlockAllItemsInitialized = true;
                }
                return unlockAllItems;
            }
        }

        private static bool isDisableEnemiesInitialized;
        private static bool disableEnemies;
        public static bool DisableEnemies {
            get {
                if (!isDisableEnemiesInitialized) {
                    string propertyName = Utility.GetPropertyName(() => DisableEnemies);
                    disableEnemies = booleanDebugSettings[propertyName];
                    isDisableEnemiesInitialized = true;
                }
                return disableEnemies;
            }
        }

        private static bool isDisableGuiInitialized;
        private static bool disableGui;
        public static bool DisableGui {
            get {
                if (!isDisableGuiInitialized) {
                    string propertyName = Utility.GetPropertyName(() => DisableGui);
                    disableGui = booleanDebugSettings[propertyName];
                    isDisableGuiInitialized = true;
                }
                return disableGui;
            }
        }

        private static bool isMakePlayerInvincibleInitialized;
        private static bool makePlayerInvincible;
        public static bool MakePlayerInvincible {
            get {
                if (!isMakePlayerInvincibleInitialized) {
                    string propertyName = Utility.GetPropertyName(() => MakePlayerInvincible);
                    makePlayerInvincible = booleanDebugSettings[propertyName];
                    isMakePlayerInvincibleInitialized = true;
                }
                return makePlayerInvincible;
            }
        }

        private static bool isDisableAllGameplayInitialized;
        private static bool disableAllGameplay;
        public static bool DisableAllGameplay {
            get {
                if (!isDisableAllGameplayInitialized) {
                    string propertyName = Utility.GetPropertyName(() => DisableAllGameplay);
                    disableAllGameplay = booleanDebugSettings[propertyName];
                    isDisableAllGameplayInitialized = true;
                }
                return disableAllGameplay;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugSettings" /> class.
        /// </summary>
        /// <param name="path">The path to the XML file holding the debug settings.</param>
        public DebugSettings(string path) {
            //XElement document = XElement.Load(path);
            XmlDocument document = new XmlDocument();
            document.Load(path);
            AcquireDebugSettings(document);
        }

        // uses the old XmlDocument class which works with the WebPlayer API, unlike System.Xml.Linq
        private void AcquireDebugSettings(XmlDocument document) {
            XmlNode topNode = document.SelectSingleNode(booleanTagXPath);
            XmlNodeList settingNodes = topNode.SelectNodes(settingTagName);
            foreach (XmlNode settingNode in settingNodes) {
                XmlNode propertyNameNode = settingNode.FirstChild;
                D.Log("PropertyNameNodeTag = {0}.", propertyNameNode.Name);
                string propertyName = propertyNameNode.InnerText;
                D.Log("Boolean PropertyName = {0}.", propertyName);
                if (Utility.CheckForContent(propertyName)) {
                    string boolValue = settingNode.LastChild.InnerText;
                    D.Log("Boolean PropertyValue = {0}.", boolValue);
                    if (!Utility.CheckForContent(boolValue)) {
                        boolValue = "false";    // empty property value nodes default to false
                    }
                    booleanDebugSettings.Add(propertyName, bool.Parse(boolValue));
                }
            }

            topNode = document.SelectSingleNode(intTagXPath);
            settingNodes = topNode.SelectNodes(settingTagName);
            foreach (XmlNode settingNode in settingNodes) {
                XmlNode propertyNameNode = settingNode.FirstChild;
                string propertyName = propertyNameNode.InnerText;
                D.Log("PropertyName = {0}.", propertyName);
                if (Utility.CheckForContent(propertyName)) {
                    string propertyValue = settingNode.LastChild.InnerText;
                    D.Log("PropertyValue = {0}.", propertyValue);
                    integerDebugSettings.Add(propertyName, int.Parse(propertyValue));
                }
            }

            topNode = document.SelectSingleNode(floatTagXPath);
            settingNodes = topNode.SelectNodes(settingTagName);
            foreach (XmlNode settingNode in settingNodes) {
                XmlNode propertyNameNode = settingNode.FirstChild;
                string propertyName = propertyNameNode.InnerText;
                if (Utility.CheckForContent(propertyName)) {
                    floatDebugSettings.Add(propertyName, float.Parse(settingNode.LastChild.InnerText));
                }
            }
        }

        // uses System.Xml.Linq which is not supported by the WebPlayer API
        //[Obsolete]
        //private void AcquireDebugSettings(XElement document) {
        //    XElement topNode = document.Element(booleanTagName);
        //    var settingNodes = topNode.Elements(settingTagName);
        //    foreach (var settingNode in settingNodes) {
        //        XElement propertyNameNode = settingNode.Element(propertyNameTagName);
        //        if (!propertyNameNode.IsEmpty) {
        //            string boolValue = settingNode.Element(propertyValueTagName).Value;
        //            if (boolValue == string.Empty) {
        //                boolValue = "false";    // empty property value nodes default to false
        //            }
        //            booleanDebugSettings.Add(propertyNameNode.Value, bool.Parse(boolValue));
        //        }
        //    }

        //    topNode = document.Element(intTagName);
        //    settingNodes = topNode.Elements(settingTagName);
        //    foreach (var settingNode in settingNodes) {
        //        XElement propertyNameNode = settingNode.Element(propertyNameTagName);
        //        if (!propertyNameNode.IsEmpty) {
        //            integerDebugSettings.Add(propertyNameNode.Value, int.Parse(settingNode.Element(propertyValueTagName).Value));
        //        }
        //    }

        //    topNode = document.Element(floatTagName);
        //    settingNodes = topNode.Elements(settingTagName);
        //    foreach (var settingNode in settingNodes) {
        //        XElement propertyNameNode = settingNode.Element(propertyNameTagName);
        //        if (!propertyNameNode.IsEmpty) {
        //            floatDebugSettings.Add(propertyNameNode.Value, float.Parse(settingNode.Element(propertyValueTagName).Value));
        //        }
        //    }
        //}

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

