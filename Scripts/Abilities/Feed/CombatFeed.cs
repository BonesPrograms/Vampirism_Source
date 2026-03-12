using System;
using XRL.Core;
using VampirismSys.Properties;
using VampirismSys.Core;
using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Effects
{

	/// <summary>
	/// The "loud" feeding effect used in combat, on companions, or when stealth is invalid.
	/// </summary>
	[Serializable]
	public class CombatFeed : IFeeding
	{

		public bool Frenzy;
		public CombatFeed() : base()
		{
		}
		public CombatFeed(GameObject other, bool isAttacker, string Damage, int Duration, bool Frenzy, bool Friendly, bool Ghoul, bool vampire) : base(other, isAttacker, Damage, Duration, Ghoul, Friendly, vampire)
		{
			this.Frenzy = Frenzy;
		}
		public override bool HandleEvent(EndTurnEvent E)
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
			return base.HandleEvent(E);
		}

		public override bool Apply(GameObject Object)
		{
			if (isAttacker)
			{
				if (Frenzy) //bug when feed is activated that causes frenzy passturn to halt
				{
					XRLCore.Core.RenderDelay(100);
					Object.PassTurn();
				}
			}
			else
				ScaryMonster(Object);
			return base.Apply(Object);
		}

		void ScaryMonster(GameObject Object)
		{
			if (!base.friendly && !Object.MakeSave("Toughness", 13, null, null, "Scary Vampire Attack"))
				Object.ApplyEffect(new Terrified(WikiRng.Next(16, 20), other.Object, false, false));
		}


		void AIPassTurn()
		{
			if (!base.Object?.IsPlayer() ?? false)
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
			if (Feed())
				other?.Object?.TakeDamage(ref Amount, "Bleeding", null, null, base.Object, null, null, null, null, "from bloodloss!");
		}
	}
}