using System;
using XRL.Core;
using Nexus.Properties;
using Nexus.Core;

namespace XRL.World.Effects
{

	/// <summary>
	/// The "loud" feeding effect used in combat, on companions, or when stealth is invalid.
	/// </summary>
	[Serializable]
	public class CombatFeed : IFeeding
	{

		public CombatFeed() : base()
		{
		}
		public CombatFeed(GameObject other, bool isAttacker, int Level, string DamagePerRound, int Duration) : base(other, isAttacker, Level, DamagePerRound, Duration)
		{
		}
		public override bool HandleEvent(EndTurnEvent E)
		{
			Mainline();
			return base.HandleEvent(E);
		}

		void Mainline()
		{
			if (Security())
			{
				if (isAttacker)
				{
					FeedBroken();
					Attack();
					AIPassTurn();
				}
				Bloodloss();
			}

		}

		public override bool Apply(GameObject Object)
		{
			if (isAttacker)
			{
				if (Scan.SafeReturnProperty(base.Object, Flags.FRENZY)) //bug when feed is activated that causes frenzy passturn to halt
				{
					XRLCore.Core.RenderDelay(100);
					base.Object.PassTurn();
				}
				ScaryMonster();
			}
			return base.Apply(Object);
		}

		void ScaryMonster()
		{
			if (!Scan.IsFriendly(other.Object, base.Object) && !other.Object.MakeSave("Toughness", 13, null, null, "Scary Vampire Attack"))
				other.Object.ApplyEffect(new Terrified(WikiRng.Next(16, 20), base.Object, false, false));
		}


		void AIPassTurn()
		{
			if (base.Object?.IsPlayer() is false)
				base.Object.PassTurn();
		}
		void Attack()
		{
			if (Duration > 0)
			{
				base.Object.PlayWorldSound("Sounds/StatusEffects/sfx_statusEffect_life_drain");
				Strings();
				CombatFeeding();
			}
		}

		void CombatFeeding()
		{
			Feed(out int Amount);
			other?.Object?.TakeDamage(ref Amount, "Bleeding", null, null, base.Object, null, null, null, null, "from bloodloss!");
		}
	}
}