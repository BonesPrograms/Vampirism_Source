using System;
using XRL.World.Parts.Mutation;
using Nexus.Properties;
using Nexus.Rules;
using XRL.World.Parts;

namespace XRL.World.Effects
{
    /// <summary>
    /// Simple interface to allow the player to view Humanity score and regen time.
    /// </summary>
    [Serializable]
    public class HumanityUI : Effect
    {
        public bool gameover = false; //doesnt need to exist anymore but i left it in because well shit... its already serialized and the effect is permament. whoops. might find a use for it later!
        public HumanityUI() => DisplayName = "";
        public HumanityUI(int Duration) : this() => base.Duration = Duration;
        public override string GetDescription() => "";
        string Regen(int humanity) => humanity != HUMANITY.MAX ? "\nRegeneration: {{B sequence|" + base.Object.GetIntProperty(FLAGS.REGEN) + "}}/5000" : "\nRegeneration: {{G|Max}}";
        public override string GetDetails()
        {
            int humanity = base.Object.GetIntProperty(FLAGS.HUMANITY);
            return humanity switch
            {
                HUMANITY.MAX => "{{G sequence|5}}{{Y sequence|/5}} {{G sequence|Humanity}}" + Regen(humanity),
                HUMANITY.HIGH => "{{G sequence|4}}{{Y sequence|/5}} {{G sequence|Humanity}}" + Regen(humanity),
                HUMANITY.MID => "{{W sequence|3}}{{Y sequence|/5}} {{G sequence|Humanity}}" + Regen(humanity),
                HUMANITY.LOW => "{{W sequence|2}}{{Y sequence|/5}} {{G sequence|Humanity}}" + Regen(humanity),
                HUMANITY.CRIT => "{{R sequence|1}}{{Y sequence|/5}} {{G sequence|Humanity}}" + Regen(humanity),
                HUMANITY.GAMEOVER => "{{R sequence|Wight}}\nYou have given in to your inner animal, and have become wild.\nYou will never feel full again.",
                _ => "Loading! Please pass a turn.",
            };
        }

        public override void Remove(GameObject Object)
        {
            if (Object.HasPart<Humanity>())
            {
                Object.ApplyEffect(new HumanityUI(9999));
                MetricsManager.LogModError(ModManager.GetMod("vampirism"), "game attempted to remove humanity UI effect");
            }
        }
    }
}
