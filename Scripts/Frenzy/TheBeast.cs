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
		public bool HasFangs() => Base.HasFangs();
		public bool Incap() => ParentObject.Incap(true);
		FrenzyCore _Core;
		public FrenzyCore Core => _Core ??= new FrenzyCore(this, new Search(this));
		Vampirism _Base;
		public Vampirism Base => _Base ??= ParentObject.GetPart<Vampirism>();
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
				if (HasFangs() && Options.GetOptionBool(Nexus.Rules.OPTIONS.FRENZY))
					Core.Frenzy();
			}
			if (E.ID == Events.WISH_HUMANITY)
				GameOver = false;
			return base.FireEvent(E);
		}


		public override bool WantEvent(int ID, int cascade)
		{
			if (Options.GetOptionBool(Nexus.Rules.OPTIONS.FRENZY) && ParentObject.IsPlayer() && ID == SingletonEvent<BeginTakeActionEvent>.ID)
				return true;
			return base.WantEvent(ID, cascade);
		}
		public override bool HandleEvent(BeginTakeActionEvent E)
		{
			Clean();
			if (!ParentObject.CheckFlag(FLAGS.FEED) && !frenzied && !Incap() && HasFangs())
				Core.FrenzyChances();
			if (GameOver)
				Timer();
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

		public void Clean()
		{
			foreach (GameObject obj in TargetRegistry.KeyArray())
				if (obj?.CurrentCell?.GetCombatTarget(ParentObject) is null || !obj.HasHitpoints() || !obj.InSameZone(ParentObject))
					TargetRegistry.Remove(obj);
		}
	}

}
