using System;
using XRL.World.Parts.Mutation;
using XRL.World.Effects;
using Nexus.Properties;
using Nexus.Death;
using Nexus.Core;
using XRL.UI;

namespace XRL.World.Parts
{
    /// <summary>
    /// The external part held by all edible targets in the world. Watches for the object's conditions on death - deducts humanity if the player performs an action that violates the rules of humanity.
    /// </summary>
    [Serializable]
    public class DeathHandler : IPart
    {
        [NonSerialized]
        GameObject Player;

        [NonSerialized]
        GameObject TrueDominator;
        public bool finished;
        public override bool WantEvent(int ID, int cascade)
        {
            if (!base.WantEvent(ID, cascade))
            {
                if (!finished)
                    return ID == SingletonEvent<BeforeTakeActionEvent>.ID;
                else if (ID != SingletonEvent<EndTurnEvent>.ID)
                    return ID == DeathEvent.ID; //i ccouldnt find a way to listen to a global form of death events, so i have to manually give every creature a part so i can hear their deaths
            }
            return true;
        }

        public override bool HandleEvent(EndTurnEvent E)
        {
            if (Options.GetOptionBool(Nexus.Rules.OPTIONS.FRACTUS_NERF) && (ParentObject.CurrentCell?.HasObjectWithPart(nameof(Fracti)) is true))
                Saltify.Salt(ParentObject);
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(BeforeTakeActionEvent E)
        {
            if (Security())
                finished = Init.Evaluate(ParentObject, Player);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(DeathEvent E)
        {
            if (Security() && !Scan.SafeReturnProperty(Player, Flags.GO) && Options.GetOptionBool(Nexus.Rules.OPTIONS.HUMANITY) && E?.Dying?.HasStringProperty(Flags.DEAD) is false)
                new Deaths(Player, E.Dying, E.Killer).Run();
            return base.HandleEvent(E);
        }

        /// <summary>
        /// Ensures that the Player field is assigned to the player's source, original GameObject and that the player is a vampire before beginning.
        /// </summary>
        /// <returns></returns>
        bool Security() => Player is null || !Player.IsOriginalPlayerBody() ? FindPlayerObject() : Player.HasPart<Vampirism>();
        bool FindPlayerObject()
        {
            if (The.Player.IsOriginalPlayerBody())
            {
                Player = The.Player;
                return Player.HasPart<Vampirism>();
            }
            else
                return FindMaster();
        }

        /// <summary>
        /// Loops through the domination effect's dominator to find the player's actual GameObject and assign it to the Player field.
        /// </summary>
        /// <returns></returns>
        bool FindMaster()
        {
            if (The.Player.TryGetEffect<Dominated>(out Dominated e))
            {
                if (e.Dominator.IsOriginalPlayerBody() && e.Dominator.HasPart<Vampirism>())
                {
                    Player = e.Dominator;
                    return true;
                }
                else
                    return LoopDominator(e);
            }
            else
                return false;
        }

        bool LoopDominator(Dominated e)
        {
            TrueDominator = e.Dominator;
            while (!TrueDominator.IsOriginalPlayerBody())
            {
                Dominated d = TrueDominator.GetEffect<Dominated>();
                TrueDominator = d.Dominator;
            }
            if (TrueDominator.IsOriginalPlayerBody() && TrueDominator.HasPart<Vampirism>())
            {
                Player = TrueDominator;
                TrueDominator = null;
                return true;
            }
            else
                return false;
        }

    }

}