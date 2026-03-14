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
	public class CombatFeed : BaseFeedEffect
	{

		public bool Frenzy
		{
			get=>_frenzy;
			private init
			{
				_frenzy = value;
			}
		}

		bool _frenzy;
		public CombatFeed() : base()
		{
		}
		internal CombatFeed(GameObject other, bool isAttacker, string Damage, int Duration, bool Frenzy, bool Friendly, bool Ghoul, bool vampire) : base(other, isAttacker, Damage, Duration, Ghoul, Friendly, vampire)
		{
			this.Frenzy = Frenzy;
		}

        public override void Write(GameObject Basis, SerializationWriter Writer)
        {
			Writer.Write(_frenzy);
            base.Write(Basis, Writer);
        }

        public override void Read(GameObject Basis, SerializationReader Reader)
        {
			_frenzy = Reader.ReadBoolean();
            base.Read(Basis, Reader);
        }
		public override bool Apply(GameObject Object)
		{
			if (!isAttacker)
				ScaryMonster(Object);
			else if (Frenzy)
				XRLCore.Core.RenderDelay(100);
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
		
		protected override void Attack()
		{
				other?.Object?.TakeDamage(ref Amount, "Bleeding", null, null, base.Object, null, null, null, null, "from bloodloss!");
				AIPassTurn();
		}
	}
}