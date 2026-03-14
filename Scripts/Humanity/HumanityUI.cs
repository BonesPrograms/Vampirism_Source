using System;
using XRL.World.Parts.Mutation;
using VampirismSys.Properties;
using VampirismSys.Rules;
using XRL.World.Parts;

namespace XRL.World.Effects
{
    /// <summary>
    /// Simple interface to allow the player to view Humanity score and regen time.
    /// </summary>
    [Serializable]
    internal class HumanityUI : Effect
    {
        public bool gameover = false; //doesnt need to exist anymore but i left it in because well shit... its already serialized and the effect is permament. whoops. might find a use for it later!
        public HumanityUI()
        {
            DisplayName = "";
            Duration = 9999;
        }
        public override string GetDescription() => "";
        string Regen(int humanity) => humanity != VampirismSys.Rules.Humanity.MAX ? "\nRegeneration: {{B sequence|" + base.Object.GetIntProperty(Flags.REGEN) + "}}/5000" : "\nRegeneration: {{G|Max}}";
        public override string GetDetails()
        {
            int humanity = base.Object.GetIntProperty(Flags.HUMANITY);
            return humanity switch
            {
                VampirismSys.Rules.Humanity.MAX => "{{G sequence|5}}{{Y sequence|/5}} {{G sequence|Humanity}}" + Regen(humanity),
                VampirismSys.Rules.Humanity.HIGH => "{{G sequence|4}}{{Y sequence|/5}} {{G sequence|Humanity}}" + Regen(humanity),
                VampirismSys.Rules.Humanity.MID => "{{W sequence|3}}{{Y sequence|/5}} {{G sequence|Humanity}}" + Regen(humanity),
                VampirismSys.Rules.Humanity.LOW => "{{W sequence|2}}{{Y sequence|/5}} {{G sequence|Humanity}}" + Regen(humanity),
                VampirismSys.Rules.Humanity.CRIT => "{{R sequence|1}}{{Y sequence|/5}} {{G sequence|Humanity}}" + Regen(humanity),
                VampirismSys.Rules.Humanity.GAMEOVER => "{{R sequence|Wight}}\nYou have given in to your inner animal, and have become wild.\nYou will never feel full again.",
                _ => "Loading! Please pass a turn.",
            };
        }

        public override void Remove(GameObject Object)
        {
            if (Object.HasPart<Parts.Humanity>())
            {
                Object.ApplyEffect(new HumanityUI());
                MetricsManager.LogModError(ModManager.GetMod("vampirism"), "game attempted to remove humanity UI effect");
            }
        }
    }
}
