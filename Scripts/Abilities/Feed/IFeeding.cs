using System;
using XRL.Core;
using XRL.World.Parts;
using Nexus.Properties;
using Nexus.Core;
using XRL.World.Parts.Mutation;
using Nexus.Rules;

namespace XRL.World.Effects
{

	/// <summary>
	/// Base class for feeding that handles events, blood consumption, removal, humanity deduction, and provides premade methods for inheritors to invoke.
	/// </summary>

	[Serializable]
	public abstract class IFeeding : Effect
	{
		public string Damage;
		public GameObjectReference other; // a long time ago, this was life drain
		public bool isAttacker;
		public int Level;
		public int VictimHP => isAttacker ? other.Object.GetHPPercent() : base.Object.GetHPPercent();
		public bool stealthed;
		public IFeeding() => DisplayName = "";
		public IFeeding(GameObject other, bool isAttacker, int Level, string DamagePerRound, int Duration) : this()
		{
			this.Damage = DamagePerRound;
			this.Duration = Duration;
			this.other = other.Reference();
			this.isAttacker = isAttacker;
			this.Level = Level;
		}
		public sealed override string GetDescription() => isAttacker ? "{{R sequence|feeding}}" : "";
		public sealed override string GetDetails() => Damage + " damage per turn.";
		static bool PlayerChoseNotToPuke(Vitae v) => v.Blood >= VITAE.FEED_PUKE_WARN && v.IDontWantToPuke(true);
		bool InvalidActor() => other?.Object?.IsInvalid() ?? true || base.Object is null || !other.Object.InSameZone(base.Object);
		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade) && ID != SingletonEvent<UseEnergyEvent>.ID && ID != TookDamageEvent.ID && ID != AfterDieEvent.ID)
				return ID == SingletonEvent<EndTurnEvent>.ID;
			return true;
		}
		public sealed override bool HandleEvent(AfterDieEvent E)
		{
			if (other.Object.IsPlayer())
			{
				bool friendly = Scan.IsFriendly(E?.Dying, other.Object);
				if (!Scan.ReturnProperty(other.Object, Flags.GO) && UI.Options.GetOptionBool(OPTIONS.HUMANITY) && (Scan.ReturnProperty(E?.Dying, Flags.INNOCENT) || friendly))
					VampireKilled(friendly, E.Dying);
			}
			other.Object.RemoveEffectDescendedFrom<IFeeding>();
			return base.HandleEvent(E);
		}
		public sealed override bool HandleEvent(TookDamageEvent E) // this handler is for the AI - they will not act while feeding, but if attacked, they will react. 
		{
			if (!E.Object.IsPlayer() && base.Object == E.Object && isAttacker)
			{
				if (E.Actor is not null)
				{
					if (E.Actor.IsPlayer())
						AddPlayerMessage("You interrupt " + base.Object.t() + "'s feeding!");
					else
						AddPlayerMessage(E.Actor.t() + " interrupts " + base.Object.t() + "'s feeding!");
				}
				else if (base.Object?.HasHitpoints() is true)
					AddPlayerMessage(base.Object.t() + "'s feeding is interrupted!");
				Duration = 0;
			}
			return base.HandleEvent(E);
		}

		public sealed override bool HandleEvent(UseEnergyEvent E) // this is the thing from sunder mind that ends the effect if you move
		{
			if (E.Actor.IsPlayer() && isAttacker && !E.Passive || (!E.Type?.Contains("Pass") ?? false))
				Duration = 0;
			return base.HandleEvent(E);
		}

		protected bool Security()
		{
			if (InvalidActor())
			{
				Duration = 0;
				return false;
			}
			if (!isAttacker && !other.Object.HasHitpoints())
			{
				other = null;
				Duration = 0;
				return false;
			}
			if (!Scan.Applicable(other.Object) && isAttacker)
			{
				Duration = 0;
				return false;
			}
			return true;

		}
		protected void Strings()
		{
			if (base.Object is not null && other?.Object is not null)
			{
				if (!base.Object.IsPlayer() && !other.Object.IsPlayer())
				{
					IComponent<GameObject>.AddPlayerMessage(base.Object.t() + " {{R sequence|feeds}}" + " on " + other.Object.t() + ".");
				}
				else if (!base.Object.IsPlayer() && other.Object.IsPlayer())
				{
					IComponent<GameObject>.AddPlayerMessage(base.Object.t() + " {{R sequence|feeds}} on you!");
				}
				else if (base.Object.IsPlayer())
				{
					IComponent<GameObject>.AddPlayerMessage(base.Object.t() + " {{R sequence|feed}}" + " on " + other.Object.t() + ".");
				}
			}
		}

		protected void Feed(out int Amount)
		{
			Amount = Damage.RollCached();
			if (base.Object.IsPlayer() && base.Object.IsOriginalPlayerBody())
			{
				Vitae v = base.Object.GetPart<Vitae>();
				if (PlayerChoseNotToPuke(v))
				{
					Duration = 0;
					return; //specific order of operations. pukecheck before drink method.
				}
				v.Drink(true);
			}
			base.Object.Heal(Amount, Message: true, FloatText: true, RandomMinimum: true);

		}
		protected void Bloodloss()
		{
			if (!isAttacker && base.Object is not null)
			{
				if (VictimHP <= 75 && VictimHP > 50 && !base.Object.HasEffect<Woozy>() && !base.Object.HasEffect<Asleep>())
					base.Object.ApplyEffect(new Woozy(9999, 5));
				if (VictimHP <= 50 && !base.Object.HasEffect<Pale>())
					base.Object.ApplyEffect(new Pale(9999));
				if (VictimHP <= 25 && !base.Object.HasEffect<KO>() && !base.Object.HasEffect<Asleep>() && !stealthed) //stealth victims get put to sleep on feed end
					base.Object.ApplyEffect(new KO(9999));                                                          //dont want to stack two effects of the same type literally
			}
		}

		protected void FeedBroken()
		{
			if (Scan.Incap(base.Object, false))
				Duration = 0;
		}

		void VampireKilled(bool friendly, GameObject Dying)
		{
			PlayHumanityMessages(friendly);
			Dying.SetStringProperty(Flags.DEAD, null); //checking for if they have Hitpoints in Remove() did not work. causes a humanity loss dupe bug because victim = true on death.
			other.Object.GetPart<Humanity>().VampireKilled();
		}
		void PlayHumanityMessages(bool friendly)
		{
			if (other.Object.IsOriginalPlayerBody())
			{
				if (!friendly)
					AddPlayerMessage("For draining an innocent to death, you lose humanity.");
				else
					AddPlayerMessage("For draining your companion to death, you lose humanity.");
			}
			else if (!friendly)
				AddPlayerMessage("For draining an innocent to death, " + other.Object.t() + " loses humanity,");
			else
				AddPlayerMessage("For draining their companion to death, " + other.Object.t() + " loses humanity.");
		}

		public override bool Apply(GameObject Object)
		{
			if (isAttacker)
				base.Object.SetStringProperty(Flags.FEED, Flags.TRUE);
			return true;
		}
		public override void Remove(GameObject Object)
		{
			if (isAttacker)
			{
				CleanUpAndFinish();
				if (base.Object is not null && base.Object.TryGetPart<Vampirism>(out Vampirism v))
					MakeFangsBloody(v);
			}
			else if (base.Object?.HasEffect<Vampires_Kiss>() is true)
				base.Object.RemoveEffect<Vampires_Kiss>();
		}

		void MakeFangsBloody(Vampirism v)
		{
			v.FangsObject.DisplayName = "{{r|bloody}} fangs";
			v.bloodycounter = 1;
			base.Object.SetStringProperty(Flags.FEED, Flags.FALSE);
		}

		void CleanUpAndFinish()
		{
			EndingStrings();
			MarkVictim();
			if (other?.Object?.HasEffect<Vampires_Kiss>() is true)
				other.Object.RemoveEffect<Vampires_Kiss>();
			if (other?.Object?.HasEffectDescendedFrom<IFeeding>() is true)
				other.Object.RemoveEffectDescendedFrom<IFeeding>();
		}
		void EndingStrings()
		{
			if (base.Object?.IsPlayer() is true && isAttacker)
			{
				if (other?.Object is not null)
					IComponent<GameObject>.AddPlayerMessage("You release " + other.Object.t() + "'s neck.");
				else
					AddPlayerMessage("You release your victim's neck.");
			}
			else if (base.Object?.IsPlayer() is true && !isAttacker)
			{
				if (other?.Object?.HasHitpoints() is true)
					IComponent<GameObject>.AddPlayerMessage(other.Object.t() + " releases your neck");
				else if (other?.Object is not null)
					AddPlayerMessage(other.Object.t() + " 's grip on your neck goes slack.");
				else
					AddPlayerMessage("Your neck is released.");
			}
			else if (base.Object?.HasHitpoints() is true && isAttacker && !base.Object.IsPlayer())
			{
				if (other.Object is not null)
					IComponent<GameObject>.AddPlayerMessage(base.Object.t() + " releases " + other.Object.t() + "'s neck");
			}

		}
		void MarkVictim()
		{
			if (base.Object.IsOriginalPlayerBody()) //if the player ever encounters an AI vampire they can go crazy without fear of losing any humanity themselves during feeding
			{                                       //but only feeding, anything else tracks back to the original player's humanity score
				if (Scan.ReturnProperty(other?.Object, Flags.INNOCENT)) //ALSO prevents a potential bug where you would lose humanity for killing someone
					other.Object.SetLongProperty(Flags.VICTIM, The.Game.Turns); //that was fed on, technically, by another vampire (even if you were dominating them)
				else if (Scan.IsFriendly(other?.Object, base.Object))
					other.Object.SetLongProperty(Flags.VICTIM_HOSTILE, The.Game.Turns);
			}

		}
		public sealed override bool UseStandardDurationCountdown()
		{
			return true;
		}

		public sealed override bool SameAs(Effect e)
		{
			return false;
		}

		public sealed override bool Render(RenderEvent E)
		{
			if (!isAttacker)
			{
				int num = XRLCore.CurrentFrame % 60;
				if (num > 25 && num < 35)
				{
					E.Tile = null;
					E.RenderString = "\u0003";
					E.ColorString = "&R^k";
				}
			}
			return true;
		}
	}
}
