using XRL.UI;
using HarmonyLib;
using XRL.World;
using XRL.World.Parts;
using System.Collections.Generic;
using System.Reflection.Emit;
using XRL;
using System.Text;
using XRL.World.Parts.Mutation;
using Nexus.Rules;


namespace Nexus.Patches
{

    [HarmonyPatch(typeof(XRL.Liquids.LiquidBlood), nameof(XRL.Liquids.LiquidBlood.Drank))]

    //really did not want to have to use harmony for this mod, but alas
    //fortunately, the drank methods really arent that big
    public static class Drink_Blood

    {
        static bool PreventGhostConsumption; //prevents blood from being consumed if you refuse to drink while vomitting

        [HarmonyPrefix]

        public static bool Prefix(LiquidVolume Liquid, GameObject Target, StringBuilder Message)
        {
            if (Target.TryGetPart(out Vitae vitae) && Target.IsPlayer())
            {
                if (Liquid.IsPureLiquid())
                {
                    if (vitae.Blood >= VITAE.SIP_PUKE_WARN)
                    {
                        if (PreventGhostConsumption = vitae.IDontWantToPuke(false))
                            return false;
                    }
                    if (Options.GetOptionBool(OPTIONS.HUNTER))
                    {
                        Popup.Show("This does not satisfy - you need living blood.");
                        Target.FireEvent(Event.New("AfterDrank"));
                        return false;
                    }
                    else
                    {
                        vitae.Drink(false);
                        Popup.Show("Ahh, {{R sequence|refreshing}}! You feel " + vitae.BloodStatus() + ".");
                        return false;
                    }
                }
                else
                    Popup.Show("Disgusting! This blood is ruined! You feel " + vitae.BloodStatus() + ".");
            }
            return true;
        }


        [HarmonyPostfix]
        static void Postfix(ref bool __result)
        {
            if (!PreventGhostConsumption)
                __result = true;
            else
            {
                __result = false;
                PreventGhostConsumption = false; //true flipper, necessary due to statics. i know youre smart enough to know that, this note is for me
            }
        }
    }


    [HarmonyPatch(typeof(XRL.World.Parts.Stomach), nameof(XRL.World.Parts.Stomach.WaterStatus))]
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

    [HarmonyPatch(typeof(XRL.Liquids.LiquidWater), nameof(XRL.Liquids.LiquidWater.Drank))]


    public static class Drank_Water
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
        static void Postfix(ref bool __result)
        {
            __result = true;
        }
    }

    [HarmonyPatch(typeof(Qud.UI.PlayerStatusBar), "BeginEndTurn")]
    public static class WaterUIThing
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
            var codes = new List<CodeInstruction>(instructions);
            var target = AccessTools.Method(typeof(GameObject), "GetFreeDrams",
                new[] { typeof(string), typeof(GameObject),
                    typeof(List<GameObject>),
                    typeof(System.Predicate<GameObject>), typeof(bool) });
            var repl = AccessTools.Method(typeof(WaterUIThing),
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
    }
}