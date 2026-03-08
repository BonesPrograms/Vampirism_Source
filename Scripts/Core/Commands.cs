using XRL.World.Parts;
using XRL.Wish;
using XRL;
using Nexus.Registry;
using Nexus.Properties;
using XRL.World;
using Nexus.Core;
using XRL.World.Effects;
using XRL.World.Parts.Mutation;

namespace Nexus.Wish
{
    [HasWishCommand]
    public static class Commands
    {


        [WishCommand(Command = "vamplvl")]
        public static void PowerUp(string text)
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

        public static void bloodpack() => The.Player.Inventory.AddObject("BloodCanteen");

        [WishCommand(Command = "frenzy")]
        public static void Frenzy()
        {
            if (Security())
            {
                TheBeast beast = The.Player.GetPart<TheBeast>();
                beast.Core.Frenzy();
            }
        }

        [WishCommand(Command = "bloodlust")]
        public static void Bloodlust()
        {
            if (Security())
            {
                Vitae Vitae = The.Player.GetPart<Vitae>();
                Vitae.Blood = 1;
            }
        }


        [WishCommand(Command = "wassail")]
        public static void GameOverWish()
        {
            if (Security())
            {
                The.Player.GetPart<Humanity>().SetZero();
                The.Player.SetStringProperty(Flags.GO, Flags.TRUE);
                The.Player.PassTurn();
            }
        }

        [WishCommand(Command = "humanity")]
        public static void Gameover()
        {
            if (Security())
            {
                The.Player.FireEvent(Event.New(Events.WISH_HUMANITY));
                The.Player.SetIntProperty(Flags.HUMANITY, Rules.Humanity.MAX);
                The.Player.SetStringProperty(Flags.GO, Flags.FALSE);
                IComponent<GameObject>.AddPlayerMessage("{{G sequence|Humanity reset to maximum.}}");
            }
        }

        [WishCommand(Command = "vitae")]
        public static void Blood()
        {
            if (Security())
            {
                Vitae Vitae = The.Player.GetPart<Vitae>();
                Vitae.Blood = Nexus.Rules.Vitae.BLOOD_GLUTTONOUS;
                The.Player.SetIntProperty(Flags.BLOOD_VALUE, Rules.Vitae.BLOOD_GLUTTONOUS);
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
                IComponent<GameObject>.AddPlayerMessage("Not a vampire!");
                return false;
            }

        }
    }
}