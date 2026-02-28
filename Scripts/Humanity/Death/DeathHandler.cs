using System;
using XRL.World.Parts.Mutation;
using XRL.World.Effects;
using Nexus.Properties;
using Nexus.Death;
using Nexus.Core;
using Nexus.Rules;
using XRL.UI;
using Nexus.Stealth;
using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Parts
{
    /// <summary>
    /// The external part held by all edible targets in the world. Watches for the object's conditions on death - deducts humanity if the player performs an action that violates the rules of humanity.
    /// </summary>
    [Serializable]
    [HasGameBasedStaticCache]
    public class DeathHandler : IPart
    {
        public static bool FreeMote;
        public static GameObject Player => _Player?.Object; //this is used for two major purposes: accessing the players humanity and checking hostility
                                                            //if you try to access by the.player (static) then you will get whatever
        [GameBasedStaticCache(false)]                       //gameobject they are currently dominating
        static GameObjectReference _Player;

        //instead of the gameobject that is "really" them 
        public bool finished;                                   //meaning: we cant find the humanity part, and innocence becomes relative to whatever gameobject the player is currently dominating
        public override bool WantEvent(int ID, int cascade)     //so you could dominate a snapjaw, and load a zone with snapjaws, and then come back as the original player
        {                                                       //start feeding on them and then lose humanity because they have the innocent flag
            if (!finished && ID == SingletonEvent<BeforeTakeActionEvent>.ID) //(for various reasons, checking hostility on death doesnt work)
                return true;
            if (Options.GetOptionBool(OPTIONS.FRACTUS_NERF) && ID == TookDamageEvent.ID)
                return true;
            if (ID == DeathEvent.ID)
                return true;
            return base.WantEvent(ID, cascade);
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
        }                                                       //which can result in funky behavior where AI are innocent in one save and not the other despite relations being the same
        public override bool HandleEvent(DeathEvent E)
        {
            bool isvampire = E.Dying.IsVampire();
            if (E.Dying.CurrentCell != null)
            {
                MarkForEmbrace(E.Dying, isvampire); //we always have this run even if the player isnt a vampire, incase they become one later on
                if (!isvampire)
                    DropMote(E.Dying);
            }
            if (!isvampire)
                CreateDeathsInstance(E.Killer, E.Dying); //but this will check Security() and wont create an instance if the player isnt a vampire
            return base.HandleEvent(E);
        }
        static void DropMote(GameObject Dying)
        {
            if (Dying.CheckFlag(FLAGS.INNOCENT))
            {
                if (FreeMote || WikiRng.Next(1, 1000) <= 1)
                    Dying.CurrentCell.AddObject("MoteOfHumanity");
            }
        }
        static void CreateDeathsInstance(GameObject Killer, GameObject Dying)
        {
            if (Options.GetOptionBool(Nexus.Rules.OPTIONS.HUMANITY) && Security() && !Player.CheckFlag(FLAGS.GO) && !Dying.HasStringProperty(FLAGS.DEAD))
            {
                bool friendly = Dying.IsFriendly(The.Player);
                if (Options.GetOptionBool(Nexus.Rules.OPTIONS.DOUG) && friendly && !Dying.IsGhoulOf(The.Player) && !Dying.IsBeguiledBy(The.Player))
                    return;                             //The.Player != this.Player if the player is dominating. Targets beguiled by a gameobject will not be loyal to gameobjects that they dominate, only the source object
                else                                    //so for us this means morality and friendship is relative to how AI feel about the player's current body rather than original body
                    new Deaths(Player, Dying, Killer, friendly, Dying.IsHostileTowards(The.Player)).Possibilities();
            }
        }

        static void MarkForEmbrace(GameObject Dying, bool isvampire) //only "feedable" targets can become vampires, but deathhandler only exists as a part on feedable objects, so the check is already done
        {                                   //corpse objects whose source object didnt have this part wont have the property at all and thus will not be embraceable
            foreach(var obj in Dying.CurrentCell.Objects)
            {
                if (obj.PropertyEquals("SourceID", Dying.ID))
                {
                    if (isvampire)
                    {
                        AddPlayerMessage($"{Dying.t()} burns to ashes!");
                        obj.SetStringProperty(FLAGS.EMBRACE.EMBRACEABLE, FLAGS.FALSE);
                    }
                    else if (Dying.TryGetPart(out Corpse corpse))
                        CompareBlueprints(Dying, obj, corpse);
                    return;
                }
            }
        }

        static void CompareBlueprints(GameObject Dying, GameObject obj, Corpse corpse)
        {
            if (obj.Blueprint == corpse.CorpseBlueprint)
            {
                obj.SetIntProperty(FLAGS.EMBRACE.LEVEL_ON_DEATH, Dying.Level);
                obj.SetStringProperty(FLAGS.EMBRACE.EMBRACEABLE, FLAGS.TRUE);
                EmbraceableObject copy = new(Dying);
                obj.AddPart(copy);

            }
            else if (obj.Blueprint == corpse.BurntCorpseBlueprint || obj.Blueprint == corpse.VaporizedCorpseBlueprint)
                obj.SetStringProperty(FLAGS.EMBRACE.EMBRACEABLE, FLAGS.FALSE);
        }

        /// <summary>
        /// Ensures that the Player field is assigned to the player's source, original GameObject and that the player is a vampire before beginning.
        /// </summary>
        /// <returns></returns>
        public static bool Security() => Player?.HasEffect<Dominated>() ?? true ? FindTruePlayer() : Player.HasPart<Vampirism>();
        static bool FindTruePlayer()
        {
            if (The.Player.TryGetEffect(out Dominated e))
                return FindMaster(e);
            else
            {
                _Player = The.Player.Reference();
                return Player.HasPart<Vampirism>();
            }
        }

        /// <summary>
        /// Loops through the domination effect's dominator to find the player's actual GameObject and assign it to the Player field.
        /// </summary>
        /// <returns></returns>
        static bool FindMaster(Dominated e)
        {
            if (!e.Dominator.HasEffect<Dominated>())
            {
                _Player = e.Dominator.Reference();
                return Player.HasPart<Vampirism>();
            }
            else
                return LoopDominator(e);
        }

        static bool LoopDominator(Dominated e)
        {
            GameObject TrueDominator = e.Dominator;
            while (TrueDominator.HasEffect<Dominated>())
            {
                Dominated d = TrueDominator.GetEffect<Dominated>();
                if (d != null)
                    TrueDominator = d.Dominator;
                else
                {
                    // Credits to _Cell for this 
                    Vehicle vehiclePart = TrueDominator.GetPart<Vehicle>();
                    if (vehiclePart != null && vehiclePart.Pilot != null)
                    {
                        TrueDominator = vehiclePart.Pilot;
                        MetricsManager.LogInfo("!!Found vehicle pilot!");
                    }
                    else
                    {

                        MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), "LoopDominator() failed to find the source player body. DeathHandler will not fire.");
                        return false;
                    }

                }
            }
            _Player = TrueDominator.Reference();
            return Player.HasPart<Vampirism>();
        }

        // bool LastResort()
        // {
        //     GameObject Object = GameObject.Find(x=>x.IsOriginalPlayerBody());
        //     Object.IsSelfControlledPlayer
        // }

    }

}