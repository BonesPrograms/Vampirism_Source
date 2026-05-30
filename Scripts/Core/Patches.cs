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
using VampirismSys.Extensions;
using XRL.World.Effects;
using XRL.World.Parts.Mutation;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine.SocialPlatforms;

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
    internal static class AutogetSilverAilment
    {
        [HarmonyPostfix]
        static void Postfix(ref bool __result, GameObject __instance)
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
                    if (vitae.Blood >= Rules.Metab.SIP_PUKE_WARN)
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


    //the purpose of this patch is to change the color of your $water count from blue to red
    //also it will make it display $blood instead of $water
    //(new) it will also patch a bug for mechanical vampires that dont have stomachs and thus dont have a blood thirst status
    //it will add an elseif statement if your stomach is null that checks if you are a vampire and then adds your blood status to the ui bar if true

    //the code were taking a look at is between lines 357-365, 

    //note: the way insertion works, our instructions will be inserted *before* the instruction that we are targetting via index

    [HarmonyPatch(typeof(PlayerStatusBar), "BeginEndTurn")]
    [HarmonyDebug]
    public static class BloodDisplayForVampires
    {
        public static void MechanicalFix(StringBuilder sb)
        {
            VampireBloodMetabolism metab = The.Player.GetPart<VampireBloodMetabolism>();
            sb.Length = 0;
            if(metab != null)
            sb.Append($" {metab.UIBloodDisplay}");
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> input, ILGenerator il)
        {
            List<CodeInstruction> codes = input.ToList();

            MethodInfo getpart = AccessTools.Method(typeof(GameObject), nameof(GameObject.GetPart), null, new[] { typeof(Stomach) });
            int callIndex = (int)GetInstruction(codes, OpCodes.Callvirt, getpart);
            LocalBuilder isVampire = il.DeclareLocal(typeof(bool)); //this will represent the result of if the player is a vampire
            InjectVampireCheck(codes, isVampire, callIndex);

            HijackJumpLabel(codes, il, callIndex, out Label newJump, out Label? originalJump);

            Type enumType = AccessTools.Inner(typeof(PlayerStatusBar), "StringDataType");
            MethodInfo updateString = AccessTools.Method(typeof(PlayerStatusBar), "UpdateString", new[] { enumType, typeof(StringBuilder), typeof(bool) });
            int jumpIndex = (int)GetInstruction(codes, OpCodes.Call, updateString, callIndex);
            InjectElseIfStatement(codes, updateString, il, isVampire, jumpIndex, newJump, (Label)originalJump);

            int waterIndex = (int)GetLdstrInstruction(codes, "water", jumpIndex);
            InjectStringJump(codes, isVampire, il, waterIndex, "blood", "water");

            int blueIndex = (int)GetLdstrInstruction(codes, "# {{blue|", waterIndex);
            InjectStringJump(codes, isVampire, il, blueIndex, "# {{red|", "# {{blue|");

            foreach (var code in codes)
                yield return code;
        }

        static void InjectVampireCheck(List<CodeInstruction> codes, LocalBuilder boolean, int callIndex)
        {
            MethodInfo isVampire = AccessTools.Method(typeof(QudExtensions), nameof(QudExtensions.IsVampire), new[] { typeof(GameObject) });
            CodeInstruction[] injectBool = new CodeInstruction[]
            {
                new(OpCodes.Ldloc_0),
                new(OpCodes.Call, isVampire),
                new(OpCodes.Stloc_S, boolean)
            };
            callIndex--; //shifts index back so that it gets injected before getpart loads the GameObject onto the stack
            Inject(codes, callIndex, injectBool);
        }
        static void HijackJumpLabel(List<CodeInstruction> codes, ILGenerator il, int position, out Label newJump, out Label? originalJump)
        {
            originalJump = null;
            newJump = il.DefineLabel();
            for (int i = position; i < codes.Count; i++)
            {
                var code = codes[i];
                if (code.opcode == OpCodes.Brfalse_S)
                {
                    originalJump = (Label)code.operand; //i store the original jump label to be used in my instruction incase you fail my else-if condition
                    code.operand = newJump; //replace the original, now if your stomach == null, you will jump to my else-if statement
                    return;
                }
            }
        }
        static void InjectElseIfStatement(List<CodeInstruction> codes, MethodInfo updateString, ILGenerator il, LocalBuilder boolean, int injectIndex, Label newJump, Label originalJump)
        {
            injectIndex++; //shifts index forward so instruction is inserted after UpdateString call
            MethodInfo fix = AccessTools.Method(typeof(BloodDisplayForVampires), nameof(MechanicalFix));
            FieldInfo sb = AccessTools.Field(typeof(PlayerStatusBar), "sb");
            MethodInfo setLength = AccessTools.PropertySetter(typeof(StringBuilder), "Length");
            Label isVampireJump = il.DefineLabel();
            CodeInstruction[] injectUpdateString = new CodeInstruction[]
            {
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, sb),
                new(OpCodes.Ldc_I4_0), //FoodWater value for stringdatatype enum
                new(OpCodes.Call, updateString),
            };
            List<CodeInstruction> injectElse = new()
            {
                new(OpCodes.Br_S, originalJump),//this gets inserted before the null-stomach jump target; to prevent it from running if you have a stomach
                new(OpCodes.Ldloc_S, boolean), //so if you are already here you definitely do not have a stomach and were only checking for vampirism
                new(OpCodes.Brtrue_S, isVampireJump), // if true jump to ldarg_0 below Br instruction, false continue
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, sb), //the instructions between Brtrue_S and Br fix a big - doc below (this really makes it an else-elseif-else)
                new(OpCodes.Ldc_I4_0),                                                                    //if(stomach != null)
                new(OpCodes.Callvirt, setLength), //updatestring call will be injected after this           //elseif(!isVampire) 
                new(OpCodes.Br, originalJump), //original jump position that we hijacked is assigned as the jump for failing the vampire check
                new(OpCodes.Ldarg_0),//isVampireJump targets here (you are a vampire)
                new(OpCodes.Ldfld, sb),
                new(OpCodes.Call, fix), //so this makes it so that mechanical vampires can see their blood thirst
                //updateString call will be injected here
                //after that series of instructions, original label/jump target for failure is right here
            };
            injectElse[1].labels.Add(newJump);//this is what causes the code to jump here if stomach == null, ldloc_s is the new target, elseif is created
            injectElse[^3].labels.Add(isVampireJump); 
            Inject(injectElse, injectElse.Count - 4, injectUpdateString); //injects update string call after callvirt setlength
            Inject(injectElse, injectElse.Count, injectUpdateString);//injects update stringcall after call fix
            Inject(codes, injectIndex, injectElse);
        }

        //Bug Docuemntation:
        //If you play as a mechanical vampire, you dont have a normal stomach thirst/hunger display, so my code takes over and displays blood thirst.
        //However, if you start a different game as a mechanical non-vampire during the same session
        //you will not get any updates to your FoodWater display, because you dont have a stomach/arent a vampire
        //So my string will persist  and not receive any updates
        //So if you are not a vampire and you are mechanical it sets the sblength to 0 and calls updatestring
        //probalby could be better, i am going to experiment with dominating mechanical creatures (if thats possible) and seeing how the game does it normally

        //this jump conditional is relative to if the player is a vampire
        static void InjectStringJump(List<CodeInstruction> codes, LocalBuilder boolean, ILGenerator il, int injectIndex, string ifTrue, string ifFalse)
        {
            codes.RemoveAt(injectIndex); //removes target string load (because were going to be shifting it around)
            Label trueJump = il.DefineLabel();
            Label falseJump = il.DefineLabel();
            CodeInstruction[] inject = new CodeInstruction[]
            {
                new(OpCodes.Ldloc_S, boolean),
                new(OpCodes.Brtrue_S, trueJump),
                new(OpCodes.Ldstr, ifFalse), //here would usually be the original string load
                new(OpCodes.Br_S, falseJump),
                new(OpCodes.Ldstr, ifTrue), //truejumptarget
                //false jump target would be right here
            };
            codes[injectIndex].labels.Add(falseJump); //marks the next position in the original instruction the false jump point (original target)
            inject[^1].labels.Add(trueJump); //marks the end of this array as the true jump point
            Inject(codes, injectIndex, inject);
        }
        static void Inject(List<CodeInstruction> codes, int injectIndex, IList<CodeInstruction> inject)
        {
            for (int i = inject.Count - 1; i >= 0; i--)
            {
                codes.Insert(injectIndex, inject[i]);
            }
        }
        static int? GetLdstrInstruction(List<CodeInstruction> codes, string operand, int startIndex = 0)
        {
            return GetInstruction(codes, OpCodes.Ldstr, operand, startIndex);
        }
        static int? GetInstruction(List<CodeInstruction> codes, OpCode opCode, object operand, int startIndex = 0)
        {
            for (int i = startIndex; i < codes.Count; i++)
            {
                var code = codes[i];
                if (code.opcode == opCode && Equals(code.operand, operand))
                    return i; //returns index of instruction in codes list
            }
            return null;
        }

        //This is old AI code that I formerly used to achieve this patch
        //I like to stare at it because its not total shit Im planning on using some ideas here to make a more "efficient" transpiler later
        //since the current one calls GetFreeDrams twice

        //problem with this one is you could not change the color at runtime
        //but it is technically more efficient with one less call to getfreedrams
        //and it doesnt do the method insertion thing that i do, it just replaces an existing call

        // static int GetFreeDramsReplacement(GameObject player)
        // {
        //     var drams = player?.GetPart<VampireBloodMetabolism>();
        //     return drams is not null ? (int)drams.BloodDrams : player.GetFreeDrams("water", null, null, null, false);
        // }

        // static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        // {
        //     if (Options.GetOptionBool(Rules.ModOptions.REDTEXT))
        //     {
        //         var codes = new List<CodeInstruction>(instructions);
        //         var target = AccessTools.Method(typeof(GameObject), "GetFreeDrams",
        //             new[] { typeof(string), typeof(GameObject),
        //             typeof(List<GameObject>),
        //             typeof(System.Predicate<GameObject>), typeof(bool) });
        //         var repl = AccessTools.Method(typeof(UIFreeDramsColor),
        //                                         nameof(GetFreeDramsReplacement));

        //         for (int i = 0; i < codes.Count; i++)
        //         {
        //             if (codes[i].Calls(target))
        //             {

        //                 codes.RemoveRange(i - 5, 6);
        //                 codes.Insert(i - 5, new CodeInstruction(OpCodes.Call, repl));
        //                 break;
        //             }
        //         }


        //         foreach (var c in codes)
        //             if (c.opcode == OpCodes.Ldstr && c.operand is string s && s.Contains("blue"))
        //                 c.operand = s.Replace("blue", "red");

        //         return codes;
        //     }
        //     return null;
        // }
    }
}