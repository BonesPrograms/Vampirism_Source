using XRL.UI;
using HarmonyLib;
using XRL.World;
using XRL.World.Parts;
using System.Collections.Generic;
using System.Reflection.Emit;
using XRL;
using XRL.World.Parts.Mutation;
using Nexus.Rules;
using XRL.Liquids;
using Qud.UI;
using System;

namespace Nexus.Patches
{

				///NOTE: found a heal string event in Albino that is useful for interrupting heal without throwing messages in chat
	// [HarmonyPatch(typeof(GameObject), nameof(GameObject.HealsNaturally))]

	// public static class GhoulHealPatch
	// {
	// 	[HarmonyPostfix]
	// 	public static void Postfix(GameObject __instance, ref bool __result)
	// 	{
	// 		if (__instance.TryGetEffect(out EnthralledGhoul ghoul) && ghoul.WasFedOn)
	// 			__result = false;
	// 	}
	// }

    [HarmonyPatch(typeof(TorchProperties), nameof(TorchProperties.HandleEvent), new Type[] { typeof(InventoryActionEvent) })]
    public static class TorchPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref bool __result, TorchProperties __instance, InventoryActionEvent E)
        {
            if (__result == true && E.Command == "TorchLight" && Options.GetOptionBool(OPTIONS.FIRE) && Options.GetOptionBool(OPTIONS.TORCH) && Core.QudExtensions.IsVampire(E.Actor, out var v))
            {
                __result = false;
                v.FakeDropTorch(__instance.ParentObject);
            }
        }
    }

    [HarmonyPatch(typeof(LiquidBlood), nameof(LiquidBlood.Drank))]
    public static class Blood
    {
        static bool PreventGhostConsumption; //prevents blood from being consumed if you refuse to drink while vomitting

        [HarmonyPrefix]

        public static bool Prefix(LiquidVolume Liquid, GameObject Target)
        {
            if (Target.TryGetPart(out Vitae vitae) && Target.IsPlayer())
            {
                if (Liquid.IsPureLiquid())
                {
                    if (Options.GetOptionBool(OPTIONS.HUNTER))
                    {
                        Popup.Show("This does not satisfy - you need living blood.");
                        Target.FireEvent(Event.New("AfterDrank"));
                        return false;
                    }
                    if (vitae.Blood >= VITAE.SIP_PUKE_WARN)
                    {
                        if (PreventGhostConsumption = vitae.IDontWantToPuke(false, false))
                            return false;
                    }
                    vitae.Drink(false, false);
                    Popup.Show(DrinkMessage(Liquid.ParentObject, vitae));
                    return false;
                }
                else
                    Popup.Show("Disgusting! This blood is ruined! You feel " + vitae.BloodStatus() + ".");
            }
            return true;
        }

        [HarmonyPostfix]
        public static void Postfix(ref bool __result)
        {
            if (!PreventGhostConsumption)
                __result = true;
            else
            {
                __result = false;
                PreventGhostConsumption = false; //true flipper, necessary due to statics. i know youre smart enough to know that, this note is for me
            }
        }

        static string DrinkMessage(GameObject Object, Vitae vitae)
        {
            return Object?.HasTag("WaterContainer") ?? false ? "Ahh, {{R sequence|refreshing}}! You feel " + vitae.BloodStatus() + "." : "You fall to your knees and sup {{R|blood}} from the ground. You feel " + vitae.BloodStatus() + ".";

        }
    }

    [HarmonyPatch(typeof(Stomach), nameof(Stomach.WaterStatus))]
    public static class Status
    {

        [HarmonyPostfix]
        public static void Postfix(ref string __result)
        {
            if (The.Player.TryGetPart(out Vitae vitae))
            {
                __result = vitae.BloodStatus();
            }
        }
    }

    [HarmonyPatch(typeof(LiquidWater), nameof(LiquidWater.Drank))]
    public static class Water
    {
        [HarmonyPrefix]
        public static bool Prefix(LiquidVolume Liquid, GameObject Target)
        {

            if (Target.HasPart<Vampirism>() && Target.IsPlayer())
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
        public static void Postfix(ref bool __result)
        {
            __result = true;
        }
    }

    [HarmonyPatch(typeof(PlayerStatusBar), "BeginEndTurn")]
    public static class UI
    {

        static int GetFreeDramsReplacement(GameObject player)
        {
            var drams = player?.GetPart<Vitae>();
            return drams is not null ? (int)drams.BloodDrams : player.GetFreeDrams("water", null, null, null, false);
        }
        //this is the one piece of code i didnt write
        //i have no idea how it works
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (Options.GetOptionBool(Rules.OPTIONS.REDTEXT))
            {
                var codes = new List<CodeInstruction>(instructions);
                var target = AccessTools.Method(typeof(GameObject), "GetFreeDrams",
                    new[] { typeof(string), typeof(GameObject),
                    typeof(List<GameObject>),
                    typeof(System.Predicate<GameObject>), typeof(bool) });
                var repl = AccessTools.Method(typeof(UI),
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