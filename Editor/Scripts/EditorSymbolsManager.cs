
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ActionFit.PackageInstaller
{
    public static class EditorSymbolsManager
    {
        public static bool IsSymbolDefined(string symbol)
        {
#if UNITY_ANDROID
            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
#elif UNITY_IOS
            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.iOS);
#else
            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
#endif
            return defines.Contains(symbol);
        }
        
        public static void AddSymbol(string symbol)
        {
#if UNITY_ANDROID
            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
#elif UNITY_IOS
            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.iOS);
#else
            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
#endif
            
            if (!defines.Contains(symbol))
            {
                if (defines.Length > 0)
                {
                    defines += ";";
                }
                
                defines += symbol;
                
#if UNITY_ANDROID
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, defines);
#elif UNITY_IOS
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.iOS, defines);
#else
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, defines);
#endif
            }
        }
        
        public static void RemoveSymbol(string symbol)
        {
#if UNITY_ANDROID
            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
#elif UNITY_IOS
            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.iOS);
#else
            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
#endif

            if (defines.Contains(symbol))
            {
                string pattern = $"(^{symbol};?)|(;{symbol};?)|(;{symbol}$)";
                string newDefines = System.Text.RegularExpressions.Regex.Replace(defines, pattern, "");
                
#if UNITY_ANDROID
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, newDefines);
#elif UNITY_IOS
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.iOS, newDefines);
#else                
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, newDefines);
#endif
            }
        }
    }
}