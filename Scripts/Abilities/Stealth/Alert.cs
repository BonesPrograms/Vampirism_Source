
using XRL.World.AI;
using System.Linq;
using System.Collections.Generic;
using XRL.World.Parts;
using XRL.World;
using XRL;
using Nexus.Core;
using XRL.World.Effects;


namespace Nexus.Stealth
{


    class Alert
    {
        readonly Nightbeast Source;

        /// <summary>
        /// For popups.
        /// </summary>
        public GameObject Exposer;
        public List<GameObject> Witnesses;

        /// <summary>
        /// Gives back a dictionary of all objects from the input list, with a string detailing what modifications were made.
        /// </summary>
        /// 

        public const string defaultAlertText = "You are caught sneaking around by";
        public const string altAlertText = "You are caught sneaking around!";
        public uint AoE(uint AoE) => AoE == default ? Nexus.Rules.STEALTH.AI_RADIUS : AoE;

        /// <param name="Source"></param>
        /// <param name="Witnesses">It is recommended to use ValidSentients as the base for your list, because it is "validated" (see conditionals in StealthCore)
        /// and has none of the restrictions of the other lists, such as LOS, awareness, and distance.</param>
        /// <param name="Exposer">If using spotters and the return value is SPOTTER_IN_DETECTION, it is recommended to assign the spotter to the exposer for consistency.
        /// </param>
        /// <param name="Target"></param>
        public Alert(Nightbeast Source, List<GameObject> Witnesses, GameObject Exposer = null)
        {
            this.Source = Source;
            this.Witnesses = Witnesses;
            this.Exposer = Exposer;
        }

        bool Validated(GameObject obj, uint AoE) => obj != null && obj.DistanceTo(Source.ParentObject) <= AoE;


        /// <summary>
        /// Quick use method for popups when stealth is broken.
        /// </summary>
        /// <param name="ShowExposer">Set this to false if you want to send in completely custom strings.</param>
        /// <param name="PopupText"></param>
        /// <param name="backup">If showExposer is true and exposer is null, will default to backup that does not try to access Exposer.</param>
        public void Popup(bool ShowExposer, string PopupText = defaultAlertText, string backup = altAlertText)
        {
            if (ShowExposer)
            {
                if (Exposer is null)
                {
                    MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), "Error @ Alert<T>.Popup - Exposer is null. Playing alternative string.");
                    PopupText = backup;
                }
                else
                    PopupText = $"{PopupText} {Exposer.t()}!";
            }
            else
                PopupText = PopupText == defaultAlertText ? backup : PopupText;
            XRL.UI.Popup.Show(PopupText);
        }

        /// <summary>
        /// Default template for a list that excludes plants. Usually, your target will be on the list, even if they are unaware.
        /// </summary>
        /// <param name="Source"></param>
        /// <returns></returns>

        public static List<GameObject> GiveDefaultList(Nightbeast Source)
        {
            List<GameObject> local = new(); //Notice how we pull from ValidSentients, which has no LOS, awareness, or distance restrictions.
            for (int i = 0; i < Source.ValidSentients.Count; i++)
            {
                GameObject obj = Source.ValidSentients[i];
                if (!StealthCore.Inanimate(obj))
                    local.Add(obj);
            }
            return local;
        }
        /// <summary>
        /// Quick access method to wake up sleepers.
        /// </summary>
        /// <param name="AoE"></param>
        public void RemoveSleepFromWitnesses(uint AoE = default) => RemoveEffectFromWitness<Asleep>(AoE);

        public GameObject SafeAdd(GameObjectReference Target)
        {
            return SafeAdd(Target?.Object);
        }

        public GameObject SafeAdd(GameObjectReference Target, out bool IsNull)
        {
            return SafeAdd(Target?.Object, out IsNull);
        }

        public GameObject SafeAdd(GameObject Target)
        {
            if (Target != null && !Witnesses.Contains(Target))
            {
                Witnesses.Add(Target);
            }
            return Target;
        }

        /// <summary>
        /// A method for safely adding the target to the list and instancing them so that they may be passed as parameter to FindClosestExposerInListExcept(Target).
        /// If your target may not be on the list and you want them to be part of the effect application, add them here.
        ///  here
        /// </summary>
        /// <param name="Target"></param>
        /// <returns></returns>
        public GameObject SafeAdd(GameObject Target, out bool IsNull)
        {
            SafeAdd(Target);
            IsNull = Target is null;
            return Target;
        }
        public void AddOpinionToWitnesses<T>(uint AoE = default) where T : IOpinionSubject, new()
        {
            AoE = this.AoE(AoE);
            for (int i = 0; i < Witnesses.Count; i++)
            {
                GameObject obj = Witnesses[i];
                if (Validated(obj, AoE))
                {
                    obj.AddOpinion<T>(Source.ParentObject);
                }
            }
        }
        public void AddEffectToWitnesses<T>(T obj, uint AoE = default) where T : Effect, new()
        {
            InternalAddEffect<T>(obj, AoE);
        }

        //these methods accept custom mod effects, however
        //you will have to assign default values to your fields/in your default constructor
        //because it can only use the default constructor for mod effects
        public void AddEffectToWitnessesAndExposer<T>(T obj, uint AoE = default) where T : Effect, new()
        {
            if (CopyEffect.TryCopy(obj, out T effect))
            {
                Exposer?.ApplyEffect(effect);
                InternalAddEffect<T>(obj, AoE);
            }
        }

        public void AddOpinionToWitnessesAndExposer<T>(uint AoE = default) where T : IOpinionSubject, new()
        {
            Exposer?.AddOpinion<T>(Source.ParentObject);
            AddOpinionToWitnesses<T>(AoE);
        }

        /// <summary>
        /// Finds the closest person and assigns them as the "exposer" for popups.
        /// </summary>

        public void FindClosestExposerInList()
        {
            ProcessList(null);
        }

        /// <summary>
        /// If your target is showing up as the exposer and you want to prevent it, pass them by this method.
        /// </summary>
        /// <param name="Target"></param>
        public void FindClosestExposerInListExcept(GameObject Target)
        {
            ProcessList(Target);
        }
        public void RemoveEffectFromWitness<T>(uint AoE = default) where T : Effect, new()
        {
            AoE = this.AoE(AoE);
            for (int i = 0; i < Witnesses.Count; i++)
            {
                GameObject obj = Witnesses[i];
                if (Validated(obj, AoE) && obj.HasEffect<T>())
                {
                    obj.RemoveEffect<T>();
                }
            }

        }
        void ProcessList(GameObject Target)
        {
            if (Witnesses.Count == 1)
            {
                if (Source.ParentObject.HasLOSTo(Witnesses[0], false) && Witnesses[0] != Target)
                    Exposer = Witnesses[0];
            }
            Exposer ??= CreateDictionaryOfRanges(Target);
        }

        GameObject CreateDictionaryOfRanges(GameObject Target)
        {
            Dictionary<GameObject, int> distances = new();
            for (int i = 0; i < Witnesses.Count; i++)
            {
                GameObject obj = Witnesses[i];
                if (Source.ParentObject.HasLOSTo(obj, false) && obj != Target)
                    distances.Add(obj, Source.ParentObject.DistanceTo(obj));
            }
            return ReturnKey(distances);

        }

        GameObject ReturnKey(Dictionary<GameObject, int> distances)
        {
            if (distances.Count != 0)
            {
                if (distances.Count != 1)
                {
                    int min = distances.Values.Min();
                    return distances.First(x => x.Value == min).Key;
                }
                else
                    return distances.ElementAt(0).Key;
            }
            else
            {
                MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), "Err @ Alert.FindClosestExposerInList(Except?) - no one in list has LOS to player, no exposer assigned.");
                return null;
            }
        }

        void InternalAddEffect<T>(T obj, uint AoE) where T : Effect, new()
        {
            AoE = this.AoE(AoE);
            for (int i = 0; i < Witnesses.Count; i++)
            {
                GameObject gameObject = Witnesses[i];
                if (Validated(gameObject, AoE) && CopyEffect.TryCopy(obj, out T effect))
                    gameObject.ApplyEffect(effect);
            }

        }
    }
}
