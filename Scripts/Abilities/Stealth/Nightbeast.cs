using System;
using System.Collections.Generic;
using Nexus.Properties;
using Nexus.Stealth;
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
		/// All objects with part brain in the zone. Avoid writing to this.
		/// </summary>
		[NonSerialized]
		public List<GameObject> Sentients = new();

		/// <summary>
		/// List of verified (see StealthCore.ValidSentient()) Sentients. Companions not included. This list is useful if you want to mess with people who are not nearby or in LOS.
		/// Usually when stealth is broken, only a small portion of people will actually be in LOS. Avoid writing to this.
		/// </summary>
		[NonSerialized]
		public List<GameObject> ValidSentients = new();

		/// <summary>
		/// List of nearby objects in LOS and within the AI_RADIUS. Avoid writing to this.
		/// </summary>

		[NonSerialized]
		public List<GameObject> NearbySentients = new();

		/// <summary>
		/// List of NearbySentients that are aware, can see you(based on lighting) and are (usually) not plants. This is the primary list that is actually
		/// used to evaluate stealth.
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

		/// <summary>
		///  AI generally won't perceive you beyond this range in base-game combat.
		/// </summary>
		[NonSerialized]
		const int DEFAULT_RADIUS = 21;

		[NonSerialized]
		public bool AllowCombatStealth;

		/// <summary>
		/// Allows you to remove objects from the ActiveWitnesses list before stealth evaluation can begin.
		/// </summary>
		[NonSerialized]
		public List<GameObject> Exclusions; //experimental

		/// <summary>
		/// Allows you to add objects to the ActiveWitnesses list before stealth evaluation begins.
		/// </summary>
		[NonSerialized]

		public List<GameObject> Additions; //experimental

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade))
				return ID == SingletonEvent<BeforeTakeActionEvent>.ID;
			return true;
		}
		/// maybe?:
		/// For those who want to evaluate or modify stealth externally, especially on a turn-by-turn basis, it is paramount to do so within the BeforeTakeActionEvent handler, so that they are perfectly synced.
		/// Any "reactions" (such as Run, stealth being broken, setting an effect duration to 0) should also be handled in the same method to avoid a noticeable waiting period. The EndTurnEvent is not recommended.
		public override bool HandleEvent(BeforeTakeActionEvent E)
		{
			if (!AutoAct.IsActive() && ParentObject.IsPlayer())
			{
				if (!ForceExposure)
				{
					bool combat = ParentObject.IsInCombat();
					if (combat && AllowCombatStealth) //experimental thing
					{
						//	foreach (var obj in ActiveWitnesses)
						//		if (obj.GetHostilityTarget() == ParentObject)
						{
							//	obj.Target = null;
							//obj.Brain.Allegiance.Calm = true;
						}
					}
					else if (!combat)
						RunStealthSystem();
					else if (StealthStage1 || StealthStage2)
						HaltStealthSystem("{{R|Spotted!}}");
				}

			}
			return base.HandleEvent(E);
		}

		/// <summary>
		/// Can force the player into a non-stealth state and halt the stealth system entirely. Must be reset afterwards using this method. The lists will retain
		/// the values they had before the system was halted for the duration of halting.
		/// </summary>
		/// <param name="text"></param>
		public void ForceExpose(bool value, string text = null)
		{
			if (ForceExposure = value == true)
				HaltStealthSystem(text);
		}
		void HaltStealthSystem(string text)
		{
			if (!HideMessages && ParentObject.Target is null && text is not null)
				AddPlayerMessage(text);
			ParentObject.SetStringProperty(Flags.STEALTH, Flags.FALSE);
			StealthStage1 = false;
			StealthStage2 = false;
		}
		public void RunStealthSystem()
		{
			Sentients = ParentObject.CurrentZone.GetObjectsWithPart(nameof(Brain));
			new StealthCore(this, base.ParentObject?.CurrentCell?.GetLight(), ConsiderPlantsWitnesses, Exclusions, Additions, ForceStealth).ScanEnvironment();
			new ActiveStealth(this, HideMessages).SetStealth(ActiveWitnesses.Count);
		}

		/// <summary>
		/// This supports the one witness feature: it ensures that there are either no witnesses in sight, or if there is one witness, that you are attacking the one
		/// witness themself.
		/// </summary>
		/// <param name="Target"></param>
		/// <returns></returns>
		public bool ValidateStealthATK(GameObject Target) => (StealthStage1 || StealthStage2) && (ActiveWitnesses.Count == 0 || ActiveWitnesses.Contains(Target));
		//anyone using stealth should use this
		//if you only use StealthStage1, it will create strange scenes where players are able to use stealth attacks on sleeping people
		//while someone is nearby watching, just because they are in a "one witness" state
		//and if you only use StealthStage2, you will break the ability to get stealth attacks on the "one witness"
		//and if you use a general stealth readout that doesnt specify target, you will suffer either one of these bugs

		/// <summary>
		/// A general stealth state readout, It is equivelant to using the property
		/// FLAGS.STEALTH, but is available here incase you already have access to Nightbeast.
		/// </summary>
		public bool InStealthState => StealthStage1 || StealthStage2;
		//this should not be used for overall outcome, it is for skipping "resist attack" saves/checks, you should use BeforeAttackValidate
		//to determine whether or not the attack itself is open or stealth

		//in standard ruleset, the "one witness" is vulnerable to stealth attacks:
		//you should not use BeforeAttackValidate for "resist attack" checks, because the target is irrelevent for those - if Stealthed value is true, it implies you are either
		//attacking the one witness themself, or someone who is asleep in their vicinity

	}
}
