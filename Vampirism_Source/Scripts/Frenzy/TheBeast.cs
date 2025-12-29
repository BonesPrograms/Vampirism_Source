using System;
using System.Collections.Generic;
using XRL.UI;
using XRL.World.Effects;
using System.Linq;
using Nexus.Properties;
using Nexus.Core;
using Nexus.Registry;
using XRL.World.Parts.Mutation;
using Nexus.Frenzy;

namespace XRL.World.Parts
{

	/// <summary>
	/// The eyes and brain of FrenzyAI that scans the environment for targets and initiates Frenzy based on the Vampire's property values.
	/// </summary>
	[Serializable]

	public class TheBeast : IPart
	{
		public Dictionary<GameObject, int> TargetRegistry = new();
		public bool GameOver;
		public bool Wassail;
		public bool frenzied; //to prevent stacked frenzying effects 
		public const int FLAG_AVOID = 150; //arbitrary value assigned to targets to prevent them from being re-targetted

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register(Events.GAMEOVER);
			Registrar.Register(Events.WISH_HUMANITY);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == Events.GAMEOVER && ParentObject.IsPlayer())
			{
				GameOver = true;
				if (ParentObject.GetPart<Vampirism>().HasFangs() && Options.GetOptionBool(Nexus.Rules.OPTIONS.FRENZY))
					new FrenzyCore(this).Frenzy();
			}
			if (E.ID == Events.WISH_HUMANITY)
				GameOver = false;
			return base.FireEvent(E);
		}


		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade))
				return ID == SingletonEvent<BeginTakeActionEvent>.ID;
			return true;
		}
		public override bool HandleEvent(BeginTakeActionEvent E)
		{
			if (Options.GetOptionBool(Nexus.Rules.OPTIONS.FRENZY) && ParentObject.IsPlayer()) //actually prevents you from frenzying if dominating
			{
					Clean();
				if (!Scan.SafeReturnProperty(ParentObject, Flags.FEED) && !frenzied  && ParentObject.GetPart<Vampirism>().HasFangs() && !Scan.Incap(ParentObject, true))
					new FrenzyCore(this).FrenzyChances();
				if (GameOver)
					Timer();
			}
			return base.HandleEvent(E);
		}

		/// <summary>
		/// Randomly removes a FLAG_AVOID target from the list so that they may be autoattacked	 again.
		/// </summary>
		void Timer()
		{
			if (TargetRegistry.Count != 0 && !frenzied)
			{
				if (WikiRng.Next(1, 50) == 50)
				{
					GameObject Key = TargetRegistry.GetRandomElement();
					AddPlayerMessage("{{R|The Beast}} forgets " + Key.t() + ".");
					TargetRegistry.Remove(Key);
				}
			}

		}

		void Clean()
		{
			foreach (GameObject obj in TargetRegistry.Keys.ToList())
				if (obj?.CurrentCell?.GetCombatTarget(ParentObject) is null || !obj.HasHitpoints() || !obj.InSameZone(ParentObject))
					TargetRegistry.Remove(obj);


		}
	}

}
