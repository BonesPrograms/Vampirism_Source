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
using System.Linq;

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
        public static GameObject Player => _player?.Object; //this is used for two major purposes: accessing the players humanity and checking hostility
                                                            //if you try to access by the.player (static) then you will get whatever
        [GameBasedStaticCache(false)]                       //gameobject they are currently dominating
        static GameObjectReference _player;

        //instead of the gameobject that is "really" them 
        public bool FinishedInit;                                   //meaning: we cant find the humanity part, and innocence becomes relative to whatever gameobject the player is currently dominating
        public override bool WantEvent(int ID, int cascade)     //so you could dominate a snapjaw, and load a zone with snapjaws, and then come back as the original player
        {                                                       //start feeding on them and then lose humanity because they have the innocent flag
            if (ID == SingletonEvent<BeforeTakeActionEvent>.ID) //(for various reasons, checking hostility on death doesnt work)
                return !FinishedInit;
            if (ID == TookDamageEvent.ID)
                return Options.GetOptionBool(ModOptions.FRACTUS_NERF);
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
            Security(); //changed - now that we can guarantee security always finds the player, even if the player is not a vampire, innocence will be prepared for them
            FinishedInit = Init.Evaluate(ParentObject, Player);
            return base.HandleEvent(E);
        }
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
            if (FreeMote || WikiRng.Next(1, 5000) <= 1)
                Dying.CurrentCell.AddObject("MoteOfHumanity");
        }
        static void CreateDeathsInstance(GameObject Killer, GameObject Dying)
        {
            if (Options.GetOptionBool(Nexus.Rules.ModOptions.HUMANITY) && Security() && !Player.CheckFlag(Flags.GO) && !Dying.HasStringProperty(Flags.DEAD))
            {
                bool friendly = Dying.IsFriendly(The.Player);
                if (Options.GetOptionBool(Nexus.Rules.ModOptions.DOUG) && friendly && !Dying.IsGhoulOf(The.Player) && !Dying.IsBeguiledBy(The.Player))
                    return;                             //The.Player != this.Player if the player is dominating. Targets beguiled by a gameobject will not be loyal to gameobjects that they dominate, only the source object
                else                                    //so for us this means morality and friendship is relative to how AI feel about the player's current body rather than original body
                    new Deaths(Player, Dying, Killer, friendly, Dying.IsHostileTowards(The.Player)).Possibilities();
            }
        }

        static void MarkForEmbrace(GameObject Dying, bool isvampire) //only "feedable" targets can become vampires, but deathhandler only exists as a part on feedable objects, so the check is already done
        {                                   //corpse objects whose source object didnt have this part wont have the property at all and thus will not be embraceable
            var obj = Dying.CurrentCell.Objects.FirstOrDefault(x => x.PropertyEquals("SourceID", Dying.ID));
            if (obj != null)
                DetermineEmbraceability(obj, Dying, isvampire);

        }

        static void DetermineEmbraceability(GameObject obj, GameObject Dying, bool isvampire)
        {
            if (isvampire)
            {
                AddPlayerMessage($"{Dying.t()} burns to ashes!");
                obj.SetStringProperty(Flags.Embrace.EMBRACEABLE, Flags.FALSE);
            }
            else if (Dying.TryGetPart(out Corpse corpse))
                CompareBlueprints(Dying, obj, corpse);
        }

        static void CompareBlueprints(GameObject Dying, GameObject obj, Corpse corpse)
        {
            //  if (obj.Blueprint == corpse.CorpseBlueprint)
            if (obj.Blueprint == "Ash" || obj.Blueprint == corpse.BurntCorpseBlueprint || obj.Blueprint == corpse.VaporizedCorpseBlueprint)
                obj.SetStringProperty(Flags.Embrace.EMBRACEABLE, Flags.FALSE);
            else
            {
                obj.SetIntProperty(Flags.Embrace.LEVEL_ON_DEATH, Dying.Level);
                obj.SetStringProperty(Flags.Embrace.EMBRACEABLE, Flags.TRUE);
                EmbraceableObject copy = new(Dying);
                obj.AddPart(copy);
            }
        }

        /// <summary>
        /// Ensures that the Player field is assigned to the player's source, original GameObject and that the player is a vampire before beginning.
        /// </summary>
        /// <returns></returns>
        public static bool Security() => !Player?.HasHitpoints() ?? true ? FindAndCheckPlayer() : Player.HasPart<Vampirism>();
        //because you can die but still not be null and the system will break if you are domination-hopping to a new body
        static bool FindAndCheckPlayer()
        {
            _player = PlayerFinder().Reference();
            return Player.HasPart<Vampirism>();
        }
        static GameObject PlayerFinder()
        {
            if (The.Player.TryGetEffect(out Dominated e))
                return LoopDominator(e);
            else if (The.Player.TryGetPart(out Vehicle v))
                return CheckPilot(v.Pilot);
            return The.Player;
        }

        static GameObject CheckPilot(GameObject pilot)
        {
            if (pilot.TryGetEffect(out Dominated e))
                return LoopDominator(e);
            return pilot;
        }

        /// <summary>
        /// Loops through the domination effect's dominator to find the player's actual GameObject and assign it to the Player field.
        /// </summary>
        /// <returns></returns>
        static GameObject LoopDominator(Dominated e)
        {
            GameObject TrueDominator = e.Dominator;
            while (TrueDominator.HasEffect<Dominated>())
            {
                Dominated d = TrueDominator.GetEffect<Dominated>();
                TrueDominator = d.Dominator;
            }
            if (TrueDominator.TryGetPart(out Vehicle v))
                return CheckPilot(v.Pilot);
            return TrueDominator;
        }
        // bool LastResort()
        // {
        //     GameObject Object = GameObject.Find(x=>x.IsOriginalPlayerBody());
        //     Object.IsSelfControlledPlayer
        // }

    }

}