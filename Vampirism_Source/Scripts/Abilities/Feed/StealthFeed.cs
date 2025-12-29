using System;
using XRL.UI;
using XRL.World.AI;
using Nexus.Core;
using XRL.World.Parts;
using System.Collections.Generic;
using System.Linq;

namespace XRL.World.Effects
{
	/// <summary>
	/// The silent feeding effect that does not actually "attack" the enemy and listens for stealth broken events from Nightbeast.cs.
	/// </summary>
	[Serializable]
	public class StealthFeed : IFeeding
	{
		public bool stealthy;
		public StealthFeed() : base()
		{
		}
		public StealthFeed(GameObject other, bool isAttacker, int Level, string DamagePerRound, int Duration) : base(other, isAttacker, Level, DamagePerRound, Duration)
		{
			stealthed = true;
		}
		public override void Remove(GameObject Object)
		{
			if (isAttacker)
				Knockout();
			base.Remove(Object);
		}

		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade))
				return ID == SingletonEvent<BeforeTakeActionEvent>.ID;
			return base.WantEvent(ID, cascade);
		}

		public override bool HandleEvent(BeforeTakeActionEvent E) //synced with nightbeast
		{
			StealthChecks();
			return base.HandleEvent(E);
		}
		public override bool HandleEvent(EndTurnEvent E)
		{
			Mainline();
			return base.HandleEvent(E);
		}

		void StealthChecks()
		{
			if (isAttacker)
			{
				Nightbeast n = base.Object.GetPart<Nightbeast>();
				stealthy = n.StealthStage2;
				if (!stealthy)
					CaughtInTheAct(n);
			}
		}

		void CaughtInTheAct(Nightbeast n)
		{
			string message = "You are caught in the act of predation!";
			if (n.StealthStage1)
				message = $"You are caught in the act of predation by {n.ActiveWitnesses[0]}!";
			n.Alert<OpinionDominate>(Exposer: null, message, false);
			Scare();
			Duration = 0;
		}

		void Mainline()
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

		}
		void StealthATK()
		{
			Feed(out int Amount);
			AddPlayerMessage(other.Object.t() + " takes {{}}" + Amount + " damage from bloodloss!");
			Strings();
			other.Object.hitpoints -= Amount;
			other?.Object?.ParticleText($"{Amount}", IComponent<GameObject>.ConsequentialColorChar(base.Object, other.Object));
		}

		void Scare()
		{
			if (other?.Object?.HasEffect<Asleep>() is true)
				other.Object.RemoveEffect<Asleep>();
			if (other?.Object?.MakeSave("Toughness", 13, null, null, "Woke During Feeding") is false)
				other.Object.ApplyEffect(new Terrified(WikiRng.Next(16, 20), base.Object, false, false));
			other?.Object?.AddOpinion<OpinionDominate>(base.Object);
		}
		void Knockout()
		{
			if (stealthy && (other?.Object?.HasHitpoints() is true) && !other.Object.HasEffect<Asleep>())
			{
				other.Object.ApplyEffect(new Asleep(WikiRng.Next(50, 100)));
				if (other.Object.HasEffect<Woozy>())
					other.Object.RemoveEffect<Woozy>();
			}
		}

	}
}
