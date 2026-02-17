using XRL.World.Effects;
using System;
using Nexus.Rules;
using Nexus.Core;
using XRL.World.Parts.Mutation;
using XRL.Core;

namespace XRL.World.Parts
{
    //this is mostly just metamorphosis. you could easily use this to make your own ad-hoc metamorphosis for any creature blueprint if you like
    public class BatformSpell : VampiricSpell
    {
        public override Type SpellType => typeof(BatformSpell);
        public override int Cooldown => 0; //100 - (Level * 5); //so at level 20 you can just become a bat whenever you want
        public GameObject OriginalBody;
        public GameObject Bat;
        public override void CollectStats(Templates.StatCollector stats)
        {
            stats.CollectCooldownTurns(MyActivatedAbility(SpellID), Cooldown);
        }

        public override void AddSpell()
        {
            SpellID = AddMyActivatedAbility("Batform", "invokeBatform", $"{CLASS}", null, "\u009f");

        }

        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == "invokeBatform" && Checks.Prerequisites(ParentObject, "transform", "tansform"))
            {
                if (!ParentObject.IsRealityDistortionUsable())
                    RealityStabilized.ShowGenericInterdictMessage(ParentObject);
                else
                    Cast();
            }
            return base.HandleEvent(E);
        }
        void Revert()
        {
            Cell cell = Bat.Physics.CurrentCell;
            cell.RemoveObject(Bat);
            cell.AddObject(OriginalBody);
            XRLCore.Core.Game.Player.Body = OriginalBody;
            Metamorphosis.TransferInventory(Bat, OriginalBody, false);
            Bat.MakeInactive();
            OriginalBody.MakeActive();
        }

        void Cast()
        {
            if (Bat?.IsPlayer() ?? false)
            {
                Revert();
                AddPlayerMessage("You revert back to your true form");

            }
            else if (base.Cast("transform"))
            {
                ExpendBlood();
                if (RealityCheck(ParentObject.CurrentCell))
                {
                    Transform();
                }
            }
        }

        void SyncMutations()
        {
            Mutations m = ParentObject.GetPart<Mutations>();
            for (int i = 0; i < m.MutationList.Count; i++)
            {
                Bat.SameMutation(m.MutationList[i]);
            }
            Bat.SamePart(this);
        }
        void Transform()
        {
            OriginalBody = ParentObject;
            Cell cell2 = ParentObject.Physics.CurrentCell;
            Bat ??= GameObject.Create("Bat");
            Metamorphosis.TransferInventory(ParentObject, Bat);
            Metamorphosis.TransferMental(ParentObject, Bat);
            if (Bat.Statistics != null)
                Bat.Statistics["Level"] = ParentObject.GetStat("Level");
            SyncMutations();
            XRLCore.Core.Game.ActionManager.RemoveActiveObject(ParentObject);
            XRLCore.Core.Game.ActionManager.AddActiveObject(Bat);
            cell2.RemoveObject(ParentObject);
            cell2.AddObject(Bat);
            ParentObject.MakeInactive();
            Bat.MakeActive();
            XRLCore.Core.Game.Player.Body = Bat;
            AddPlayerMessage("You assume the form of a bat");

        }
    }
}