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
        bool _midFrenzyChance => WikiRng.Next(1, 2000) is 2000;
        bool _highFrenzyChance => WikiRng.Next(1, 1000) is 1000;
        bool _critFrenzyChance => WikiRng.Next(1, 500) is 500;

        public FrenzyCore(TheBeast Source) => this.Source = Source;

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
            if (Source.ParentObject.IsInCombat() || Source.ParentObject.GetStringProperty(Flags.BLOOD_STATUS) is Flags.Blood.MIN or Flags.Blood.THIRSTY or Flags.Blood.PARCHED)
            {
                switch (Source.ParentObject.GetIntProperty(Flags.HUMANITY))
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

        public void Frenzy()
        {
            if (new Search(Source).TryScan(out GameObject Target))
                Apply(Target);
            else if (!Source.GameOver)
                IComponent<GameObject>.AddPlayerMessage("You feel a surge of adrenaline as {{R sequence|the Beast}} momentarily tries to take control.");
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
            Source.ParentObject.SetStringProperty(Flags.FRENZY, Flags.TRUE);
            CommandEvent.Send(Source.ParentObject, "CommandToggleRunning");
        }


    }
}