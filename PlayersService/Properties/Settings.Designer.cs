﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace toofz.NecroDancer.Leaderboards.PlayersService.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.3.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public int Version {
            get {
                return ((int)(this["Version"]));
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("00:00:30")]
        public global::System.TimeSpan UpdateInterval {
            get {
                return ((global::System.TimeSpan)(this["UpdateInterval"]));
            }
            set {
                this["UpdateInterval"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("00:00:05")]
        public global::System.TimeSpan DelayBeforeGC {
            get {
                return ((global::System.TimeSpan)(this["DelayBeforeGC"]));
            }
            set {
                this["DelayBeforeGC"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("500")]
        public int PlayersPerUpdate {
            get {
                return ((int)(this["PlayersPerUpdate"]));
            }
            set {
                this["PlayersPerUpdate"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("https://localhost:44300/")]
        public string ToofzApiBaseAddress {
            get {
                return ((string)(this["ToofzApiBaseAddress"]));
            }
            set {
                this["ToofzApiBaseAddress"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("PlayersService")]
        public string PlayersUserName {
            get {
                return ((string)(this["PlayersUserName"]));
            }
            set {
                this["PlayersUserName"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::toofz.EncryptedSecret PlayersPassword {
            get {
                return ((global::toofz.EncryptedSecret)(this["PlayersPassword"]));
            }
            set {
                this["PlayersPassword"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::toofz.EncryptedSecret SteamWebApiKey {
            get {
                return ((global::toofz.EncryptedSecret)(this["SteamWebApiKey"]));
            }
            set {
                this["SteamWebApiKey"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public string PlayersInstrumentationKey {
            get {
                return ((string)(this["PlayersInstrumentationKey"]));
            }
            set {
                this["PlayersInstrumentationKey"] = value;
            }
        }
    }
}