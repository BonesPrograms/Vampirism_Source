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
        public static bool ENABLE_SPELLS = false;
        public const string CORPSE = "Ashes";
        static readonly (string, int)[] IntProperties =
        {
            (Flags.BLOOD_VALUE, Rules.Vitae.BLOOD_GLUTTONOUS), (Flags.HUMANITY, Rules.Humanity.MAX), (Flags.REGEN, default)
        };

        public static readonly (string, string)[] StringProperties =
        {
            (Flags.GO, Flags.FALSE), (Flags.FEED, Flags.FALSE), (Flags.FRENZY, Flags.FALSE),
            (Flags.BLOOD_STATUS, Flags.Blood.GLUT), (Flags.STEALTH, Flags.FALSE)
        };
        static readonly Type[] IParts =
        {
            typeof(XRL.World.Parts.Humanity), typeof(XRL.World.Parts.Vitae), typeof(Nightbeast), typeof(TheBeast)
        };
        static readonly Type[] VampiricSpells =
        {
            typeof(GhoulSpell), typeof(CoffinSpell), typeof(EmbraceSpell), typeof(BatformSpell)
        };

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
            if (GO.TryGetStringProperty("BleedLiquid", out string result) && result == ModOptions.BLEEDLIQUID)
            {
                GO.SetStringProperty("BleedLiquid", "blood-1000");
            }
        }

        static void SetGameProperties(GameObject GO)
        {
            bool value = GO.IsPlayer();
            GO.SetStringProperty("WaterRitualLiquid", "blood");
            if (XRL.UI.Options.GetOptionBool(ModOptions.BLOOD_NERF) && value)
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
                    GO.SetStringProperty("BleedLiquid", ModOptions.BLEEDLIQUID);
                }
            }
            else
                GO.SetStringProperty("BleedLiquid", ModOptions.BLEEDLIQUID);
        }
        static void RequireParts(GameObject GO)
        {
            IParts.ForEach(x => GO.AddPart((IPart)Activator.CreateInstance(x)));
            GO.ApplyEffect(new HumanityUI(9999));
            if (XRL.UI.Options.GetOptionBool(ModOptions.SPELLS) && GO.IsPlayer())
                RequireSpells(GO);
        }



        static void SetVampireProperties(GameObject GO)
        {
            StringProperties.ForEach(x => GO.SetStringProperty(x.Item1, x.Item2));
            IntProperties.ForEach(x => GO.SetIntProperty(x.Item1, x.Item2));
        }

        static void RemoveParts(GameObject GO)
        {
            IParts.ForEach(x => GO.RemovePart(x));
            GO.RemoveEffect<HumanityUI>();
            GO.RemoveEffect<Bloodlust>();
            if (GO.TryGetStringProperty(Flags.SPELLS, out var spells) && spells == Flags.TRUE)
                RemoveSpells(GO);
        }

        public static void RequireSpells(GameObject GO)
        {
            if (ENABLE_SPELLS)
            {
                XRL.UI.Popup.Suppress = true;
                VampiricSpells.Select(x => (VampiricSpell)Activator.CreateInstance(x)).ForEach(x => { GO.AddPart(x); x.AddSpell(); });
                GO.SetStringProperty(Flags.SPELLS, Flags.TRUE);
                XRL.UI.Popup.Suppress = false;
            }
        }


        public static void RemoveSpells(GameObject GO)
        {
            if (ENABLE_SPELLS)
            {
                VampiricSpells.Select(x => (VampiricSpell)GO.GetPart(x)).ForEach(x => x.RemoveSpell());
                GO.SetStringProperty(Flags.SPELLS, Flags.FALSE);
            }

        }

        static void RemoveVampireProperties(GameObject GO)
        {
            StringProperties.ForEach(x => GO.RemoveStringProperty(x.Item1));
            IntProperties.ForEach(x => GO.RemoveIntProperty(x.Item1));
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
            if (GO.TryGetPart<Corpse>(out var Corpse))
            {
                VampireAshes ashes = new(Corpse.BurntCorpseBlueprint, Corpse.VaporizedCorpseBlueprint, Corpse.CorpseBlueprint, Corpse.BurntCorpseChance, Corpse.CorpseChance, Corpse.VaporizedCorpseChance);
                GO.AddPart(ashes);
                GO.RemovePart(Corpse);
            }
            else
                GO.RequirePart<VampireAshes>();
            if (GO.TryGetIntProperty("SuppressCorpseDrops", out int prop) && prop > 0)
                GO.SetIntProperty("SuppressCorpseDrops", 0);
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