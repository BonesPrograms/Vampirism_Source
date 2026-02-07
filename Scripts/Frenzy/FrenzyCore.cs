using XRL.World;
using XRL.UI;
using XRL.World.Parts;
using Nexus.Properties;
using Nexus.Core;
using XRL.World.Effects;

namespace Nexus.Frenzy
{
    public class FrenzyCore
    {
        public readonly TheBeast Source;
        bool _midFrenzyChance => WikiRng.Next(1, 2000) == 2000;
        bool _highFrenzyChance => WikiRng.Next(1, 1000) == 1000;
        bool _critFrenzyChance => WikiRng.Next(1, 500) == 500;
        public readonly Search Search;
        public FrenzyCore(TheBeast Source, Search Search)
        {
            this.Source = Source;
            this.Search = Search;
        }

        public void FrenzyChances()
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
            if (Source.ParentObject.IsInCombat() || Source.ParentObject.GetStringProperty(FLAGS.BLOOD_STATUS) is FLAGS.BLOOD.MIN or FLAGS.BLOOD.THIRSTY or FLAGS.BLOOD.PARCHED)
            {
                switch (Source.ParentObject.GetIntProperty(FLAGS.HUMANITY))
                {
                    case Nexus.Rules.HUMANITY.MID:
                        {
                            if (_midFrenzyChance)
                                Frenzy();
                            break;
                        }
                    case Nexus.Rules.HUMANITY.LOW:
                        {
                            if (_highFrenzyChance)
                                Frenzy();
                            break;
                        }
                    case Nexus.Rules.HUMANITY.CRIT:
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
            switch (Source.ParentObject.GetStringProperty(FLAGS.BLOOD_STATUS))
            {
                case FLAGS.BLOOD.THIRSTY:
                    {
                        if (_midFrenzyChance)
                            Frenzy();
                        break;
                    }
                case FLAGS.BLOOD.PARCHED:
                    {
                        if (_highFrenzyChance)
                            Frenzy();
                        break;
                    }

                case FLAGS.BLOOD.MIN:
                    {
                        if (_critFrenzyChance)
                            Frenzy();
                        break;
                    }
            }
        }

        public void Frenzy()
        {
            if (Search.TryScan(out GameObject Target))
                Apply(Target);
            else if (!Source.GameOver && Source.ParentObject.IsPlayer())
                IComponent<GameObject>.AddPlayerMessage("You feel a surge of adrenaline as {{R sequence|the Beast}} momentarily tries to take control.");
        }

        public void EmbraceFrenzy()
        {
            if (Search.TryScan(out GameObject Target))
            {
                Apply(Target);
            }
        }
        void Apply(GameObject Target)
        {
            if (!Source.GameOver)
                Popup.Show("{{R sequence|You frenzy!}}"); //specific order of operations - want the wassai thing to skip if youre not in gameover yet
            else if (!Source.Wassail)
            {
                Popup.Show("{{R sequence|Wassail!}}");
                Source.Wassail = true;
            }
            AssembleAI(Target);
        }

        void AssembleAI(GameObject Target)
        {
            Source.ParentObject.ApplyEffect(new FrenzyAI(9999, Source, Target, Source.GameOver));
            Source.frenzied = true;
            Source.ParentObject.SetStringProperty(FLAGS.FRENZY, FLAGS.TRUE);
            CommandEvent.Send(Source.ParentObject, "CommandToggleRunning");
        }


    }
}