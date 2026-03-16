using XRL.World.Parts;
using XRL.Wish;
using XRL;
using VampirismSys.Registry;
using VampirismSys.Properties;
using XRL.World;
using VampirismSys.Extensions;
using XRL.World.Effects;
using XRL.World.Parts.Mutation;

namespace VampirismSys.Wish
{
    [HasWishCommand]
    internal static class Commands
    {

        [WishCommand(Command = "vamplvl")]
        internal static void PowerUp(string text)
        {
            if (int.TryParse(text, out int result))
            {
                Vampirism v = The.Player.GetPart<Vampirism>();
                v.BaseLevel = result;
                v.CapOverride = result;
                IComponent<GameObject>.AddPlayerMessage($"Vampirism level set to {result}.");
            }
            else
                XRL.UI.Popup.Show($"{text} is not valid integer!");
        }

        [WishCommand(Command = "bloodpack")]
        internal static void Bloodpack() => The.Player.Inventory.AddObject("BloodCanteen");

        [WishCommand(Command = "frenzy")]
        internal static void Frenzy()
        {
            if (Security())
            {
                TheBeast beast = The.Player.GetPart<TheBeast>();
                beast.Core.Frenzy();
            }
        }

        [WishCommand(Command = "bloodlust")]
        internal static void Bloodlust()
        {
            if (Security())
            {
                VampireBloodMetabolism Vitae = The.Player.GetPart<VampireBloodMetabolism>();
                Vitae.Blood = 1;
            }
        }


        [WishCommand(Command = "wassail")]
        internal static void GameOverWish()
        {
            if (Security())
            {
                The.Player.GetPart<Humanity>().Score = 0;
                The.Player.SetStringProperty(Flags.GO, Flags.TRUE);
                The.Player.PassTurn();
            }
        }

        [WishCommand(Command = "humanity")]
        internal static void ReverseGameOver()
        {
            if (Security())
            {
                The.Player.FireEvent(Event.New(Events.WISH_HUMANITY));
                The.Player.SetIntProperty(Flags.HUMANITY, Rules.Hum.MAX);
                The.Player.SetStringProperty(Flags.GO, Flags.FALSE);
                IComponent<GameObject>.AddPlayerMessage("{{G sequence|Humanity reset to maximum.}}");
            }
        }

        [WishCommand(Command = "vitae")]
        internal static void Blood()
        {
            if (Security())
            {
                VampireBloodMetabolism Vitae = The.Player.GetPart<VampireBloodMetabolism>();
                Vitae.Blood = VampirismSys.Rules.Metab.BLOOD_GLUTTONOUS;
                The.Player.SetIntProperty(Flags.BLOOD_VALUE, Rules.Metab.BLOOD_GLUTTONOUS);
                The.Player.SetStringProperty(Flags.BLOOD_STATUS, Flags.Blood.GLUT);
                IComponent<GameObject>.AddPlayerMessage("{{G sequence|Thirst removed.}}");
            }
        }

        static bool Security()
        {
            if (The.Player.IsVampire())
            {
                return true;
            }
            else
            {
                XRL.UI.Popup.Show("Not a vampire!");
                return false;
            }

        }
    }
}