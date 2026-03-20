using System;
using System.Collections.Generic;
using XRL.UI;
using XRL.World.Effects;
using System.Linq;
using VampirismSys.Properties;
using VampirismSys.Extensions;
using VampirismSys.Registry;
using XRL.World.Parts.Mutation;
using VampirismSys.Frenzy;
using VampirismSys.Core;


using SerializeField = UnityEngine.SerializeField;
using AiUnity.NLog.Core.Targets;

namespace XRL.World.Parts
{

	/// <summary>
	/// The eyes and brain of FrenzyAI that scans the environment for targets and initiates Frenzy based on the Vampire's property values.
	/// </summary>
	[Serializable]

	public class TheBeast : IPart
	{

		public Dictionary<GameObject, int> TargetRegistry = new();

		public Vampirism Base => _Base ??= ParentObject.GetPart<Vampirism>();

		public FrenzyCore Core => _Core ??= new FrenzyCore(this);

		[NonSerialized]
		FrenzyCore _Core;

		[NonSerialized]
		Vampirism _Base;

		public bool GameOver;

		public bool Wassail;

		public bool Frenzied;

		public const int FLAG_AVOID = 150; //arbitrary value assigned to targets to prevent them from being re-targetted

		public bool HasFangs() => Base.HasFangs();

		public bool Incap() => ParentObject.Incap(true);

		public bool CantFrenzy()
		{
			return Base.Rotschrek || Frenzied || !HasFangs() || Incap() || ParentObject.CheckFlag(Flags.FEED) || Vampirism.SunlightInterference(ParentObject);
		}
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
				if (!CantFrenzy() && Options.GetOptionBool(VampirismSys.Rules.ModOptions.FRENZY))
					Core.Frenzy();
			}
			if (E.ID == Events.WISH_HUMANITY)
				GameOver = false;
			return base.FireEvent(E);
		}

		public override bool WantEvent(int ID, int cascade)
		{

			if (ID == SingletonEvent<BeginTakeActionEvent>.ID || ID == EnteringZoneEvent.ID)
				return Options.GetOptionBool(VampirismSys.Rules.ModOptions.FRENZY) && ParentObject.IsPlayer();
			return base.WantEvent(ID, cascade);
		}

		public override bool HandleEvent(EnteringZoneEvent E)
		{
			if (GameOver)
			{
				GameObject[] invalids = TargetRegistry.Keys.Where(x => x == null || !x.InSamePartyAs(ParentObject)).ToArray();
				invalids.ForEach(x => TargetRegistry.Remove(x));
				if (TargetRegistry.Count == 0)
					TargetRegistry = new();
			}
			else
				TargetRegistry = new();
			return base.HandleEvent(E);
		}
		public override bool HandleEvent(BeginTakeActionEvent E)
		{
			Core.FrenzyChances();
			if (GameOver && TargetRegistry.Count != 0 && !Frenzied)
				Timer();
			return base.HandleEvent(E);
		}

		/// <summary>
		/// Randomly removes a FLAG_AVOID target from the list so that they may be autoattacked	 again.
		/// </summary>
		void Timer()
		{
			if (WikiRng.Next(1, 100) == 100)
			{
				GameObject guy = TargetRegistry.GetRandomElement();
				AddPlayerMessage("{{R|The Beast}} forgets " + guy.t() + ".");
				TargetRegistry.Remove(guy);
			}
		}
	}

}
