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

		[NonSerialized]
		public Dictionary<GameObject, bool> Witnesses = new();

		[NonSerialized]
		public int TrueCount;
		public StealthCore Core => _Core ??= new StealthCore(this);
		public ActiveStealth ActiveStealth => _ActiveStealth ??= new ActiveStealth(this);
		public Zone Zone => ParentObject.CurrentZone;
		StealthCore _Core;
		ActiveStealth _ActiveStealth;

		/// <summary>
		/// Stage one means that there is only one witness.
		/// </summary>

		public bool StealthStage1;

		/// <summary>
		/// Stage two means there are no witnesses.
		/// </summary>
		public bool StealthStage2;
		public override bool WantEvent(int ID, int cascade)
		{
			if (!AutoAct.IsActive() && ParentObject.IsPlayer() && ID == SingletonEvent<BeforeTakeActionEvent>.ID)
				return true;
			if (ID == EnteringZoneEvent.ID)
				return true;
			return base.WantEvent(ID, cascade);
		}
		/// maybe?:
		/// For those who want to evaluate or modify stealth externally, especially on a turn-by-turn basis, it is paramount to do so within the BeforeTakeActionEvent handler, so that they are perfectly synced.
		/// Any "reactions" (such as Run, stealth being broken, setting an effect duration to 0) should also be handled in the same method to avoid a noticeable waiting period. The EndTurnEvent is not recommended.
		/// 

		public override bool HandleEvent(EnteringZoneEvent E)
		{
			Witnesses = new();
			return base.HandleEvent(E);
		}
		public override bool HandleEvent(BeforeTakeActionEvent E)
		{
			if (!ParentObject.IsInCombat())
				RunStealthSystem();
			else if (StealthStage1 || StealthStage2)
				HaltStealthSystem("{{R|Spotted!}}");
			return base.HandleEvent(E);
		}

		void HaltStealthSystem(string text)
		{
			if (ParentObject.Target != null)
				AddPlayerMessage(text);
			ParentObject.SetStringProperty(FLAGS.STEALTH, FLAGS.FALSE);
			StealthStage1 = false;
			StealthStage2 = false;
		}
		void RunStealthSystem()
		{
			TrueCount = default;
			Core.LightLevel = ParentObject.CurrentCell?.GetLight();
			Core.ScanEnvironment();
			ActiveStealth.SetStealth(TrueCount);
		}

		/// <summary>
		/// This supports the one witness feature: it ensures that there are either no witnesses in sight, or if there is one witness, that you are attacking the one
		/// witness themself.
		/// </summary>
		/// <param name="Target"></param>
		/// <returns></returns>
		public bool ValidateStealthATK(GameObject Target) => (StealthStage1 || StealthStage2) && (Witnesses.Count == 0 || Witnesses.ContainsKey(Target));

	}
}
