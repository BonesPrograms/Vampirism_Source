using XRL.World.Parts;
using XRL.Wish;
using XRL;
using Nexus.Registry;
using Nexus.Properties;
using XRL.World;
using XRL.World.Parts.Mutation;
using Nexus.Rules;

namespace Nexus.Core
{
    [HasWishCommand]
    public static class Commands
    {

        [WishCommand(Command = "bloodpack")]

        public static void bloodpack() => The.Player.Inventory.AddObject("BloodCanteen");

        [WishCommand(Command = "frenzy")]
        public static void Frenzy()
        {
            if (Security() == false)
                return;
            TheBeast beast = The.Player.GetPart<TheBeast>();
            Frenzy.FrenzyCore frenzy = new(beast);
            frenzy.Frenzy();
        }

        [WishCommand(Command = "bloodlust")]
        public static void Bloodlust()
        {
            if (Security() == false)
                return;
            Vitae Vitae = The.Player.GetPart<Vitae>();
            Vitae.Blood = 1;
        }


        [WishCommand(Command = "wassail")]
        public static void GameOverWish()
        {
            if (Security() == false)
                return;
            The.Player.GetPart<Humanity>().Score = 0;
            The.Player.SetStringProperty(Flags.GO, Flags.TRUE);
            The.Player.PassTurn();
        }

        [WishCommand(Command = "humanity")]
        public static void Gameover()
        {
            if (Security() == false)
                return;
            The.Player.FireEvent(Event.New(Events.WISH_HUMANITY));
            The.Player.SetIntProperty(Flags.HUMANITY, Rules.HUMANITY.MAX);
            The.Player.SetStringProperty(Flags.GO, Flags.FALSE);
            IComponent<GameObject>.AddPlayerMessage("{{G sequence|Humanity reset to maximum.}}");
        }

        [WishCommand(Command = "vitae")]
        public static void Blood()
        {
            if (Security() == false)
                return;
            Vitae Vitae = The.Player.GetPart<Vitae>();
            Vitae.Blood = VITAE.BLOOD_GLUTTONOUS;
            The.Player.SetIntProperty(Flags.BLOOD_VALUE, VITAE.BLOOD_GLUTTONOUS);
            The.Player.SetStringProperty(Flags.BLOOD_STATUS, Flags.Blood.GLUT);
            IComponent<GameObject>.AddPlayerMessage("{{G sequence|Thirst removed.}}");
        }

        static bool Security()
        {
            if (!The.Player.HasPart<Vampirism>() || !The.Player.IsOriginalPlayerBody())
            {
                if (The.Player.IsOriginalPlayerBody())
                    IComponent<GameObject>.AddPlayerMessage("Not a vampire!");
                else
                    IComponent<GameObject>.AddPlayerMessage("Cannot use on dominated targets!");
                return false;
            }
            else
                return true;

        }
    }
}