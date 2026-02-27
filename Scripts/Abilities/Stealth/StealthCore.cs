using XRL.World;
using System.Collections.Generic;
using XRL.World.Parts.Mutation;
using Nexus.Core;
using XRL.World.Effects;
using XRL.World.Parts;
using XRL;
using System;
using XRL.Collections;

namespace Nexus.Stealth
{
    /// <summary>
    /// Scans the environment and constantly updates the lists used in Nightbeast.
    /// </summary>
    [HasGameBasedStaticCache]
    public static class StealthCore
    {
        public static GameObject Player => The.Player;

        [GameBasedStaticCache]
        public static LightLevel? LightLevel;

        [GameBasedStaticCache]
        static int _TrueCount = 0;
        public static int TrueCount => _TrueCount;
        static GameObject[] KeyArray => Nightbeast.KeyArray;
        public static void ScanEnvironment(Zone zone)
        {
            zone.Mapper(delegate (Cell cell) { if (cell.HasObjectWithPart(nameof(Brain))) cell.Objects.ForEach(x => CheckValidity(x)); });
        }
        public static void Stealth()
        {
            _TrueCount = default;
            KeyArray.ForEach(delegate (GameObject obj)
            {
                if (!obj?.HasHitpoints() ?? true || !obj.InSameZone(The.Player))
                {
                    Nightbeast.Witnesses.Remove(obj);
                }
                else
                {
                    bool check = NearbySentient(obj) && ActiveWitness(obj); //but this can change actively!
                    Nightbeast.Witnesses[obj] = check;
                    if (check)
                        _TrueCount++; //the count is re-iterated every single turn
                }
            });
            if (Nightbeast.Witnesses.Count != KeyArray.Length)
            {
                Nightbeast.UpdateKeys();
            }
        }

        static void CheckValidity(GameObject obj) //zoneload
        {
            if (ValidSentient(obj))
            {
                Nightbeast.Witnesses[obj] = NearbySentient(obj) && ActiveWitness(obj);
            }
        }

        /// <summary>
        /// The evaluation that separates a NearbySentient from an ActiveWitness. It checks if they are aware and if they can see you based on light levels.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        /// 
        /// 
        public static bool ActiveWitness(GameObject obj)
        {
            return !obj.Unaware(false) && !Shrouded(obj) && !IsFriendly(obj) && obj.HasHitpoints() && !InDominationChain(obj.Effects);
        }

        public static bool IsFriendly(GameObject who)
        {
            return who.IsInLoveWith(Player) || who.InSamePartyAs(Player) || who.IsPlayerControlled() || who.IsPlayerLed();
        }

        //Custom version of IsFriendly - the extension method in Core was causing a major bug. People who were allied to you were not considered witnesses, which was very noticeable when dominating
        //a vampiric farmer in Joppa.
        //this caused a serious bug that took me HOURS to figure out (i never tested stealth as an NPC vampire) becasue i was in the middle of a rework of the system and didnt know where the issue was
        //really this issue has existed since day one and im surprised no one reported it yet
        //the bug in question - everyone in joppa is allied to local farmers. player dominates a farmer, thus they are their allies.
        //  allies do not fight back if you kill them. therefore, you are allowed a free feed on anyone who is considered an ally
        // however, because allies were showing up as witnesses, they would expose the farmer and become hostile

        /// <summary>
        /// The evaluation that separates a NearbySentient from a ValidSentient. It restricts by AI RADIUS and LOS.
        /// </summary>
        /// <param name="witness"></param>
        /// <returns></returns>
        public static bool NearbySentient(GameObject witness)
        {
            return witness.HasLOSTo(Player, false) && witness.DistanceTo(Player) <= Nexus.Rules.STEALTH.AI_RADIUS && witness.InSameZone(Player);
        }

        /// <summary>
        /// The evaluation that is mostly for security and keeps friendlies, dead people, yourself (if dominating), and objects outside the zone off the list.
        /// </summary>
        /// <param name="witness"></param>
        /// <returns></returns>
        public static bool ValidSentient(GameObject witness)
          =>
            witness?.Brain != null
            && witness != Player
            && witness.IsCombatObject()
            && !Inanimate(witness); //insamezone check cannot go here because we use this to check nextzone in EZ event and i dont feel like adding a zone parameter
        public static bool Inanimate(GameObject witness)
     =>
        witness.Body?.Anatomy == "Echinoid"
        || CheckTags(witness.GetBlueprint())
        || CheckParts(witness.PartsList);

        /// <summary>
        /// It is recommended to exclude plants from your lists of witnesses (you'll see me do it often in Alert and Spotter), because being spotted by vines, roots and
        /// ivories felt strange.
        /// </summary>
        /// 

        static bool InDominationChain(Rack<Effect> effects)
         => effects.IfEachReturn(delegate (Effect e)
        {
            Type type = e.GetType();
            return type == typeof(Dominated) || type == typeof(Dominating);
        });


        static bool CheckTags(GameObjectBlueprint Blueprint) => Blueprint.Tags?.IfEachReturn(x => CheckPair(x)) ?? false;

        static bool CheckPair(KeyValuePair<string, string> data)
        {
            if (data.Key == "Culture" && (data.Value == "Plant" || data.Value == "Fungal"))
                return true;
            if (data.Key == "Class" && (data.Value == "fungus" || data.Value == "root"))
                return true;
            if (data.Key == "Species" && data.Value == "root")
                return true;
            if (CheckKey(data.Key))
                return true;
            return false;
        }

        static bool CheckKey(string key)
        {
            return key == "LivePlant" || key == "Plank" || key == "HangingSupport" || key == "LiveFungus" || key == "ExcludeFromHostiles";
        }

        static bool CheckParts(PartRack rack)
         => rack.IfEachReturn(delegate (IPart part)
         {
             Type type = part.GetType();
             return type == typeof(Harvestable) || type == typeof(PlantProperties) || type == typeof(FungusProperties);
         });


        /// <summary>
        /// Simple method that evaluates if you are detectable via lighting. Light levels in a cell are relative to what the player can see only,
        ///  and if you are using nightvision, your light level is technically not dark. This method considers those extra possibilities to ensure everything works.
        /// </summary>
        /// <param name="witness"></param>
        /// <returns></returns>

        static bool Shrouded(GameObject witness)
         =>
            LightLevel switch
            {
                XRL.World.LightLevel.None or XRL.World.LightLevel.Darkvision or XRL.World.LightLevel.Dimvision
                => !SpottedByDarkvision(witness, Player.DistanceTo(witness)), //shrouded must return true, but if SpottedByDarkvision is true, then we have to return false
                null => BadLight(),
                _ => false
            };

        static bool BadLight()
        {
            string err = "Err @ StealthCore.Shrouded()";
            string message = Player?.CurrentCell is null ? $"{err} : current cell is null, lightlevel null" : $"{err} : attempting to access Shrouded() without assigning light level!";
            MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), message);
            return false;
        }

        static bool SpottedByDarkvision(GameObject witness, int DistanceTo)
        {
            // if (witness.TryGetPart(out DarkVision D) && DistanceTo <= D.Radius)
            //     return true;
            if (witness.TryGetPart(out HeightenedSmell HS) && DistanceTo <= HS.GetRadius())
                return true;
            if (witness.TryGetPart(out HeightenedHearing HH) && DistanceTo <= HH.GetRadius())
                return true;
            //    if (witness.TryGetPart(out XRL.World.Parts.Mutation.NightVision N) && distance <= N.Level * 5)
            //   return true; // suspeneded until i can figure out how the actual range for nightvision works
            return false;
        }


    }
}

