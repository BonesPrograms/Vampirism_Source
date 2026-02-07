using System;
using XRL.Core;
using XRL.World.Parts;
using Nexus.Properties;
using Nexus.Core;
using XRL.World.Parts.Mutation;
using Nexus.Rules;
using Nexus.Spells;

namespace XRL.World.Effects
{

	/// <summary>
	/// Base class for feeding that handles events, blood consumption, removal, humanity deduction, and provides premade methods for inheritors to invoke.
	/// </summary>

	[Serializable]
	public abstract class IFeeding : Effect
	{
		Vitae _Vitae;
		Vitae Vitae => _Vitae ??= Object.GetPart<Vitae>();
		public string Damage;
		public GameObjectReference other; // a long time ago, this was life drain
		public bool isAttacker;
		public int VictimHP => isAttacker ? other.Object.GetHPPercent() : base.Object.GetHPPercent();
		public bool StealthVersion;
		public bool Ghoul;
		protected int Amount;
		public bool friendly;
		public IFeeding() => DisplayName = "";
		public bool vampire;
		public IFeeding(GameObject other, bool isAttacker, string DamagePerRound, int Duration, bool Ghoul, bool friendly, bool vampire) : this()
		{
			this.Damage = DamagePerRound;
			this.Duration = Duration;
			this.other = other.Reference();
			this.isAttacker = isAttacker;
			this.Ghoul = Ghoul;
			this.friendly = friendly;
			this.vampire = vampire;
		}
		public sealed override string GetDescription() => isAttacker ? "{{R sequence|feeding}}" : "";
		public sealed override string GetDetails() => Damage + " damage per turn.";
		bool InvalidActor() => other?.Object?.IsInvalid() ?? true || base.Object == null || !other.Object.InSameZone(base.Object);
		public override bool WantEvent(int ID, int cascade)
		{
			if (isAttacker)
			{
				if (ID == KilledEvent.ID)
					return true;
				if (Object.IsPlayer())
				{
					if (ID == SingletonEvent<UseEnergyEvent>.ID)
						return true;
				}
				else if (ID == TookDamageEvent.ID)
					return true;
			}
			if (ID == SingletonEvent<EndTurnEvent>.ID)
				return true;
			return base.WantEvent(ID, cascade);
		}
		public sealed override bool HandleEvent(KilledEvent E) //cannot be KilledEvent because StealthFeed does not count as an actual kill for the feeder
		{
			if (UI.Options.GetOptionBool(OPTIONS.HUMANITY) && E?.Killer == Object && E?.Dying != null && E.Dying == other?.Object)
			{
				if (!vampire && !Object.CheckFlag(FLAGS.GO) && (friendly || other.Object.CheckFlag(FLAGS.INNOCENT)))
					if (UI.Options.GetOptionBool(Nexus.Rules.OPTIONS.DOUG) && friendly && !other.Object.IsGhoulOf(Object) && !other.Object.IsBeguiledBy(Object))
						return base.HandleEvent(E);
					else
						VampireKilled();
			}
			if (vampire)
				Buff();
			Duration = 0;
			return base.HandleEvent(E);
		}
		public sealed override bool HandleEvent(TookDamageEvent E) // this handler is for the AI - they will not act while feeding, but if attacked, they will react. 
		{
			if (base.Object == E.Object)
			{
				if (E.Actor != null)
				{
					if (E.Actor.IsPlayer())
						AddPlayerMessage("You interrupt " + base.Object.t() + "'s feeding!");
					else
						AddPlayerMessage(E.Actor.t() + " interrupts " + base.Object.t() + "'s feeding!");
				}
				else if (base.Object?.HasHitpoints() ?? false)
					AddPlayerMessage(base.Object.t() + "'s feeding is interrupted!");
				Duration = 0;
			}
			return base.HandleEvent(E);
		}

		public sealed override bool HandleEvent(UseEnergyEvent E) // this is the thing from sunder mind that ends the effect if you move
		{
			if (!E.Passive || (!E.Type?.Contains("Pass") ?? false))
				Duration = 0;
			return base.HandleEvent(E);
		}

		void Buff()
		{
			if (Object != null && (!other?.Object?.HasPart<Fledgling>() ?? false))
			{
				if (WikiRng.Next(1, 1000) == 1000)
				{
					if (Object.IsPlayer())
						UI.Popup.Show($"You consume {other.Object.t()}'s power");
					var e = Object.GetPart<Vampirism>();
					e.ChangeLevel(e.Level + 1);
				}
				//rejuvenate
			}

		}

		protected bool Security()
		{
			if (InvalidActor())
			{
				Duration = 0;
				return false;
			}
			if (!other.Object.HasHitpoints())
			{
				other = null;
				Duration = 0;
				return false;
			}
			if (!other.Object.HasEffectDescendedFrom<IFeeding>())
			{
				other = null;
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

		protected void CheckIfRecognized()
		{
			if(Object.TryGetEitherLongProperty(FLAGS.VICTIM, FLAGS.VICTIM_HOSTILE, out var value) && value > 1000)
				AddPlayerMessage("You recognize the flavor of this one.");
		}
		protected bool Feed()
		{
			int damage = Damage.RollCached();
			Amount = Ghoul ? damage / 2 : damage;
			if (base.Object.IsPlayer())
			{
				if (Vitae.IDontWantToPuke(true, Ghoul))
				{
					Duration = 0;
					return false;
				}
				Vitae.Drink(true, Ghoul);
			}
			base.Object.Heal(Amount, Message: true, FloatText: true, RandomMinimum: true);
			return ThrallCheck();
		}

		bool ThrallCheck()
		{
			if (Ghoul)
			{
				if (other.Object.hitpoints - Amount <= 0)
				{
					Duration = 0;
					AddPlayerMessage($"{other.Object.t()} has no more blood to give.");
					return false;
				}
			}
			return true;
		}
		protected void Bloodloss()
		{
			if (!isAttacker && base.Object is not null)
			{
				if (VictimHP <= 75 && VictimHP > 50 && !base.Object.HasEffect<Woozy>() && !base.Object.HasEffect<Asleep>())
					base.Object.ApplyEffect(new Woozy(9999, 5));
				if (VictimHP <= 50 && !base.Object.HasEffect<Pale>())
					base.Object.ApplyEffect(new Pale(9999));
				if (VictimHP <= 25 && !base.Object.HasEffect<KO>() && !base.Object.HasEffect<Asleep>() && !StealthVersion) //stealth victims get put to sleep on feed end
					base.Object.ApplyEffect(new KO(9999));                                                          //dont want to stack two effects of the same type literally
			}
		}

		protected void FeedBroken()
		{
			if (Object?.Incap(false) ?? true)
				Duration = 0;
		}

		void VampireKilled()
		{
			PlayHumanityMessages(friendly);
			other.Object.SetStringProperty(FLAGS.DEAD, null); //checking for if they have Hitpoints in Remove() did not work. causes a humanity loss dupe bug because victim = true on death.
			Object.GetPart<Humanity>().VampireKilled();
		}
		void PlayHumanityMessages(bool friendly)
		{
			if (Object.IsPlayer())
			{
				if (!friendly)
					AddPlayerMessage("For draining an innocent to death, you lose humanity.");
				else
					AddPlayerMessage("For draining your companion to death, you lose humanity.");
			}
			else if (!friendly)
				AddPlayerMessage("For draining an innocent to death, " + Object.t() + " loses humanity,");
			else
				AddPlayerMessage("For draining their companion to death, " + Object.t() + " loses humanity.");
		}

		public override bool Apply(GameObject Object)
		{
			if (isAttacker)
			{
				base.Object.SetStringProperty(FLAGS.FEED, FLAGS.TRUE);
				CheckIfRecognized();
			}
			return true;
		}
		public override void Remove(GameObject Object)
		{
			if (!isAttacker)
				Object?.RemoveEffect<Vampires_Kiss>();
			if (isAttacker)
			{
				other?.Object?.RemoveEffect<Vampires_Kiss>();
				CleanUpAndFinish();
				if (Object != null && Object.TryGetPart<Vampirism>(out Vampirism v))
					MakeFangsBloody(v);
			}
		}

		void MakeFangsBloody(Vampirism v)
		{
			v.FangsObject.DisplayName = "{{r|bloody}} fangs";
			v.bloodycounter = 1;
			base.Object.SetStringProperty(FLAGS.FEED, FLAGS.FALSE);
		}

		void CleanUpAndFinish()
		{
			EndingStrings();
			MarkVictim();
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
			if (!base.Object.HasEffect<Dominated>()) //if the player ever encounters an AI vampire they can go crazy without fear of losing any humanity themselves during feeding
			{                                       //but only feeding, anything else tracks back to the original player's humanity score
				if (other?.Object?.CheckFlag(FLAGS.INNOCENT) ?? false) //ALSO prevents a potential bug where you would lose humanity for killing someone
					other.Object.SetLongProperty(FLAGS.VICTIM, The.Game.Turns); //that was fed on, technically, by another vampire (even if you were dominating them)
				else if (other?.Object?.IsFriendly(base.Object) ?? false)
					other.Object.SetLongProperty(FLAGS.VICTIM_HOSTILE, The.Game.Turns);
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
