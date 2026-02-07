using XRL.World.Parts;
using XRL.World.Effects;
using XRL.World;
using Nexus.Properties;
using Nexus.Rules;
using System.Collections.Generic;
using System.Linq;
using System;
using Nexus.Core;

namespace Nexus.Core
{
    /// <summary>
    /// Assigns properties, adds parts and effects to Vampires.
    /// </summary>
    /// 

    static class VampireBuilder
    {
        // static readonly List<Effect> VampiricEffects = new()
        // {
        //     new HumanityUI(9999), new Bloodlust()
        // };
        public const string CORPSE = "Ashes";
        static readonly (string, int)[] IntProperties =
        {
            (FLAGS.BLOOD_VALUE, VITAE.BLOOD_GLUTTONOUS), (FLAGS.HUMANITY, HUMANITY.MAX), (FLAGS.REGEN, default)
        };

        static readonly (string, string)[] StringProperties =
        {
            (FLAGS.GO, FLAGS.FALSE), (FLAGS.FEED, FLAGS.FALSE), (FLAGS.FRENZY, FLAGS.FALSE),
            (FLAGS.BLOOD_STATUS, FLAGS.BLOOD.GLUT), (FLAGS.STEALTH, FLAGS.FALSE)
        };

        static readonly Type[] IParts =
        {
            typeof(Humanity), typeof(Vitae), typeof(Nightbeast), typeof(TheBeast)
        };
        public static readonly Type[] VampiricSpells = { typeof(GhoulSpell), typeof(CoffinSpell) };

        public static void Make(GameObject GO)
        {
            SetGameProperties(GO);
            SetVampireProperties(GO);
            RequireParts(GO);
            ChangeCorpse(GO);
        }

        public static void Unmake(GameObject GO)
        {
            RemoveGameProperties(GO);
            RemoveVampireProperties(GO);
            RemoveParts(GO);
            RevertCorpse(GO);
        }


        static void RemoveGameProperties(GameObject GO)
        {
            GO.SetStringProperty("WaterRitualLiquid", "water");
            if (GO.TryGetStringProperty("BleedLiquid", out string result) && result == OPTIONS.BLEEDLIQUID)
            {
                GO.SetStringProperty("BleedLiquid", "blood-1000");
            }
        }

        static void SetGameProperties(GameObject GO)
        {
            bool value = GO.IsPlayer();
            GO.SetStringProperty("WaterRitualLiquid", "blood");
            if (XRL.UI.Options.GetOptionBool(OPTIONS.BLOOD_NERF) && value)
            {
                SetBleedLiquid(GO);
            }
            else if (!value)
            {
                SetBleedLiquid(GO);
            }
        }

        static void SetBleedLiquid(GameObject GO)
        {
            if (GO.TryGetStringProperty("BleedLiquid", out string result))
            {
                if (result.IsNullOrEmpty() || result == "blood-1000")
                {
                    GO.SetStringProperty("BleedLiquid", OPTIONS.BLEEDLIQUID);
                }
            }
            else
                GO.SetStringProperty("BleedLiquid", OPTIONS.BLEEDLIQUID);
        }
        static void RequireParts(GameObject GO)
        {
            for (int i = 0; i < IParts.Length; i++)
                GO.RequirePart(IParts[i]);
            GO.ApplyEffect(new HumanityUI(9999));
            if (XRL.UI.Options.GetOptionBool(OPTIONS.SPELLS))
                RequireVampiricObjects(GO);
        }



        static void SetVampireProperties(GameObject GO)
        {
            for (int i = 0; i < StringProperties.Length; i++)
                GO.SetStringProperty(StringProperties[i].Item1, StringProperties[i].Item2);
            for (int i = 0; i < IntProperties.Length; i++)
                GO.SetIntProperty(IntProperties[i].Item1, IntProperties[i].Item2);
        }

        static void RemoveParts(GameObject GO)
        {
            for (int i = 0; i < IParts.Length; i++)
                GO.RemovePart(IParts[i]);
            GO.RemoveEffect<HumanityUI>();
            GO.RemoveEffect<Bloodlust>();
            RemoveVampiricObjects(GO);
        }

        public static void RequireVampiricObjects(GameObject GO)
        {
            for (int i = 0; i < VampiricSpells.Length; i++)
                if (GO.RequiresPart<VampiricSpell>(VampiricSpells[i], out var obj))
                    obj.RequireObject();

        }


        public static void RemoveVampiricObjects(GameObject GO)
        {
            for (int i = 0; i < VampiricSpells.Length; i++)
            {
                var obj = GO.GetPart<VampiricSpell>(VampiricSpells[i]);
                obj?.RemoveObject();
            }

        }

        static void RemoveVampireProperties(GameObject GO)
        {
            for (int i = 0; i < StringProperties.Length; i++)
                GO.RemoveStringProperty(StringProperties[i].Item1);
            for (int i = 0; i < IntProperties.Length; i++)
                GO.RemoveIntProperty(IntProperties[i].Item1);
        }

        //will need to write reset code that changes the corpse back to its original code
        //might make a part

        static void RevertCorpse(GameObject GO)
        {
            var ashes = GO.GetPart<VampireAshes>();
            if (ashes.HasCopyData)
                GO.AddPart(ashes.Revert());
            GO.RemovePart(ashes);
        }
        public static void ChangeCorpse(GameObject GO)
        {
            if (!GO.HasPart<VampireAshes>())
            {
                if (GO.TryGetPart<Corpse>(out var Corpse))
                {
                    VampireAshes ashes = new(Corpse.BurntCorpseBlueprint, Corpse.VaporizedCorpseBlueprint, Corpse.CorpseBlueprint, Corpse.BurntCorpseChance, Corpse.CorpseChance, Corpse.VaporizedCorpseChance);
                    GO.AddPart(ashes);
                    GO.RemovePart(Corpse);
                }
                else
                    GO.AddPart<VampireAshes>();
                if (GO.TryGetIntProperty("SuppressCorpseDrops", out int prop) && prop > 0)
                    GO.SetIntProperty("SuppressCorpseDrops", 0);
            }
        }
    }
}

namespace XRL.World.Parts
{

    /// <summary>
    /// Inheritor of Corpse with additional fields to backup and store the blueprints for the original corpse.
    /// </summary>
    /// 
    [Serializable]
    public class VampireAshes : Corpse
    {
        public bool HasCopyData = false;
        public string OldBurntCorpseBlueprint = default;
        public string OldVaporizedCorpseBlueprint = default;
        public string OldCorpseBlueprint = default;
        public int OldBurntCorpseChance = default;
        public int OldCorpseChance = default;
        public int OldVaporizedCorpseChance = default;
        /// <summary>
        /// For objects that do not have a corpse part.
        /// </summary>
        public VampireAshes()
        {
            CorpseBlueprint = VampireBuilder.CORPSE;
            VaporizedCorpseBlueprint = VampireBuilder.CORPSE;
            BurntCorpseBlueprint = VampireBuilder.CORPSE;
            BurntCorpseChance = 100;
            CorpseChance = 100;
            VaporizedCorpseChance = 100;
        }


        /// <summary>
        /// For backing up corpses.
        /// </summary>
        public VampireAshes(string BurntCorpseBlueprint, string VaporizedCorpseBlueprint, string CorpseBlueprint, int BurntCorpseChance, int CorpseChance, int VaporizedCorpseChance) : this()
        {
            OldBurntCorpseBlueprint = BurntCorpseBlueprint.IsNullOrEmpty() ? default : BurntCorpseBlueprint;
            OldVaporizedCorpseBlueprint = VaporizedCorpseBlueprint.IsNullOrEmpty() ? default : VaporizedCorpseBlueprint;
            OldCorpseBlueprint = CorpseBlueprint.IsNullOrEmpty() ? default : CorpseBlueprint;
            OldBurntCorpseChance = BurntCorpseChance;
            OldCorpseChance = CorpseChance;
            OldVaporizedCorpseChance = VaporizedCorpseChance;
            HasCopyData = true;
        }
        public Corpse Revert()
        {
            Corpse corpse = new()
            {
                BurntCorpseBlueprint = OldBurntCorpseBlueprint,
                VaporizedCorpseBlueprint = OldVaporizedCorpseBlueprint,
                CorpseBlueprint = OldCorpseBlueprint,
                BurntCorpseChance = OldBurntCorpseChance,
                CorpseChance = OldCorpseChance,
                VaporizedCorpseChance = OldVaporizedCorpseChance
            };
            return corpse;
        }
    }
}