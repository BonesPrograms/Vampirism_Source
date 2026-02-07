using System;
using XRL.World.Parts.Mutation;
using XRL.World.Effects;
using Nexus.Properties;
using Nexus.Death;
using Nexus.Core;
using Nexus.Rules;
using XRL.UI;
using Nexus.Registry;

namespace XRL.World.Parts
{
    /// <summary>
    /// The external part held by all edible targets in the world. Watches for the object's conditions on death - deducts humanity if the player performs an action that violates the rules of humanity.
    /// </summary>
    [Serializable]
    public class DeathHandler : IPart //it is necessary for this part to exist on all gameobjects ever since the Embrace spell was added
    {                                   //incase the player ever becomes a vampire, corpses they encountered in the past are already marked
        [NonSerialized]                    //as embraceable or not-embraceable. 
        public static GameObject Player;           //it is not as simple as checking corpse blueprints, only specific types of creatures can become vampires
        [NonSerialized]                     //and only those kinds of creatures will have the DeathHandler part - feedable creatures can become vampires
        static GameObject TrueDominator;
        public bool finished;
        public override bool WantEvent(int ID, int cascade)
        {
            if (!finished && ID == SingletonEvent<BeforeTakeActionEvent>.ID)
                return true;
            if (Options.GetOptionBool(OPTIONS.FRACTUS_NERF) && ID == TookDamageEvent.ID)
                return true;
            if (ID == DeathEvent.ID)
                return true;
            if (ID == ZoneActivatedEvent.ID)
                return true;
            return base.WantEvent(ID, cascade);
        }

        public override bool HandleEvent(ZoneActivatedEvent E) //incase the player isnt a vampire at this moment, but there are vampires in his save
        {                                                       //the player will always have DeathHandler so he will always be able to send the event to vampires in his zone
            TryUpdate(E.Zone);
            return base.HandleEvent(E);
        }

        static void TryUpdate(Zone zone) //so that we can avoid firing events on every object in a  zone that does not need updating
        {
            if (zone.TryGetZoneProperty(FLAGS.MOD.VERSION, out var result))
            {
                if (result != MOD.VERSION)
                    UpdateZone(zone);
            }
            else
                UpdateZone(zone);
        }

        static void UpdateZone(Zone zone)
        {
            zone.FireEvent(Events.UPDATE);
            zone.SetZoneProperty(FLAGS.MOD.VERSION, MOD.VERSION);
        }
        public override bool HandleEvent(TookDamageEvent E)
        {
            if (E.Object == ParentObject && (ParentObject.CurrentCell?.HasObjectWithPart(nameof(Fracti)) ?? false))
                Saltify.Salt(ParentObject.CurrentCell);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeTakeActionEvent E)
        {
            if (Security())
                finished = Init.Evaluate(ParentObject, Player); //AI are not assigned Innocent flags until the player has become a vampire for the first time
            return base.HandleEvent(E);                         //as per Security()
        }
        public override bool HandleEvent(DeathEvent E)
        {
            bool isvampire = E.Dying.IsVampire();
            MarkForEmbrace(E.Dying, isvampire); //we always have this run even if the player isnt a vampire, incase they become one later on
            if (!isvampire)
                CreateDeathsInstance(E.Killer, E.Dying); //but this will check Security() and wont create an instance if the player isnt a vampire
            return base.HandleEvent(E);
        }
        static void CreateDeathsInstance(GameObject Killer, GameObject Dying)
        {
            if (Security() && !Player.CheckFlag(FLAGS.GO) && Options.GetOptionBool(Nexus.Rules.OPTIONS.HUMANITY) && !Dying.HasStringProperty(FLAGS.DEAD))
            {
                bool friendly = Dying.IsFriendly(The.Player);
                if (Options.GetOptionBool(Nexus.Rules.OPTIONS.DOUG) && friendly && !Dying.IsGhoulOf(The.Player) && !Dying.IsBeguiledBy(The.Player))
                    return;
                else
                    new Deaths(Player, Dying, Killer, friendly, Dying.IsHostileTowards(The.Player)).Possibilities();
            }
        }

        static void MarkForEmbrace(GameObject Dying, bool isvampire) //only "feedable" targets can become vampires, but deathhandler only exists as a part on feedable objects, so the check is already done
        {                                   //corpse objects whose source object didnt have this part wont have the property at all and thus will not be embraceable
            if (Dying.CurrentCell != null)
            {
                for (int i = 0; i < Dying.CurrentCell.Objects.Count; i++)
                {
                    GameObject obj = Dying.CurrentCell.Objects[i];
                    if (obj.PropertyEquals("SourceID", Dying.ID))
                    {
                        if (isvampire)
                        {
                            AddPlayerMessage($"{Dying.t()} burns to ashes!");
                            obj.SetStringProperty(FLAGS.EMBRACE.EMBRACEABLE, FLAGS.FALSE);
                        }
                        else if (Dying.TryGetPart(out Corpse corpse))
                            CompareBlueprints(Dying.Level, obj, corpse);
                        return;
                    }
                }
            }
        }

        static void CompareBlueprints(int level, GameObject obj, Corpse corpse)
        {
            if (obj.Blueprint == corpse.CorpseBlueprint)
            {
                obj.SetIntProperty(FLAGS.EMBRACE.LEVEL_ON_DEATH, level);
                obj.SetStringProperty(FLAGS.EMBRACE.EMBRACEABLE, FLAGS.TRUE);
            }
            else if (obj.Blueprint == corpse.BurntCorpseBlueprint || obj.Blueprint == corpse.VaporizedCorpseBlueprint)
                obj.SetStringProperty(FLAGS.EMBRACE.EMBRACEABLE, FLAGS.FALSE);
        }

        /// <summary>
        /// Ensures that the Player field is assigned to the player's source, original GameObject and that the player is a vampire before beginning.
        /// </summary>
        /// <returns></returns>
        static bool Security() => Player == null || Player.HasEffect<Dominated>() ? FindPlayerObject() : Player.HasPart<Vampirism>();
        static bool FindPlayerObject()
        {
            if (!The.Player.HasEffect<Dominated>())
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
        static bool FindMaster()
        {
            if (The.Player.TryGetEffect<Dominated>(out Dominated e))
            {
                if (!e.Dominator.HasEffect<Dominated>())
                {
                    Player = e.Dominator;
                    return Player.HasPart<Vampirism>();
                }
                else
                    return LoopDominator(e);
            }
            else
                return false;
        }

        static bool LoopDominator(Dominated e)
        {
            TrueDominator = e.Dominator;
            while (TrueDominator.HasEffect<Dominated>())
            {
                Dominated d = TrueDominator.GetEffect<Dominated>();
                TrueDominator = d.Dominator;
            }
            Player = TrueDominator;
            TrueDominator = null;
            return Player.HasPart<Vampirism>();
        }

        // bool LastResort()
        // {
        //     GameObject Object = GameObject.Find(x=>x.IsOriginalPlayerBody());
        //     Object.IsSelfControlledPlayer
        // }

    }

}