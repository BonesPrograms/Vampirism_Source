using XRL.UI;
using HarmonyLib;
using XRL.World;
using XRL.World.Parts;
using System.Collections.Generic;
using System.Reflection.Emit;
using XRL;
using VampirismSys.Rules;
using XRL.Liquids;
using Qud.UI;
using System;
using VampirismSys.Core;
using XRL.World.Effects;
using XRL.World.Parts.Mutation;

namespace VampirismSys.Patches
{

    [HarmonyPatch(typeof(Asleep), nameof(Asleep.Apply))]

    internal static class AsleepApplyPatch
    {
        static bool PreventSleepStack = false;

        [HarmonyPrefix]
        static bool Prefix(GameObject Object)
        {
            if (Object.HasEffectDescendedFrom<Asleep>())
            {
                PreventSleepStack = true;
                return false;
            }
            return true;
        }

        [HarmonyPostfix]

        static void Postfix(ref bool __result)
        {
            if (PreventSleepStack)
            {
                PreventSleepStack = false;
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(GameObject), nameof(GameObject.ShouldAutoget))]
    internal static class AutogetSilverAilment //i will probably redo blood autoget to be this one day but for now its just for silver ailment
    {
        [HarmonyPostfix]
        static void Postfix(ref bool __result, GameObject __instance) //prevents you from autogetting silver nuggets and burning yourself to death
        {
            if (__result == true && __instance.IsSilver() && Options.GetOptionBool(ModOptions.SILVER))
                __result = false;
        }
    }

    [HarmonyPatch(typeof(TorchProperties), nameof(TorchProperties.HandleEvent), new Type[] { typeof(InventoryActionEvent) })] //prevents you from lighting torches as a vampire and does some other fun stuff that wouldnt happen normally
    internal static class TorchLightRotschrek                                                                                     //when dropping a torch (such as it dropping lit)
    {
        [HarmonyPostfix]
        static void Postfix(ref bool __result, TorchProperties __instance, InventoryActionEvent E)
        {
            if (__result == true && E.Command == "TorchLight" && Options.GetOptionBool(ModOptions.FIRE) && Options.GetOptionBool(ModOptions.TORCH) && E.Actor.IsVampire(out Vampirism v))
            {
                __result = false;
                v.FakeDropRotschrek(__instance.ParentObject);
            }
        }
    }

    [HarmonyPatch(typeof(LiquidBlood), nameof(LiquidBlood.Drank))]
    internal static class BloodDrinking
    {
        static bool PreventGhostConsumption; //prevents blood from being consumed if you refuse to drink while vomitting

        [HarmonyPrefix]

        static bool Prefix(LiquidVolume Liquid, GameObject Target)
        {
            if (Target.TryGetPartDescendedFrom(out VampireBloodMetabolism vitae) && Target.IsPlayer())
            {
                if (Liquid.IsPureLiquid())
                {
                    if (Options.GetOptionBool(ModOptions.HUNTER))
                    {
                        Popup.Show("This does not satisfy - you need living blood.");
                        Target.FireEvent(Event.New("AfterDrank"));
                        return false;
                    }
                    if (vitae.Blood >= Rules.Vitae.SIP_PUKE_WARN)
                    {
                        PreventGhostConsumption = vitae.PukeWarning(false);
                        if (PreventGhostConsumption)
                            return false;
                    }
                    vitae.Drink();
                    Popup.Show(DrinkMessage(Liquid.ParentObject, vitae));
                    return false;
                }
                else
                    Popup.Show("Disgusting! This blood is ruined! You feel " + vitae.UIBloodDisplay + ".");
            }
            return true;
        }

        [HarmonyPostfix]
        static void Postfix(ref bool __result)
        {
            if (PreventGhostConsumption)
            {
                __result = false;
                PreventGhostConsumption = false; //true flipper, necessary due to statics. i know youre smart enough to know that, this note is for me
            }
        }

        static string DrinkMessage(GameObject Object, VampireBloodMetabolism vitae)
        {
            return Object?.HasTag("WaterContainer") ?? false ? "Ahh, {{R sequence|refreshing}}! You feel " + vitae.UIBloodDisplay + "." : "You fall to your knees and sup {{R|blood}} from the ground. You feel " + vitae.UIBloodDisplay + ".";

        }
    }

    [HarmonyPatch(typeof(Stomach), nameof(Stomach.WaterStatus))]
    internal static class BloodStatus
    {

        [HarmonyPostfix]
        static void Postfix(ref string __result)
        {
            if (The.Player.TryGetPartDescendedFrom(out BaseBloodMetabolism vitae))
            {
                __result = vitae.UIBloodDisplay;
            }
        }
    }

    [HarmonyPatch(typeof(LiquidWater), nameof(LiquidWater.Drank))]
    internal static class WaterDrinking
    {
        [HarmonyPrefix]
        static bool Prefix(LiquidVolume Liquid, GameObject Target)
        {

            if (Target.HasPartDescendedFrom<BaseBloodMetabolism>() && Target.IsPlayer())
            {
                if (Liquid.IsPureLiquid())
                {
                    Popup.Show("It tastes inert. You feel no satisfaction from water.");
                    Target.FireEvent(Event.New("AfterDrank"));
                    return false;
                }
            }
            return true;
        }
        [HarmonyPostfix]
        static void Postfix(ref bool __result)
        {
            __result = true;
        }
    }

    [HarmonyPatch(typeof(PlayerStatusBar), "BeginEndTurn")]
    internal static class UIFreeDramsColor
    {

        static int GetFreeDramsReplacement(GameObject player)
        {
            var drams = player?.GetPart<VampireBloodMetabolism>();
            return drams is not null ? (int)drams.BloodDrams : player.GetFreeDrams("water", null, null, null, false);
        }
        //this is the one piece of code i didnt write
        //i have no idea how it works
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (Options.GetOptionBool(Rules.ModOptions.REDTEXT))
            {
                var codes = new List<CodeInstruction>(instructions);
                var target = AccessTools.Method(typeof(GameObject), "GetFreeDrams",
                    new[] { typeof(string), typeof(GameObject),
                    typeof(List<GameObject>),
                    typeof(System.Predicate<GameObject>), typeof(bool) });
                var repl = AccessTools.Method(typeof(UIFreeDramsColor),
                                                nameof(GetFreeDramsReplacement));

                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].Calls(target))
                    {

                        codes.RemoveRange(i - 5, 6);
                        codes.Insert(i - 5, new CodeInstruction(OpCodes.Call, repl));
                        break;
                    }
                }


                foreach (var c in codes)
                    if (c.opcode == OpCodes.Ldstr && c.operand is string s && s.Contains("blue"))
                        c.operand = s.Replace("blue", "red");

                return codes;
            }
            return null;
        }
    }
}