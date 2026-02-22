using Nexus.Core;
using Nexus.Rules;
using XRL.Rules;
using Qud.API;
using XRL.Language;
using XRL.UI;

namespace XRL.World.Parts
{
    public class MoteOfHumanity : IScribedPart
    {

        public const string COMMAND_NAME = "moteHumanity";

        public static bool MoteAutoMemory = false;

        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == GetInventoryActionsEvent.ID || ID == InventoryActionEvent.ID || ID == SingletonEvent<EndTurnEvent>.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }

            public override bool HandleEvent(EndTurnEvent E)
        {
            if (ParentObject?.CurrentCell != null && ParentObject.InInventory == null && ParentObject.Holder == null && WikiRng.Next(1, 2) == 2)
                ParentObject.Move(Directions.GetRandomDirection(), true, false, true, false, false, true, null, null, true, null, null, null, true, true, null, null);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetInventoryActionsEvent E)
        {
            if (E.Actor.IsVampire())
                E.AddAction("AbsorbHumanity", "absorb", COMMAND_NAME, null, 'a', FireOnActor: false, 0, 0, Override: false, WorksAtDistance: false, WorksTelekinetically: false);
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(InventoryActionEvent E)
        {
            if (E.Command == COMMAND_NAME)
            {
                Humanity h = E.Actor.GetPart<Humanity>();
                if (CheckHumanity(h))
                {
                    UI.Popup.Show("A chaotic storm of fractured human experience cascades over your mind like a crashing wave. For a moment, you feel alive - then it fades.");
                    h.AddHumanity();
                    AddPlayerMessage($"You absorb {ParentObject.t()}");
                    Secrets();
                    ParentObject.Obliterate();
                }
            }
            return base.HandleEvent(E);
        }

        void Secrets() //from mumble mouth
        {
            if (MoteAutoMemory || WikiRng.Next(1, 5000) <= 5)
            {
                IBaseJournalEntry randomUnrevealedNote = JournalAPI.GetRandomUnrevealedNote();
                JournalMapNote obj = randomUnrevealedNote as JournalMapNote;
                string text = "";
                text = ((obj == null) ? randomUnrevealedNote.Text : ("The location of " + Grammar.InitLowerIfArticle(randomUnrevealedNote.Text)));
                Popup.Show($"{ParentObject.t()} whispers to you one of it's secrets:\n\n" + text);
                randomUnrevealedNote.Reveal(ParentObject.DisplayName);
            }
        }

        bool CheckHumanity(Humanity h)
        {
            if (h.GameOver)
            {
                UI.Popup.Show("The feeling of human experience repulses you. You will never know what it is like to be human again.");
                AddPlayerMessage($"{ParentObject.t()} dissipates into mist.");
                ParentObject.TakeDamage(WikiRng.Next(5, 10), ParentObject, null);
                ParentObject.Obliterate();
                return false;
            }
            if (h.Score >= HUMANITY.MAX)
            {
                UI.Popup.Show("You are flush with false life, and cannot absorb any more humanity.");
                return false;
            }
            return true;
        }

    }
}