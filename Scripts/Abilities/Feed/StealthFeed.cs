using System;
using XRL.UI;
using XRL.World.AI;
using Nexus.Core;
using XRL.World.Parts;
using System.Collections.Generic;
using System.Linq;
using Nexus.Stealth;
using XRL.World.Parts.Mutation;

namespace XRL.World.Effects
{
	/// <summary>
	/// The silent feeding effect that does not actually "attack" the enemy and listens for stealth broken events from Nightbeast.cs.
	/// </summary>
	[Serializable]
	public class StealthFeed : IFeeding
	{
		public bool ActiveStealth;
		public Nightbeast Stealthpart => _Stealthpart ??= Object.GetPart<Nightbeast>();
		Nightbeast _Stealthpart;
		public StealthFeed() : base()
		{
		}
		public StealthFeed(GameObject other, bool isAttacker, string Damage, int Duration, bool vampire) : base()
		{
			base.other = other.Reference();
			base.isAttacker = isAttacker;
			base.Damage = Damage;
			base.Duration = Duration;
			StealthVersion = true;
			Ghoul = false;
			base.vampire = vampire;

		}

		public override void Remove(GameObject Object)
		{
			if (isAttacker)
				Knockout();
			base.Remove(Object);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			if (isAttacker)
			{
				if (ID == SingletonEvent<BeforeTakeActionEvent>.ID)
					return true;
			}
			else if (ID == AfterDieEvent.ID)
				return true;
			return base.WantEvent(ID, cascade);
		}

		public override bool HandleEvent(AfterDieEvent E)
		{
			if (E?.Killer == null && E.Dying == Object) //stealthfeed doesnt perform a real attack so a death by stealth feed is always a null killer
				KilledEvent.Send(Object, other?.Object); //could cause problems maybe well wait and see
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(BeforeTakeActionEvent E) //synced with nightbeast
		{
				ActiveStealth = Stealthpart.StealthStage2;
				if (!ActiveStealth)
					CaughtInTheAct();
			return base.HandleEvent(E);
		}
		public override bool HandleEvent(EndTurnEvent E)
		{
			if (Security()) //must be evaluated first
			{
				if (isAttacker)
				{
					FeedBroken();
					StealthATK();
				}
				Bloodloss();
			}
			return base.HandleEvent(E);
		}
		void CaughtInTheAct()
		{
			DoAlert(new Alert(Stealthpart, Alert.GiveDefaultList(Stealthpart)));
			if (other?.Object?.MakeSave("Toughness", 13, null, null, "Woke During Feeding") is false)
				other.Object.ApplyEffect(new Terrified(WikiRng.Next(16, 20), base.Object, false, false));
			Duration = 0;
		}

		void DoAlert(Alert alert)
		{
			alert.FindClosestExposerInListExcept(alert.SafeAdd(other));
			alert.RemoveSleepFromWitnesses();
			alert.AddOpinionToWitnessesAndExposer<OpinionDominate>();
			alert.Popup(true, "You are caught in the act of predation by", "You are caught in the act of predation!");
		}
		void StealthATK()
		{
			if (Duration > 0 && Feed())
			{
				AddPlayerMessage(other.Object.t() + " takes {{}}" + Amount + " damage from bloodloss!");
				Strings();
				other.Object.hitpoints -= Amount;
				other?.Object?.ParticleText($"{Amount}", IComponent<GameObject>.ConsequentialColorChar(base.Object, other.Object));
			}
		}
		void Knockout()
		{
			if (ActiveStealth && (other?.Object?.HasHitpoints() is true) && !other.Object.HasEffect<Asleep>())
			{
				other.Object.ApplyEffect(new Asleep(WikiRng.Next(50, 100)));
				if (other.Object.HasEffect<Woozy>())
					other.Object.RemoveEffect<Woozy>();
			}
		}

	}
}
