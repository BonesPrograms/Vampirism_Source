
using XRL.World.AI;
using System.Linq;
using System.Collections.Generic;
using XRL.World.Parts;
using XRL.World;
using XRL;
using VampirismSys.Core;
using XRL.World.Effects;


namespace VampirismSys.Stealth
{


    internal class Alert
    {
        readonly GameObject Source;

        /// <summary>
        /// For popups.
        /// </summary>
        internal GameObject Exposer;
        internal List<GameObject> Witnesses;

        /// <summary>
        /// Gives back a dictionary of all objects from the input list, with a string detailing what modifications were made.
        /// </summary>
        /// 

        internal const string defaultAlertText = "You are caught sneaking around by";
        internal const string altAlertText = "You are caught sneaking around!";

        /// <param name="source"></param>
        /// <param name="witnesses">It is recommended to use ValidSentients as the base for your list, because it is "validated" (see conditionals in StealthCore)
        /// and has none of the restrictions of the other lists, such as LOS, awareness, and distance.</param>
        /// <param name="exposer">If using spotters and the return value is SPOTTER_IN_DETECTION, it is recommended to assign the spotter to the exposer for consistency.
        /// </param>
        /// <param name="Target"></param>
        internal Alert(GameObject source, List<GameObject> witnesses, GameObject exposer = null)
        {
            this.Source = source;
            this.Witnesses = witnesses;
            this.Exposer = exposer;
        }

        internal Alert(GameObject source, GameObject exposer = null)
        {
            this.Source = source;
            this.Exposer = exposer;
            Witnesses = Alert.GiveDefaultList(source);
        }

        bool Validated(GameObject obj, uint AoE) => obj != null && obj.DistanceTo(Source) <= AoE;


        /// <summary>
        /// Quick use method for popups when stealth is broken.
        /// </summary>
        /// <param name="showExposer">Set this to false if you want to send in completely custom strings.</param>
        /// <param name="popupText"></param>
        /// <param name="backup">If showExposer is true and exposer is null, will default to backup that does not try to access Exposer.</param>
        internal void Popup(bool showExposer, string popupText = defaultAlertText, string backup = altAlertText)
        {
            if (showExposer)
            {
                if (Exposer is null)
                {
                    MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), "Error @ Alert<T>.Popup - Exposer is null. Playing alternative string.");
                    popupText = backup;
                }
                else
                    popupText = $"{popupText} {Exposer.t()}!";
            }
            else
                popupText = popupText == defaultAlertText ? backup : popupText;
            XRL.UI.Popup.Show(popupText);
        }

        /// <summary>
        /// Default template for a list that excludes plants. Usually, your target will be on the list, even if they are unaware.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>

        internal static List<GameObject> GiveDefaultList(GameObject source)
        {
            return source.CurrentZone.CombatObjects(x => StealthCore.ValidSentient(x)).ToList(); //this does not check for unawareness because it will wake up anyone who is unaware
        }

        /// <summary>
        /// Quick access method to wake up sleepers.
        /// </summary>
        /// <param name="AoE"></param>
        internal void RemoveSleepFromWitnesses(uint AoE = Rules.Stealth.AI_RADIUS) => RemoveEffectFromWitness<Asleep>(AoE);

        internal GameObject Add(GameObjectReference tgt)
        {
            return Add(tgt?.Object);
        }

        internal GameObject Add(GameObjectReference tgt, out bool isNull)
        {
            return Add(tgt?.Object, out isNull);
        }

        internal GameObject Add(GameObject tgt)
        {
            if (tgt != null)
            {
                Witnesses.SafeAddReference(tgt);
            }
            return tgt;
        }

        /// <summary>
        /// A method for safely adding the target to the list and instancing them so that they may be passed as parameter to FindClosestExposerInListExcept(Target).
        /// If your target may not be on the list and you want them to be part of the effect application, add them here.
        ///  here
        /// </summary>
        /// <param name="tgt"></param>
        /// <returns></returns>
        internal GameObject Add(GameObject tgt, out bool isNull)
        {
            Add(tgt);
            isNull = tgt is null;
            return tgt;
        }
        internal void AddOpinionToWitnesses<T>(uint radius = Rules.Stealth.AI_RADIUS) where T : IOpinionSubject, new()
        {
            Witnesses.Where(x => Validated(x, radius)).ForEach(x => x.AddOpinion<T>(Source));
        }
        // internal void AddEffectToWitnesses<T>(T obj, uint AoE = default) where T : Effect, new()
        // {
        //     InternalAddEffect<T>(obj, AoE);
        // }

        //these methods accept custom mod effects, however
        //you will have to assign default values to your fields/in your default constructor
        //because it can only use the default constructor for mod effects

        // internal void AddEffectToWitnessesAndExposer<T>(T obj, uint AoE = default) where T : Effect, new()
        // {
        //     if (CopyEffect.TryCopy(obj, out T effect))
        //     {
        //         Exposer?.ApplyEffect(effect);
        //         InternalAddEffect<T>(obj, AoE);
        //     }
        // }

        internal void AddOpinionToWitnessesAndExposer<T>(uint radius = Rules.Stealth.AI_RADIUS) where T : IOpinionSubject, new()
        {
            Exposer?.AddOpinion<T>(Source);
            AddOpinionToWitnesses<T>(radius);
        }

        /// <summary>
        /// Finds the closest person and assigns them as the "exposer" for popups.
        /// </summary>

        internal void FindClosestExposerInList()
        {
            ProcessList(null);
        }

        /// <summary>
        /// If your target is showing up as the exposer and you want to prevent it, pass them by this method.
        /// </summary>
        /// <param name="tgt"></param>
        internal void FindClosestExposerInListExcept(GameObject tgt)
        {
            ProcessList(tgt);
        }
        internal void RemoveEffectFromWitness<T>(uint radius = Rules.Stealth.AI_RADIUS) where T : Effect, new()
        {
            Witnesses.Where(x => Validated(x, radius)).ForEach(x => x.RemoveEffect<T>());
        }
        void ProcessList(GameObject tgt)
        {
            Exposer = CreateDictionaryOfRanges(tgt);
        }

        GameObject CreateDictionaryOfRanges(GameObject tgt)
        {
            Dictionary<GameObject, int> distances = new();
            Witnesses.Where(x => Source.HasLOSTo(x, false) && x != Source && x != tgt).ForEach(x => distances[x] = Source.DistanceTo(x));
            return ReturnKey(distances);

        }

        GameObject ReturnKey(Dictionary<GameObject, int> distances)
        {
            if (distances.Count != 0)
            {
                int min = distances.Values.Min();
                return distances.First(x => x.Value == min).Key;
            }
            else
            {
                MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), "Err @ Alert.FindClosestExposerInList(Except?) - no one in list has LOS to player, no exposer assigned.");
                return null;
            }
        }

        // void InternalAddEffect<T>(T obj, uint AoE) where T : Effect, new()
        // {
        //     AoE = this.AoE(AoE);
        //     for (int i = 0; i < Witnesses.Count; i++)
        //     {
        //         GameObject gameObject = Witnesses[i];
        //         if (Validated(gameObject, AoE) && CopyEffect.TryCopy(obj, out T effect))
        //             gameObject.ApplyEffect(effect);
        //     }

        // }
    }
}
