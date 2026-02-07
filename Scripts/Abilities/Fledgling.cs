using XRL.World.Parts;
using Nexus.Rules;
using Nexus.Core;
using Nexus.Frenzy;
using XRL.World;
using XRL.World.Parts.Mutation;
using System;

namespace XRL.World.Parts
{
    [Serializable]
    public class Fledgling : IPart
    {
        public GameObject Sire;
        public bool HatesSire;
        public long TimeOfSiring = The.Game.Turns;

        public bool IsChildeOf(GameObject Target)
        {
            return Target == Sire;
        }

        public Fledgling()
        {

        }

        public Fledgling(GameObject Sire, bool HatesSire) : this()
        {
            this.Sire = Sire;
            this.HatesSire = HatesSire;
        }
    }

    public class EmbraceSpell : IPart // VampiricPart
    {
        //Budding
        //one important thing: make sure the corpse is not blueprint ashes, lol. and organic and other stuff too i suppose
        ///could maybe run scan applicable on the ID
        public string ExpendBloodText() => $"You invoke blood magic.";
        //this will have listener for companion limit
        //i can draw from beguiling to see how to add new chat options like "follow"

        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == PooledEvent<CommandEvent>.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }

        public override bool HandleEvent(CommandEvent E)
        {
            if(E.Command == Nexus.Rules.EMBRACE.COMMAND_NAME)
            {
                if(ParentObject.TryGetTarget("embrace", "embracefail", out var pick))
                {
                    //nah it cant be tryget target, needs to be able to get acorpse object
                    //ill make TryGetCorpse next heh
                    //thinking something like
                    //i can just make TryGetTarget take a string, or <T>
                    //and instead of checking if it has an object with part combat, we check if it has an object with part corpse
                    //idk something like that

                    //furthermore
                    //you embrace people at your vampirism level (maybe)
                    //doesnt rly make sense


                    //i have a canbeEmbraced method now for objects
                }
            }
            return base.HandleEvent(E);
        }
    }
}

namespace XRL.World.Effects
{
    [Serializable]
    public class Embracing : Effect
    {

    }

    [Serializable]
    public class Embraced : Effect
    {
        public Embraced()
        {
            Duration = 9999;
            DisplayName = "";
        }
        public override string GetDescription() => "{{r|embraced}}";
        public sealed override string GetDetails() => "A newly embraced flegling vampire that has yet to feed.";
        bool Roll => WikiRng.Next(1, 100) == 100; //ridiculously high frenzy chance
        TheBeast _Beast;
        public TheBeast Beast => _Beast ??= Object.GetPart<TheBeast>();
        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == EffectAppliedEvent.ID)
                return true;
            if (Roll && ID == SingletonEvent<BeginTakeActionEvent>.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }

        public override bool HandleEvent(EffectAppliedEvent E)
        {
            if (E.Effect is IFeeding feed && feed.isAttacker && feed.Object == Object)
                Duration = 0;
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(BeginTakeActionEvent E)
        {
            if (!Object.IsPlayer() || !UI.Options.GetOptionBool(OPTIONS.FRENZY)) //prevents clean from running twice if object is player
                Beast.Clean();                                                    //if object is player and frenzy is disabled, enables clean
            if (!Beast.frenzied && !Beast.Incap() && Beast.HasFangs())           //(because disabling frenzy disables the internal clean in TheBeast)
                Beast.Core.EmbraceFrenzy();
            return base.HandleEvent(E);
        }

        public override void Remove(GameObject Obj)
        {
            if (!Obj?.IsPlayer() ?? false)
            {
                Vitae v = Obj.GetPart<Vitae>();
                v.SetBlood(VITAE.BLOOD_QUENCHED);
            }
        }

        public override bool Apply(GameObject Obj)
        {
            Vitae v = Obj?.GetPart<Vitae>();
            v?.SetBlood(VITAE.BLOOD_MIN);
            return true;
        }

    }
}