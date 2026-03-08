using System;
using System.Collections.Generic;
using System.Linq;
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
		public static Dictionary<GameObject, bool> Witnesses => _witnesses;

		[GameBasedStaticCache]
		public static bool NeedsReactivate = false; //for gamestart

		[GameBasedStaticCache(false)]
		static Dictionary<GameObject, bool> _witnesses;

		//this was throwing nullref errors in Stealth() during gamestart if i didnt create an instance of it prematurely. will need to do some more research as to why late
		public static bool StealthStage1 => ActiveStealth.StealthStage1;
		public static bool StealthStage2 => ActiveStealth.StealthStage2;

		//either/or means stealth ATK is valid
		public static bool Stealthed => StealthStage1 || StealthStage2;
		public override bool WantEvent(int ID, int cascade)
		{
			if (ID == AfterPlayerBodyChangeEvent.ID)
				return true;
			if (ID == SingletonEvent<BeforeTakeActionEvent>.ID)
				return !AutoAct.IsActive() && ParentObject.IsPlayer();
			if (ID == EnteringZoneEvent.ID || ID == AfterGameLoadedEvent.ID)
				return ParentObject.IsPlayer();
			return base.WantEvent(ID, cascade);
		}

		public override bool HandleEvent(AfterPlayerBodyChangeEvent E)
		{
			Reactivate();
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
		}

		static void Reactivate(Zone zone) //system relies on pinging the zone on load (or receiving new objects when one is created) and then strictly sifts through its own dictionary from then on for evaluation
		{
			_witnesses = new();
			StealthCore.LightLevel = The.Player.CurrentCell?.GetLight();
			StealthCore.ScanEnvironment(zone);
			NeedsReactivate = false;
		}

		static void HaltStealthSystem(string text)
		{
			if (The.Player.Target != null)
				AddPlayerMessage(text);
			The.Player.SetStringProperty(Flags.STEALTH, Flags.FALSE);
			ActiveStealth.Halt();
		}
		static void RunStealthSystem()
		{
			StealthCore.LightLevel = The.Player.CurrentCell?.GetLight();
			StealthCore.Stealth();
			ActiveStealth.SetStealth();
		}
	}
}
