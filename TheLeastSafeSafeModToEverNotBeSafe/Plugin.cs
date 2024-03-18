using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;

namespace TheLeastSafeSafeModToEverNotBeSafe
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("com.snmodding.nautilus")]
    public class Plugin : BaseUnityPlugin
    {
        public new static ManualLogSource Logger { get; private set; }

        private static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();

        private void Awake()
        {
            // set project-scoped logger instance
            Logger = base.Logger;


            // register harmony patches, if there are any
            Harmony.CreateAndPatchAll(Assembly, $"{PluginInfo.PLUGIN_GUID}");
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
    [Serializable]
    public struct JsonCodeFormat
    {
        private static Dictionary<string, CodeInstruction> overrideCodes = new()
        {
            //We can't load fields only using primitives without calling methods
            //So here's some hard coded opcodes to serve as reflection to enable method calling and field getting/setting
            { "Type", new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AccessTools), nameof(AccessTools.TypeByName))) },
            { "Meth", new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AccessTools), nameof(AccessTools.Method), new[] {typeof(Type), typeof(string), typeof(Type[]), typeof(Type[])})) },
            { "Fld", new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AccessTools), nameof(AccessTools.Field))) },
            { "Prop", new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AccessTools), nameof(AccessTools.Property))) },

            //We also can't call methods with only primitives, as the Call opcode requires the MethodInfo as the operand
            //So we're overriding the Call opcode, to replace it with MethodInfo.Invoke which takes a MethodInfo off the stack as opposed to needing it as an operand
            //This allows for arbitrary method calls using primitives, when combined with the reflection opcodes above
            { "Call", new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MethodInfo), nameof(MethodInfo.Invoke), new[] {typeof(MethodInfo), typeof(Type[])})) },

            //The array things here require types be given as operands, which again isn't easily done. So we simply use object arrays for everything, and tell hard typing to go fuck itself
            { "Newarr", new CodeInstruction(OpCodes.Newarr, typeof(object)) },
            { "Stelem", new CodeInstruction(OpCodes.Stelem, typeof(object)) },
        };
        public string Opcode;
        public object Operand;
        public bool TryGetOpCode(out OpCode opCode)
        {
            var field = typeof(OpCodes).GetField(Opcode, AccessTools.all);
            if(field == null)
            {
                Plugin.Logger.LogError($"Could not find opcode for opcode {Opcode} with operand {Operand}");
                opCode = default;
                return false;
            }
            opCode = (OpCode)field.GetValue(null);
            return true;
        }
        public OpCode GetOpCode()
        {
            if(TryGetOpCode(out OpCode opCode)) return opCode;

            throw new Exception($"Could not find opcode for opcode {Opcode} with operand {Operand}");
        }
        public CodeInstruction GetInstruction()
        {
            if(overrideCodes.TryGetValue(Opcode, out var overrider))
                return overrider;
            if (TryGetOpCode(out var code))
                return new CodeInstruction(code, Operand);
            return null;
        }
    }
}