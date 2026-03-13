using XRL.World;
using XRL.UI;
using XRL.World.Parts;
using VampirismSys.Properties;
using VampirismSys.Core;
using XRL.World.Effects;

namespace VampirismSys.Frenzy
{
    public class FrenzyCore
    {
        readonly TheBeast Source;
        bool _midFrenzyChance => WikiRng.Next(1, 2000) == 2000;
        bool _highFrenzyChance => WikiRng.Next(1, 1000) == 1000;
        bool _critFrenzyChance => WikiRng.Next(1, 500) == 500;
        internal readonly Search Search;
        internal FrenzyCore(TheBeast Source)
        {
            this.Source = Source;
            this.Search = new(Source);
        }

        internal void FrenzyChances()
        {
            if (!Source.GameOver)
            {
                FrenzyThirstChance();
                FrenzyHumanityChance();
            }
            else
                Frenzy();

        }
        void FrenzyHumanityChance()
        {
            if (Source.ParentObject.IsInCombat() || Source.ParentObject.GetStringProperty(Flags.BLOOD_STATUS) is Flags.Blood.MIN or Flags.Blood.THIRSTY or Flags.Blood.PARCHED)
            {
                switch (Source.ParentObject.GetIntProperty(Flags.HUMANITY))
                {
                    case VampirismSys.Rules.Humanity.MID:
                        {
                            if (_midFrenzyChance)
                                Frenzy();
                            break;
                        }
                    case VampirismSys.Rules.Humanity.LOW:
                        {
                            if (_highFrenzyChance)
                                Frenzy();
                            break;
                        }
                    case VampirismSys.Rules.Humanity.CRIT:
                        {
                            if (_critFrenzyChance)
                                Frenzy();
                            break;
                        }
                }
            }
        }

        void FrenzyThirstChance()
        {
            switch (Source.ParentObject.GetStringProperty(Flags.BLOOD_STATUS))
            {
                case Flags.Blood.THIRSTY:
                    {
                        if (_midFrenzyChance)
                            Frenzy();
                        break;
                    }
                case Flags.Blood.PARCHED:
                    {
                        if (_highFrenzyChance)
                            Frenzy();
                        break;
                    }

                case Flags.Blood.MIN:
                    {
                        if (_critFrenzyChance)
                            Frenzy();
                        break;
                    }
            }
        }
        public void Frenzy(GameObject forcedTarget = null)
        {
            if (!Source.CantFrenzy()) //frenzythirstychance and frenzyhumanitychance run right after another, so there is a slim but rare chance that rng can attempt to apply frenzyAI twice, which we do not want
            {
                if (forcedTarget != null)
                    Apply(forcedTarget);
                else if (Search.TryScan(out GameObject Target))
                    Apply(Target);
                else if (!Source.GameOver && Source.ParentObject.IsPlayer())
                    IComponent<GameObject>.AddPlayerMessage("You feel a surge of adrenaline as {{R sequence|the Beast}} momentarily tries to take control.");
            }
        }
        void Apply(GameObject Target)
        {
            if (Source.ParentObject.IsPlayer())
            {
                if (!Source.GameOver)
                    Popup.Show("{{R sequence|You frenzy!}}"); //specific order of operations - want the wassai thing to skip if youre not in gameover yet
                else if (!Source.Wassail)
                {
                    Popup.Show("{{R sequence|Wassail!}}");
                    Source.Wassail = true;
                }
            }
            else
                IComponent<GameObject>.AddPlayerMessage($"{Source.ParentObject.t()} frenzies!");
            AssembleAI(Target);
        }

        void AssembleAI(GameObject Target)
        {
            Source.ParentObject.ApplyEffect(new FrenzyAI(Target, Source.GameOver));
            Source.ParentObject.ApplyEffect(new Running(WikiRng.Next(10, 20)));
        }


    }
}