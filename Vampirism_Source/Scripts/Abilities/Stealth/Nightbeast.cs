using System;
using System.Collections.Generic;
using System.Linq;
using XRL.World.Effects;
using XRL.World.AI;
using Nexus.Properties;
using Nexus.Stealth;
using Nexus.Core;
using XRL.World.Capabilities;

namespace XRL.World.Parts
{

	/// <summary>
	/// The stealth system for vampirism that enables stealth feeding and introduces witnesses.
	/// </summary>
	[Serializable]

	public class Nightbeast : IPart
	{


		/// <summary>
		/// All objects with part brain in the zone.
		/// </summary>
		[NonSerialized]
		public List<GameObject> Sentients = new();


		[NonSerialized]
		public List<GameObject> ValidSentients = new();

		[NonSerialized]
		public List<GameObject> NearbySentients = new();

		/// <summary>
		/// List of NearbySentients that are in LOS and that are aware: not asleep, etc. For active stealth checks.
		/// </summary>

		[NonSerialized]
		public List<GameObject> ActiveWitnesses = new();

		/// <summary>
		/// Stage one means that there is only one witness.
		/// </summary>

		public bool StealthStage1;

		/// <summary>
		/// Stage two means there are no witnesses.
		/// </summary>

		public bool StealthStage2;

		/// <summary>
		/// For if you want to hide the display messages that play with stealth in chat. Should be set and reset manually.
		/// </summary>
		/// 
		[NonSerialized]
		public bool HideMessages = false;

		/// <summary>
		/// For if you want to force the stealth system to switch from stealth to exposed. Should be set and reset manually.
		/// </summary>

		[NonSerialized]
		bool ForceExposure = false;

		/// <summary>
		/// Allows you to have plants be considered valid witnesses. Indiscriminate, applies to all different kinds of plants with a brain part, from vines to glowpads.
		/// </summary>
		[NonSerialized]
		public bool ConsiderPlantsWitnesses = false;

		/// <summary>
		/// Reduces the ActiveWitness list count to 0 before it can make it out of StealthCore.
		/// </summary>
		[NonSerialized]
		public bool ForceStealth = false;

		/// <summary>
		/// Max distance - any AI beyond this distance is not considered a witness.
		/// </summary>
		[NonSerialized]
		public uint AI_RADIUS = DEFAULT_RADIUS;

		[NonSerialized]
		const int DEFAULT_RADIUS = 21;

		[NonSerialized]
		public bool AllowCombatStealth;

		/// <summary>
		/// Allows you to remove objects from the ActiveWitnesses list before stealth evaluation can begin.
		/// </summary>
		[NonSerialized]
		public List<GameObject> Exclusions;

		/// <summary>
		/// Allows you to add objects to the ActiveWitnesses list before stealth evaluation begins.
		/// </summary>
		[NonSerialized]

		public List<GameObject> Additions;
		//public void BringWitness() => new SpotterCore(this).CreateSpotter(10);
		public bool BeginAttackCheckIfSpotted<TOpinion>(out GameObject Spotter, uint AoE = default, bool Alert = true) where TOpinion : IOpinionSubject, new()
		{
			Spotter = null;
			Dictionary<GameObject, int> PotentialWitnesses = new();
			StealthCore Core = new();
			foreach (GameObject witness in ValidSentients)
			{
				if (witness.canPathTo(ParentObject.CurrentCell) && !Scan.Unaware(witness, false))
				{
					if (ConsiderPlantsWitnesses)
						PotentialWitnesses.Add(witness, witness.DistanceTo(ParentObject));
					else if (!Core.Plant(witness))
						PotentialWitnesses.Add(witness, witness.DistanceTo(ParentObject));
				}
			}
			if (PotentialWitnesses.Count != 0)
			{
				int minimumvalue = PotentialWitnesses.Values.Min();
				Spotter = PotentialWitnesses.First(x => x.Value == minimumvalue).Key;
			}
			else //return valid for stealth if spotter is null/list is empty
				return false;
			int distance = Spotter.DistanceTo(ParentObject); //it occurs to me reading this in git that i already have the distance lol. ill fix it later
			bool value = (distance == AI_RADIUS || distance == AI_RADIUS + 1) && Spotter.HasLOSTo(ParentObject);
			if (value == true)
			{
				List<GameObject> MyWitnesses = PotentialWitnesses.Keys.ToList();
				UI.Popup.Show($"You try to sneak attack, but {Spotter.t()} spots you from a distance!");
				if (Alert)
					Alert<OpinionDominate>(MyWitnesses, null, false, Spotter, null, null, false, true, false, true, true, true, AoE);
				else
					Spotter.AddOpinion<TOpinion>(ParentObject);
			}
			else
				Spotter.ApplyEffect(new Spotter(ParentObject, Nexus.Rules.FEED.DURATION));
			return value; 
		}
		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade))
				return ID == SingletonEvent<BeforeTakeActionEvent>.ID;
			return true;
		}
		/// <summary>
		/// For those who want to evaluate or modify stealth externally, especially on a turn-by-turn basis, it is paramount to do so within the BeforeTakeActionEvent handler, so that they are perfectly synced.
		/// Any "reactions" (such as Alert, stealth being broken, setting an effect duration to 0) should also be handled in the same method to avoid a noticeable waiting period. The EndTurnEvent is not recommended.
		/// </summary>
		public override bool HandleEvent(BeforeTakeActionEvent E)
		{
			if (!AutoAct.IsActive() && ParentObject.IsPlayer())
			{
				if (!ForceExposure)
				{
					if (!ParentObject.IsInCombat() || AllowCombatStealth)
						RunStealthSystem();
					else if (StealthStage1 || StealthStage2)
						HaltStealthSystem();
				}

			}
			return base.HandleEvent(E);
		}

		public void ForceExposureOff() => ForceExposure = false;

		public void ForceExpose()
		{
			ForceExposure = true;
			HaltStealthSystem("");
		}
		public void ForceExpose(string text)
		{
			ForceExposure = true;
			HaltStealthSystem(text);
		}
		void HaltStealthSystem(string text = "{{R|Spotted!}}")
		{
			if (!HideMessages && ParentObject.Target is null && text != "")
				AddPlayerMessage(text);
			ParentObject.SetStringProperty(Flags.STEALTH, Flags.FALSE);
			StealthStage1 = false;
			StealthStage2 = false;
		}
		public void RunStealthSystem()
		{
			Sentients = ParentObject.CurrentZone.GetObjectsWithPart(nameof(Brain));
			new StealthCore(this, ParentObject?.CurrentCell?.GetLight(), ConsiderPlantsWitnesses, Exclusions, Additions, ForceStealth).ScanEnvironment();
			new ActiveStealth(this, HideMessages).SetStealth(ActiveWitnesses.Count);
		}

		/// <summary>
		/// Makes sure you are attacking the "one witness" before making an attack.
		/// </summary>
		/// <param name="Target"></param>
		/// <returns></returns>
		public bool BeforeAttackValidate(GameObject Target) => (StealthStage1 || StealthStage2) && (ActiveWitnesses.Count == 0 || ActiveWitnesses.Contains(Target));

		/// <summary>
		/// This one is for if you don't care about the target, and just want a general readout for stealth state.
		/// </summary>
		public bool BeforeAttackValidate(bool CheckForOneWitness, bool CheckForNoWitnesses) => (CheckForNoWitnesses && StealthStage2) || (CheckForOneWitness && StealthStage1);

		const string defaultAlertText = "You are caught sneaking around by";

		/// 	this method has a lot of uses. you could use it to flashbang everyone on the list as part of a ninja ability if the player gets exposed. can be used to blow up the exposer's head
		/// with an effect and play a custom popup for it, whatever you want
		public List<GameObject> Alert<T>(List<GameObject> PeopleToAlert, string PopupText = defaultAlertText, bool FindExposerFromList = true, GameObject Exposer = null, Effect PeopleToAlertEffect = null, Effect ExposerEffect = null, bool ShowExposer = true, bool AddOpinionToExposer = true, bool ShowPopup = true, bool WakeSleepers = true, bool AlertPeopleListed = true, bool AddOpinionToPeopleListed = true, uint AoE = default) where T : IOpinionSubject, new()
		{
			if (PeopleToAlert.Count == 0 || PeopleToAlert is null)
			{
				MetricsManager.LogModError(ModManager.GetMod("vampirism"), "Error@ Nightbeast.Alert<T>: List count is 0 or list is null. Returning early.");
				return PeopleToAlert;
			}
			List<GameObject> Internal = new(PeopleToAlert);
			if (FindExposerFromList)
				Exposer = FindExposer(Internal, Exposer);
			if (ShowPopup)
			{
				if (PopupText is not null or "")
					ShowPopupMessages(Exposer, ShowExposer, FindExposerFromList, PopupText);
				else
					MetricsManager.LogModError(ModManager.GetMod("vampirism"), "Error@ Nightbeast.Alert<T>: ShowPopup set to true, but string PopupText set to null or empty, skipping popup.");
			}
			else if (ShowExposer)
				MetricsManager.LogModError(ModManager.GetMod("vampirism"), "Error@ Nightbeast.Alert<T>: ShowExposer set to true, but Showpopup set to false, will not display exposer.");
			if (ShowExposer && !FindExposerFromList && Exposer is null)
				MetricsManager.LogModError(ModManager.GetMod("vampirism"), "Error@ Nightbeast.Alert<T>: ShowExposer set to true, but no exposer was provided and FindExposerFromList set to false.");
			if (ExposerEffect is not null)
				AddEffect(ExposerEffect, Exposer, Internal);
			if (AddOpinionToExposer)
				AddOpinion<T>(Internal, Exposer);
			if (AlertPeopleListed)
				ProcessList<T>(Internal, Exposer, AoE, WakeSleepers, AddOpinionToPeopleListed, PeopleToAlertEffect);
			else if (WakeSleepers || AddOpinionToPeopleListed || PeopleToAlertEffect is not null)
				MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), "Error @ Nightbeast.Alert<T>: Attempting to Wake Sleepers or AddOpinion or Apply PeopleToAlertEffect without assigning AlertPeopleListed = true.");
			return Internal;
		}

		void AddEffect(Effect effect, GameObject Exposer, List<GameObject> Internal)
		{
			if (Exposer is not null)
			{
				Exposer.ApplyEffect(effect);
				if (Internal.Contains(Exposer))
					Internal.Remove(Exposer);
			}
			else
				MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), "Error @ Nightbeast.Alert<T>: Tried adding effect to Exposer, but Exposer was null");
		}

		void AddOpinion<T>(List<GameObject> Internal, GameObject Exposer) where T : IOpinionSubject, new()
		{
			if (Exposer is not null)
			{
				Exposer.AddOpinion<T>(ParentObject);
				if (Internal.Contains(Exposer))
					Internal.Remove(Exposer);
			}
			else
				MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), "Error @ Nightbeast.Alert<T>: AddOpinionToExposer set to true, but Exposer was null");

		}

		void ProcessList<T>(List<GameObject> Internal, GameObject Exposer, uint AoE, bool WakeSleepers, bool AddOpinionToPeopleListed, Effect PeopleToAlertEffect) where T : IOpinionSubject, new()
		{
			AoE = AoE == default ? AI_RADIUS + 1 : AoE; //automatically uses spotter range (max + 1)
			foreach (GameObject witness in Internal.ToList())
				if (witness != Exposer)
					ProcessWitness<T>(Internal, PeopleToAlertEffect, witness, WakeSleepers, AddOpinionToPeopleListed, AoE);

		}

		void ProcessWitness<T>(List<GameObject> Internal, Effect PeopleToAlertEffect, GameObject witness, bool WakeSleepers, bool AddOpinionToPeopleListed, uint AoE) where T : IOpinionSubject, new()
		{
			if (witness.DistanceTo(ParentObject) <= AoE)
			{
				if (WakeSleepers && witness.HasEffect<Asleep>())
					witness.RemoveEffect<Asleep>();
				if (AddOpinionToPeopleListed)
					witness.AddOpinion<T>(ParentObject);
				if (PeopleToAlertEffect is not null)
					witness.ApplyEffect(PeopleToAlertEffect);
				if (AddOpinionToPeopleListed || WakeSleepers || PeopleToAlertEffect is not null)
					Internal.Remove(witness);
			}
		}

		void ShowPopupMessages(GameObject Exposer, bool ShowExposer, bool FindExposerFromList, string PopupText)
		{
			if (Exposer is null && ShowExposer)
				MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), !FindExposerFromList ? "Error @ Nightbeast.ShowPopupMessages: FindExposerFromList set to false, showExposer set to true, but an Exposer was not provided." : "Error @ Nightbeast.ShowPopupMessages: showExposer set to true, FindExposerFromList set to true, but an Exposer was not found.");
			if (ShowExposer && Exposer is not null)
				PopupText = $"{PopupText} {Exposer.t()}!";
			else if (!ShowExposer) //can set showexposer to false if you want to send in totally custom strings entirely
				PopupText = PopupText == defaultAlertText ? "Your stealth is broken!" : PopupText;
			UI.Popup.Show(PopupText);

		}

		GameObject FindExposer(List<GameObject> Internal, GameObject Exposer)
		{
			if (Exposer is not null)
			{
				MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), "Error @ Nightbeast.FindExposer(): FindExposerFromList set to true in Alert<T>, but an Exposer was provided. Returning original exposer.");
				return Exposer;
			}
			if (Internal.Count == 1)
			{
				if (ParentObject.HasLOSTo(Internal[0], false))
					Exposer = Internal[0];
				if (Exposer is null)
					MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), "Error @ Nightbeast.FindExposer: FindExposerFromList set to true in Alert<T>, but list only has one object, and object does not have LOS to the player.");
			}
			else
				Exposer = CreateDictionaryOfRanges(Internal);
			return Exposer;
		}

		GameObject CreateDictionaryOfRanges(List<GameObject> Internal)
		{
			Dictionary<GameObject, int> distances = new();
			foreach (var obj in Internal)
				if (ParentObject.HasLOSTo(obj, false))
					distances.Add(obj, ParentObject.DistanceTo(obj));
			if (distances.Count != 0)
				return ReturnExposerByDistance(distances);
			else
				MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), "Error @ Nightbeast.FindExposer: FindExposerFromList set to true in Alert<T>, but no one in the provided list has LOS to the player, could not find object to assign to Exposer.");
			return null;
		}

		GameObject ReturnExposerByDistance(Dictionary<GameObject, int> distances)
		{
			int min = distances.Values.Min();
			return distances.First(x => x.Value == min).Key;
		}


		/// <summary>
		/// Alternate version of Alert<T> that uses my own default that excludes plants. Should provide your own exposer or text, or the target may be picked as the exposer.
		/// </summary>
		public List<GameObject> Alert<T>(GameObject Exposer, string text = defaultAlertText, bool FindExposerFromList = true, Effect effect = null, Effect ExposerEffect = null, bool ShowExposer = true, bool AddOpinionToExposer = true, bool ShowPopup = true, bool WakeSleepers = true, bool AlertPeopleListed = true, bool AddOpinion = true, uint AoE = default) where T : IOpinionSubject, new()
		{
			List<GameObject> MyWitnesses = new(ValidSentients);
			StealthCore Core = new();
			foreach (var witness in MyWitnesses.ToList())
			{
				if (!Core.Plant(witness)) // && !Scan.Unaware(witness, false) need the unaware ones actually, to be woken up!
					MyWitnesses.Remove(witness);
			}
			return Alert<T>(MyWitnesses, text, FindExposerFromList, Exposer, effect, ExposerEffect, ShowExposer, AddOpinionToExposer, ShowPopup, WakeSleepers, AlertPeopleListed, AddOpinion, AoE);
		}
	}
}



