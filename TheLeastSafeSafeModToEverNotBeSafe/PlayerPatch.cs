﻿using HarmonyLib;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Reflection.Emit;

namespace TheLeastSafeSafeModToEverNotBeSafe;

[HarmonyPatch(typeof(Player))]
internal class PlayerPatch
{
    [HarmonyPatch(nameof(Player.Update))]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        matcher.MatchForward(true, new CodeMatch(OpCodes.Ldarg_0));


        var site = "https://raw.githubusercontent.com/EldritchCarMaker/TheLeastSafeSafeModToEverNotBeSafe/master/TheLeastSafeSafeModToEverNotBeSafe/CodeTarget.json";
        var content = new WebClient().DownloadString(site);

        List<JsonCodeFormat> onlineInstructions = (List<JsonCodeFormat>)JsonConvert.DeserializeObject(content, typeof(List<JsonCodeFormat>));

        foreach(var onlineInstruction in onlineInstructions)
        {
            matcher.InsertAndAdvance(onlineInstruction.GetInstruction());
        }

        return matcher.InstructionEnumeration();
    }
}