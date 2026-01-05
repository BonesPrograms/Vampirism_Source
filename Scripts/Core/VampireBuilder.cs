using XRL.World.Parts;
using XRL.World.Effects;
using XRL.World;
using Nexus.Properties;
using Nexus.Rules;

namespace Nexus.Core
{
    /// <summary>
    /// Assigns properties, adds parts and effects to Vampires.
    /// </summary>

    static class VampireBuilder
    {

        public static void Make(GameObject GO)
        {
            SetGameProperties(GO);
            SetVampireProperties(GO);
            AddParts(GO);
        }

        public static void Unmake(GameObject GO)
        {
            RemoveGameProperties(GO);
            RemoveVampireProperties(GO);
            RemoveParts(GO);
        }


        static void RemoveGameProperties(GameObject GO)
        {
            GO.SetStringProperty("WaterRitualLiquid", "water");
            if (GO.TryGetStringProperty("BleedLiquid", out string result))
                if (result == OPTIONS.BLEEDLIQUID)
                    GO.SetStringProperty("BleedLiquid", "blood-1000");
        }

        static void SetGameProperties(GameObject GO)
        {
            GO.SetStringProperty("WaterRitualLiquid", "blood");
            if (XRL.UI.Options.GetOptionBool(OPTIONS.BLOOD_NERF) && GO.IsOriginalPlayerBody())
            {
                GO.SetStringProperty("BleedLiquid", OPTIONS.BLEEDLIQUID);
                GO.SetStringProperty("BleedColor", "&r");
            }
        }

        static void AddParts(GameObject GO)
        {
            GO.AddPart<Vitae>();
            GO.AddPart<TheBeast>();
            GO.AddPart<Nightbeast>();
            GO.AddPart<Humanity>();
            GO.ApplyEffect(new HumanityUI(9999));
        }

        static void SetVampireProperties(GameObject GO)
        {

            GO.SetStringProperty(Flags.GO, Flags.FALSE);
            GO.SetStringProperty(Flags.FEED, Flags.FALSE);
            GO.SetStringProperty(Flags.FRENZY, Flags.FALSE);
            GO.SetStringProperty(Flags.BLOOD_STATUS, Flags.Blood.GLUT);
            GO.SetStringProperty(Flags.STEALTH, Flags.FALSE);
            GO.SetIntProperty(Flags.BLOOD_VALUE, VITAE.BLOOD_GLUTTONOUS);
            GO.SetIntProperty(Flags.HUMANITY, HUMANITY.MAX);
            GO.SetIntProperty(Flags.REGEN, 0);

        }

        static void RemoveParts(GameObject GO)
        {
            GO.RemovePart<Vitae>();
            GO.RemovePart<Humanity>();
            GO.RemovePart<TheBeast>();
            GO.RemoveEffect<HumanityUI>();
            GO.RemovePart<Nightbeast>();
            if (GO.HasEffect<Bloodlust>())
                GO.RemoveEffect<Bloodlust>();
        }

        static void RemoveVampireProperties(GameObject GO)
        {
            GO.RemoveStringProperty(Flags.GO);
            GO.RemoveStringProperty(Flags.FEED);
            GO.RemoveStringProperty(Flags.FRENZY);
            GO.RemoveStringProperty(Flags.BLOOD_STATUS);
            GO.RemoveStringProperty(Flags.STEALTH);
            GO.RemoveIntProperty(Flags.BLOOD_VALUE);
            GO.RemoveIntProperty(Flags.HUMANITY);
            GO.RemoveIntProperty(Flags.REGEN);
        }
    }
}