using UnityEditor;

[InitializeOnLoad]
public class DefineSymbolSetter
{
    static DefineSymbolSetter()
    {
        AddDefineSymbol(BuildTargetGroup.Android, "VRC_QUESTIFYER_INSTALLED");
        AddDefineSymbol(BuildTargetGroup.Standalone, "VRC_QUESTIFYER_INSTALLED");
    }

    private static void AddDefineSymbol(BuildTargetGroup buildTarget, string symbol)
    {
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);

        if (!defines.Contains(symbol))
        {
            defines += ";" + symbol;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, defines);
        }
    }
}
