using System;
using XRL.World.AI;
using VampirismSys.Extensions;
using XRL.World.Parts;
using VampirismSys.Stealth;

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
		internal StealthFeed(GameObject other, bool isAttacker, string Damage, bool vampire) : base(other)
		{
			IsAttacker = isAttacker;
			base.Damage = Damage;
			IsGhoul = false;
			IsFriendly = false;	
			IsVampire = vampire;

		}



		public override void Remove(GameObject Object)
		{
			if (IsAttacker)
				Knockout();
			base.Remove(Object);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			if (ID == SingletonEvent<BeforeTakeActionEvent>.ID)
				return IsAttacker;
			if (ID == AfterDieEvent.ID)
				return !IsAttacker;
			return base.WantEvent(ID, cascade);
		}

		public override bool HandleEvent(AfterDieEvent E)
		{
			if (E.Killer == null && E.Dying == Object && Other != null) //stealthfeed doesnt perform a real attack so a death by stealth feed is always a null killer
				KilledEvent.Send(Object, Other); //could cause problems maybe well wait and see
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
			if (Other?.MakeSave("Toughness", 13, null, null, "Woke During Feeding") is false)
				Other.ApplyEffect(new Terrified(WikiRng.Next(16, 20), base.Object, false, false));
			Duration = 0;
		}

		void DoAlert(Alert alert)
		{
			alert.FindClosestExposerInListExcept(alert.Add(Other));
			alert.RemoveSleepFromWitnesses();
			alert.AddOpinionToWitnessesAndExposer<OpinionDominate>();
			alert.Popup(true, "You are caught in the act of predation by", "You are caught in the act of predation!");
		}
		protected override void Attack()
		{
			AddPlayerMessage(Other.t() + " takes {{}}" + Amount + " damage from bloodloss!");
			Other.hitpoints -= Amount;
			Other?.ParticleText($"{Amount}", IComponent<GameObject>.ConsequentialColorChar(base.Object, Other));
		}
		void Knockout()
		{
			if (ActiveStealth && (Other?.HasHitpoints() is true))
			{
				Other.ApplyEffect(new Asleep(WikiRng.Next(50, 100)));
				Other.RemoveEffect<Woozy>();
			}
		}
	}
}
