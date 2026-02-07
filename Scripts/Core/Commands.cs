using XRL.World.Parts;
using XRL.Wish;
using XRL;
using Nexus.Registry;
using Nexus.Properties;
using XRL.World;
using Nexus.Core;
using Nexus.Rules;
using XRL.World.Effects;

namespace Nexus.Wish
{
    [HasWishCommand]
    public static class Commands
    {

        [WishCommand(Command = "bloodpack")]

        public static void bloodpack() => The.Player.Inventory.AddObject("BloodCanteen");

        [WishCommand(Command = "frenzy")]
        public static void Frenzy()
        {
            if (Security(false))
            {
                TheBeast beast = The.Player.GetPart<TheBeast>();
                beast.Core.Frenzy();
            }
        }

        [WishCommand(Command = "bloodlust")]
        public static void Bloodlust()
        {
            if (Security(false))
            {
                Vitae Vitae = The.Player.GetPart<Vitae>();
                Vitae.Blood = 1;
            }
        }


        [WishCommand(Command = "wassail")]
        public static void GameOverWish()
        {
            if (Security(false))
            {
                The.Player.GetPart<Humanity>().Score = 0;
                The.Player.SetStringProperty(FLAGS.GO, FLAGS.TRUE);
                The.Player.PassTurn();
            }
        }

        [WishCommand(Command = "humanity")]
        public static void Gameover()
        {
            if (Security(false))
            {
                The.Player.FireEvent(Event.New(Events.WISH_HUMANITY));
                The.Player.SetIntProperty(FLAGS.HUMANITY, Rules.HUMANITY.MAX);
                The.Player.SetStringProperty(FLAGS.GO, FLAGS.FALSE);
                IComponent<GameObject>.AddPlayerMessage("{{G sequence|Humanity reset to maximum.}}");
            }
        }

        [WishCommand(Command = "vitae")]
        public static void Blood()
        {
            if (Security(false))
            {
                Vitae Vitae = The.Player.GetPart<Vitae>();
                Vitae.Blood = VITAE.BLOOD_GLUTTONOUS;
                The.Player.SetIntProperty(FLAGS.BLOOD_VALUE, VITAE.BLOOD_GLUTTONOUS);
                The.Player.SetStringProperty(FLAGS.BLOOD_STATUS, FLAGS.BLOOD.GLUT);
                IComponent<GameObject>.AddPlayerMessage("{{G sequence|Thirst removed.}}");
            }
        }

        static bool Security(bool dominationblock)
        {
            if (The.Player.IsVampire())
            {
                if (dominationblock && The.Player.HasEffect<Dominated>())
                {
                    IComponent<GameObject>.AddPlayerMessage("Cannot use on dominated targets!");
                    return false;
                }
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