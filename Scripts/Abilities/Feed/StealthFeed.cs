using System;
using XRL.UI;
using XRL.World.AI;
using VampirismSys.Core;
using XRL.World.Parts;
using System.Collections.Generic;
using System.Linq;
using VampirismSys.Stealth;
using XRL.World.Parts.Mutation;
using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Effects
{
	/// <summary>
	/// The silent feeding effect that does not actually "attack" the enemy and listens for stealth broken events from Nightbeast.cs.
	/// </summary>
	[Serializable]
	public class StealthFeed : BaseFeedEffect
	{
		bool ActiveStealth;
		public StealthFeed() : base()
		{
		}
		internal StealthFeed(GameObject other, bool isAttacker, string Damage, int Duration, bool vampire) : base()
		{
			base.other = other.Reference();
			base.isAttacker = isAttacker;
			base.Damage = Damage;
			base.Duration = Duration;
			Ghoul = false;
			base.vampire = vampire;

		}

        public override void Write(GameObject Basis, SerializationWriter Writer)
        {
			Writer.Write(ActiveStealth);
            base.Write(Basis, Writer);
        }

        public override void Read(GameObject Basis, SerializationReader Reader)
        {
			ActiveStealth = Reader.ReadBoolean();
            base.Read(Basis, Reader);
        }

		public override void Remove(GameObject Object)
		{
			if (isAttacker)
				Knockout();
			base.Remove(Object);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			if (ID == SingletonEvent<BeforeTakeActionEvent>.ID)
				return isAttacker;
			if (ID == AfterDieEvent.ID)
				return !isAttacker;
			return base.WantEvent(ID, cascade);
		}

		public override bool HandleEvent(AfterDieEvent E)
		{
			if (E.Killer == null && E.Dying == Object && other?.Object != null) //stealthfeed doesnt perform a real attack so a death by stealth feed is always a null killer
				KilledEvent.Send(Object, other.Object); //could cause problems maybe well wait and see
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(BeforeTakeActionEvent E) //synced with nightbeast
		{
			ActiveStealth = Nightbeast.StealthStage2;
			if (!ActiveStealth)
				CaughtInTheAct();
			return base.HandleEvent(E);
		}
		void CaughtInTheAct()
		{
			DoAlert(new Alert(Object));
			if (other?.Object?.MakeSave("Toughness", 13, null, null, "Woke During Feeding") is false)
				other.Object.ApplyEffect(new Terrified(WikiRng.Next(16, 20), base.Object, false, false));
			Duration = 0;
		}

		void DoAlert(Alert alert)
		{
			alert.FindClosestExposerInListExcept(alert.Add(other));
			alert.RemoveSleepFromWitnesses();
			alert.AddOpinionToWitnessesAndExposer<OpinionDominate>();
			alert.Popup(true, "You are caught in the act of predation by", "You are caught in the act of predation!");
		}
		protected override void Attack()
		{
			AddPlayerMessage(other.Object.t() + " takes {{}}" + Amount + " damage from bloodloss!");
			other.Object.hitpoints -= Amount;
			other?.Object?.ParticleText($"{Amount}", IComponent<GameObject>.ConsequentialColorChar(base.Object, other.Object));
		}
		void Knockout()
		{
			if (ActiveStealth && (other?.Object?.HasHitpoints() is true))
			{
				other.Object.ApplyEffect(new Asleep(WikiRng.Next(50, 100)));
				other.Object.RemoveEffect<Woozy>();
			}
		}

	}
}
