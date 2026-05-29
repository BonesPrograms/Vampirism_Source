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

    [HarmonyPatch(typeof(PlayerStatusBar), "BeginEndTurn")]

    //the purpose of this patch is to change the color of your $water count from blue to red
    //also it will make it display $blood instead of $water

    //the code were taking a look at is between lines 357-365, 
    //here is what it looks like in il

    // 	IL_0104: ldloc.0
    // IL_0105: callvirt instance int32 XRL.World.GameObject::GetCarriedWeight() /* 06005D2B */
    // IL_010a: stloc.s 8
    // IL_010c: ldloc.0
    // IL_010d: callvirt instance int32 XRL.World.GameObject::GetMaxCarriedWeight() /* 06005D2C */
    // IL_0112: stloc.s 9
    // IL_0114: ldloc.0
    // IL_0115: ldstr "water" /* 70039B83 */
    // IL_011a: ldnull
    // IL_011b: ldnull
    // IL_011c: ldnull
    // IL_011d: ldc.i4.0
    // IL_011e: callvirt instance int32 XRL.World.GameObject::GetFreeDrams(string, class XRL.World.GameObject, class [mscorlib]System.Collections.Generic.List`1<class XRL.World.GameObject>, class [mscorlib]System.Predicate`1<class XRL.World.GameObject>, bool) /* 06005C3A */
    // IL_0123: stloc.s 10
    // IL_0125: ldarg.0
    // IL_0126: ldfld class [mscorlib]System.Text.StringBuilder Qud.UI.PlayerStatusBar::sb /* 04001977 */
    // IL_012b: ldc.i4.0
    // IL_012c: callvirt instance void [mscorlib]System.Text.StringBuilder::set_Length(int32) /* 0A00025F */
    // IL_0131: ldarg.0
    // IL_0132: ldfld class [mscorlib]System.Text.StringBuilder Qud.UI.PlayerStatusBar::sb /* 04001977 */
    // IL_0137: ldloc.s 8
    // IL_0139: callvirt instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::Append(int32) /* 0A00022E */
    // IL_013e: ldstr "/" /* 7001B0FB */
    // IL_0143: callvirt instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::Append(string) /* 0A0001F4 */
    // IL_0148: ldloc.s 9
    // IL_014a: call string Extensions::ToStringCached(int32) /* 060000E0 */
    // IL_014f: callvirt instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::Append(string) /* 0A0001F4 */
    // IL_0154: ldstr "# {{blue|" /* 7003C622 */
    // IL_0159: callvirt instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::Append(string) /* 0A0001F4 */
    // IL_015e: ldloc.s 10
    // IL_0160: call string Extensions::ToStringCached(int32) /* 060000E0 */
    // IL_0165: callvirt instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::Append(string) /* 0A0001F4 */
    // IL_016a: ldstr "$}}" /* 7003C636 */
    // IL_016f: callvirt instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::Append(string) /* 0A0001F4 */
    // IL_0174: pop


    //after this pop the instructions we will actually be working with can be found

    // IL_0175: ldarg.0
    // IL_0176: ldc.i4.3
    // IL_0177: ldarg.0
    // IL_0178: ldfld class [mscorlib]System.Text.StringBuilder Qud.UI.PlayerStatusBar::sb /* 04001977 */
    // IL_017d: ldc.i4.0
    // IL_017e: call instance void Qud.UI.PlayerStatusBar::UpdateString(valuetype Qud.UI.PlayerStatusBar/StringDataType, class [mscorlib]System.Text.StringBuilder, bool) /* 06002D14 */

    //dont rly know what im doing so im taking the easy route
    //i will be inserting a call to my method ReplaceWaterWithBlood which takes a stringbuilder gameobject and int32
    //this call will be inserted right before IL_017e
    //the int32 is your water count, i will be retrieving this from the stack and sending it as a parameter so that i can find it's value in the stringbuilder and replace it with blood val
    //we use .replace on the stringbuilder to replace the color blue with red

    //after writing all this, i just realized I couldve done a Postfix to UpdateString, which has the stringbuilder sent to it as a parameter, and done my modifications there
    //in normal C#
    //but we already wrote this and its pretty cool so im just gonna leave it
    public static class BloodDramsForVampires
    {

        public static void ReplaceWaterWithBlood(StringBuilder sb, GameObject player, int wtr)
        {
            if (player.TryGetPart(out VampireBloodMetabolism metab))
            {
                if (Options.GetOptionBool(ModOptions.REDTEXT))
                    sb.Replace("blue", "red");
                sb.Replace(wtr.ToStringCached(), metab.BloodDrams.ToStringCached());
            }
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> DisplayInjector(IEnumerable<CodeInstruction> input)
        {
            List<CodeInstruction> codes = input.ToList();
            int? callindx = GetInsertionPoint(codes, out LocalBuilder waterVarIndex);
            if (callindx is int index && waterVarIndex != null)
            {
                index = RemoveInstructions(codes, index);
                MethodInfo methodInsert = AccessTools.Method(typeof(BloodDramsForVampires), nameof(ReplaceWaterWithBlood));
                FieldInfo sbField = AccessTools.Field(typeof(PlayerStatusBar), "sb");
                InsertInstructions(codes, methodInsert, index, waterVarIndex, sbField);
            }
            foreach (var code in codes)
                yield return code;
        }

        static void InsertInstructions(List<CodeInstruction> codes, MethodInfo methodInsert, int index, LocalBuilder waterVarIndex, FieldInfo sbField)
        {
            CodeInstruction[] inject = new CodeInstruction[]
            {
             new(OpCodes.Ldloc_0), //we reload the gameobject onto the stack (the player)
             new(OpCodes.Ldloc_S, waterVarIndex), //here we load local variable that holds the value of water, using its local variable index
             new(OpCodes.Call, methodInsert), //they are sent as parameters to ReplaceWaterWithBlood
             new(OpCodes.Ldarg_0), //the 'this' we removed is placed back onto the stack (at this point we are duplicating the original instructions)
             new(OpCodes.Ldc_I4_3), //enum that we previously removed from instructions is loaded onto the stack
             new(OpCodes.Ldarg_0), //'this' is reloaded onto the stack 
             new(OpCodes.Ldfld, sbField), //stringbuilder field is reloaded (we hijacked the original stringbuilder that was already on the stack)
             new(OpCodes.Ldc_I4_0) //default bool value for updatestring that we removed is loaded onto the stack
            };                      //instructions will now continue from IL_017e
            for (int i = inject.Length - 1; i >= 0; i--) //our array is written in a way that resembles actual IL, top-down instructions
            {                                             //but because of how List.Insert works, we need to actually insert it backwards
                codes.Insert(index, inject[i]);
            }

        }

        static int RemoveInstructions(List<CodeInstruction> codes, int index)
        {
            for (int i = index; i >= 0; i--) //here we are counting back UP from the updatestring call to remove 'this' and one of its enum parameters from the stack
            {
                var code = codes[i];
                if (code.opcode == OpCodes.Ldc_I4_3)
                {
                    codes.RemoveAt(i - 1); //removes an instruction that loads 'this' for the updatestring method call
                    i--; //shifts index back to ldc_i4_3 instruction's new position in the array
                    codes.RemoveAt(i); //removes ldc_i4_3 instruction (this was being set up as part of parameters for an UpdateString call)
                    break;
                }

            }
            index -= 2; //2 instructions were removed, index is shifted back by 2 to keep it in position on UpdateString call
            codes.RemoveAt(index - 1); //boolean that occurs just before UpdateString is removed
            index--; //index decremented again to keep our position on the updatestring call
            return index; //now the enum and boolean parameter for UpdateString have been removed from the stack and the stringbuilder is on the top of the stack
        }

        static int? GetInsertionPoint(List<CodeInstruction> codes, out LocalBuilder waterVarIndex)
        {
            Type stringDataEnum = AccessTools.Inner(typeof(PlayerStatusBar), "StringDataType");
            MethodInfo freeDrams = AccessTools.Method(typeof(GameObject), nameof(GameObject.GetFreeDrams), new[] { typeof(string), typeof(GameObject), typeof(List<GameObject>), typeof(Predicate<GameObject>), typeof(bool) });
            MethodInfo updateString = AccessTools.Method(typeof(PlayerStatusBar), "UpdateString", new[] { stringDataEnum, typeof(StringBuilder), typeof(bool) });
            waterVarIndex = GetWaterIndex(codes, freeDrams, out int index);//index may be confusing here - the index for the local variable that holds the value of water is in the LocalBuilder
            for (int i = index; i < codes.Count; i++)  //out int index refers to where we will keep counting from when we begin the next loop as you see here
            {
                var code = codes[i];
                if (code.opcode == OpCodes.Call && code.operand is MethodInfo method && method == updateString)
                    return i; //this gets the index of the updatestring method, which is the location we will be inserting out instructions into
            }
            return null;
        }

        static LocalBuilder GetWaterIndex(List<CodeInstruction> codes, MethodInfo freeDrams, out int index)
        {
            index = default;
            for (int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                if (code.opcode == OpCodes.Callvirt && code.OperandIs(freeDrams)) //bug documentation below
                {
                    index = i + 1; //the next instruction is stloc_s so we step to the next instruction and retrieve it's local variable index from the operand
                    return codes[index].operand as LocalBuilder;//which holds the int value for water
                }
            }
            return null;
        }

        //Bug Docuemntation:
        //Originally, all the code you see in these two for loops, was in one giant for loop, so i only iterated once instead of twice
        //However, for some reason, when I was trying to find the opcode that was calling the FreeDrams method, none of the operands were returning true as the FreeDrams method
        //i was only able to fix this bug by looking for the freedrams method in a separate for loop, as you see above
        //the code was literally the same, swear to god, if you copy paste the forloop from "GetWaterIndex" into the forloop in "GetInsertionPoint", it does not work because:
        //for some reason, no operands return true as being the FreeDrams method


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