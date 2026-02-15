using System;
using System.Collections.Generic;
using Nexus.Core;
using Nexus.Properties;
using Nexus.Stealth;
using XRL.World.Capabilities;


//to avoid enumeration issues, witnesses are converted to a key array after the zone is scanned
//our WitnessCreatedListener adds witnesses to the dictionary if they are valid, which will create an enumeration error in StealthCore.Stealth
//important to note: stealth only runs if parentobject.isplayer() (see WantEvent) so you dont need to worry about NPC vampires modifying the list, they are not even receiving the beforetakeaction event

//recently the system has been changed to be 99% static
//only the player experiences stealth so it doesnt really need to be instance data based at all
//player can dominate any vampire object and the system will shift to the new object
//because the system only ever looks for The.Player
//but the Nightbeast part will not run stealth if it's parentobject isnt the player
//having this part, at this point, little more than a tag saying "I am a vampire, run stealth if I am the player" (because thats all this is for)

namespace XRL.World.Parts
{

	/// <summary>
	/// The stealth system for vampirism that enables stealth feeding and introduces witnesses.	
	/// </summary>
	[Serializable]

	[HasGameBasedStaticCache]
	public class Nightbeast : IPart
	{

		[GameBasedStaticCache]
		public static bool NeedsReactivate = false; //for gamestart

		[GameBasedStaticCache(false)]
		public static Dictionary<GameObject, bool> Witnesses;

		[GameBasedStaticCache(false, true)]
		public static GameObject[] KeyArray = new GameObject[0]; //this was throwing nullref errors in Stealth() during gamestart if i didnt create an instance of it prematurely. will need to do some more research as to why later

		[GameBasedStaticCache]
		public static int TrueCount = 0;

		/// <summary>
		/// Stage one means that there is only one witness.
		/// </summary>
		/// 
		[GameBasedStaticCache]
		public static bool StealthStage1 = default;

		/// <summary>
		/// Stage two means there are no witnesses.
		/// </summary>
		/// 
		[GameBasedStaticCache]
		public static bool StealthStage2 = default;

		//either/or means stealth ATK is valid
		public static bool Stealthed => StealthStage1 || StealthStage2;

		public override bool WantEvent(int ID, int cascade)
		{
			if (ID == AfterPlayerBodyChangeEvent.ID)
				return true;
			if (ParentObject.IsPlayer())
			{
				if (!AutoAct.IsActive() && ID == SingletonEvent<BeforeTakeActionEvent>.ID)
					return true;
				if (ID == EnteringZoneEvent.ID)
					return true;
				if (ID == AfterGameLoadedEvent.ID)
					return true;
			}
			return base.WantEvent(ID, cascade);
		}

		public override bool HandleEvent(AfterPlayerBodyChangeEvent E)
		{
			NeedsReactivate = true;
			return base.HandleEvent(E);
		}
		public override bool HandleEvent(AfterGameLoadedEvent E)
		{
			Reactivate();
			return base.HandleEvent(E);
		}
		public override bool HandleEvent(EnteringZoneEvent E)
		{
			Reactivate(E.Cell.ParentZone);
			return base.HandleEvent(E);
		}
		public override bool HandleEvent(BeforeTakeActionEvent E)
		{
			if (NeedsReactivate)
				Reactivate();
			if (!ParentObject.IsInCombat())
				RunStealthSystem();
			else if (StealthStage1 || StealthStage2)
				HaltStealthSystem("{{R|Spotted!}}");
			return base.HandleEvent(E);
		}

		static void Reactivate()
		{
			Reactivate(The.Player.CurrentZone);
			NeedsReactivate = false;
		}

		static void Reactivate(Zone zone) //system relies on pinging the zone on load (or receiving new objects when one is created) and then strictly sifts through its own dictionary from then on for evaluation
		{
			Witnesses = new();
			StealthCore.Zone = zone;
			StealthCore.LightLevel = The.Player.CurrentCell?.GetLight();
			StealthCore.ScanEnvironment();
			KeyArray = Witnesses.KeyArray();
		}

		static void HaltStealthSystem(string text)
		{
			if (The.Player.Target != null)
				AddPlayerMessage(text);
			The.Player.SetStringProperty(FLAGS.STEALTH, FLAGS.FALSE);
			StealthStage1 = false;
			StealthStage2 = false;
		}
		static void RunStealthSystem()
		{
			TrueCount = default;
			StealthCore.LightLevel = The.Player.CurrentCell?.GetLight();
			StealthCore.Stealth();
			ActiveStealth.SetStealth();
		}
	}
}
