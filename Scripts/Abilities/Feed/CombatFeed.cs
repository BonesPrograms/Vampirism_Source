using System;
using XRL.Core;
using VampirismSys.Extensions;
using VampirismSys.Core;

namespace XRL.World.Effects
{

	/// <summary>
	/// The "loud" feeding effect used in combat, on companions, or when stealth is invalid.
	/// </summary>
	[Serializable]
	public class CombatFeed : BaseFeedEffect
	{
		bool Frenzy;
		public CombatFeed() : base()
		{
		}
		public CombatFeed(GameObject other, bool isAttacker, string Damage, bool Frenzy, bool Friendly, bool Ghoul, bool vampire) : base(other, isAttacker, Damage, Ghoul, Friendly, vampire)
		{
			this.Frenzy = Frenzy;
		}


		public override bool Apply(GameObject Object)
		{
			if (!IsAttacker)
				ScaryMonster(Object);
			else if (Frenzy)
				XRLCore.Core.RenderDelay(100);
			return base.Apply(Object);
		}

		void ScaryMonster(GameObject Object)
		{
			if (!base.IsFriendly && !Object.MakeSave("Toughness", 13, null, null, "Scary Vampire Attack"))
				Object.ApplyEffect(new Terrified(WikiRng.Next(16, 20), Other, false, false));
		}


		void AIPassTurn()
		{
			if (!base.Object?.IsPlayer() ?? false)
				base.Object.PassTurn();
		}

		protected override void Attack()
		{
			Other?.TakeDamage(ref Amount, "Bleeding", null, null, base.Object, null, null, null, null, "from bloodloss!");
			AIPassTurn();
		}
	}
}