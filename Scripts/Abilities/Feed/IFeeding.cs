using System;
using XRL.Core;
using XRL.World.Parts;
using Nexus.Properties;
using Nexus.Core;
using XRL.World.Parts.Mutation;
using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Effects
{

	/// <summary>
	/// Base class for feeding that handles events, blood consumption, removal, humanity deduction, and provides premade methods for inheritors to invoke.
	/// </summary>

	[Serializable]
	public abstract class IFeeding : Effect
	{

		public static bool AutoLevel;
		public GameObjectReference other; // a long time ago, this was life drain
		Vitae _Vitae;
		Vitae Vitae => _Vitae ??= Object.GetPart<Vitae>();
		public bool isAttacker => _isAttacker;
		int VictimHP => isAttacker ? other.Object.GetHPPercent() : base.Object.GetHPPercent();
		protected int Amount;


		public string Damage;


		public bool _isAttacker;


		public bool StealthVersion;


		public bool Ghoul;


		public bool friendly;


		public bool vampire;

		public IFeeding() => DisplayName = "";
		public IFeeding(GameObject other, bool isAttacker, string DamagePerRound, int Duration, bool Ghoul, bool friendly, bool vampire) : this()
		{
			this.Damage = DamagePerRound;
			this.Duration = Duration;
			this.other = other.Reference();
			this._isAttacker = isAttacker;
			this.Ghoul = Ghoul;
			this.friendly = friendly;
			this.vampire = vampire;
		}
		public sealed override string GetDescription() => isAttacker ? "{{R sequence|feeding}}" : "";
		public sealed override string GetDetails() => Damage + " damage per turn.";
		bool InvalidActor() => other?.Object?.IsInvalid() ?? true || base.Object == null || other.Object.InSameZone(Object);
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
			if (UI.Options.GetOptionBool(Nexus.Rules.ModOptions.HUMANITY) && E.Killer == Object && E.Dying != null && E.Dying == other?.Object)
			{
				if (!vampire && !Object.CheckFlag(Flags.GO) && (friendly || other.Object.CheckFlag(Flags.INNOCENT)))
					if (UI.Options.GetOptionBool(Nexus.Rules.ModOptions.DOUG) && friendly && !other.Object.IsGhoulOf(Object) && !other.Object.IsBeguiledBy(Object))
						return base.HandleEvent(E);
					else
						VampireKilled();
			}
			if (vampire)
				Diablerie();
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

		void Diablerie()
		{
			if (Object != null && (!other?.Object?.HasStringProperty(Flags.FLEDGLING) ?? false))
			{
				if (AutoLevel || WikiRng.Next(1, 20) == 1)
				{

					if (Object.IsPlayer())
					{
						string msg = WikiRng.Next(1, 2) == 2 ? "Diablerie!" : "Amaranth!";
                        UI.Popup.Show($"{msg} You consume {other.Object.t()}'s soul!");
					}
					var e = Object.GetPart<Vampirism>();
					e.BaseLevel++;
					e.ChangeLevel(e.Level);
				}
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
			if (Object.DistanceTo(other.Object) > 1)
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
			if (!Ghoul && Object.TryGetLongProperty(Flags.VICTIM, Flags.VICTIM_HOSTILE, out var value) && value > 1000)
				AddPlayerMessage("You recognize the flavor of this one.");
		}
		protected bool Feed()
		{
			int damage = Damage.RollCached();
			Amount = Ghoul ? damage / 2 : damage;
			if (base.Object.IsPlayer())
			{
				if (Vitae.IDontWantToPuke(true))
				{
					Duration = 0;
					return false;
				}
				Vitae.Drink(true);
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
			other.Object.SetStringProperty(Flags.DEAD, null); //checking for if they have Hitpoints in Remove() did not work. causes a humanity loss dupe bug because victim = true on death.
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
				base.Object.SetStringProperty(Flags.FEED, Flags.TRUE);
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
			v.BloodyFangsCounter = 1;
			base.Object.SetStringProperty(Flags.FEED, Flags.FALSE);
		}

		void CleanUpAndFinish()
		{
			EndingStrings();
			MarkVictim();
		}
		void EndingStrings()
		{
			if (base.Object?.IsPlayer() ?? false && isAttacker)
			{
				if (other?.Object != null)
					IComponent<GameObject>.AddPlayerMessage("You release " + other.Object.t() + "'s neck.");
				else
					AddPlayerMessage("You release your victim's neck.");
			}
			else if (base.Object?.IsPlayer() ?? false && !isAttacker)
			{
				if (other?.Object?.HasHitpoints() ?? false)
					IComponent<GameObject>.AddPlayerMessage(other.Object.t() + " releases your neck");
				else if (other?.Object != null)
					AddPlayerMessage(other.Object.t() + " 's grip on your neck goes slack.");
				else
					AddPlayerMessage("Your neck is released.");
			}
			else if (base.Object?.HasHitpoints() ?? false && isAttacker && !base.Object.IsPlayer())
			{
				if (other?.Object != null)
					IComponent<GameObject>.AddPlayerMessage(base.Object.t() + " releases " + other.Object.t() + "'s neck");
			}

		}
		void MarkVictim()
		{
			if (!base.Object.HasEffect<Dominated>()) //if the player ever encounters an AI vampire they can go crazy without fear of losing any humanity themselves during feeding
			{                                       //but only feeding, anything else tracks back to the original player's humanity score
				if (other?.Object?.CheckFlag(Flags.INNOCENT) ?? false)
					other.Object.SetLongProperty(Flags.VICTIM, The.Game.Turns);
				else if (other?.Object?.IsFriendly(base.Object) ?? false)
					other.Object.SetLongProperty(Flags.VICTIM_HOSTILE, The.Game.Turns);
			} //this also serves as a huge security measure: if you are dominating, humanity loss by feeding is local as previously stated
			  //however, the death penalty system does not check for the Innocence flag, because Hostile Victims are not considered innocent
		}       //so if dominated targets were able to apply victim, then you would come back and kill them as the original  player and lose humanity
				//I COULD compare feeders by ID as a string property, but humanity is mostly only relative to the player, and i dont care if the Victims part of the Deaths system is inactive when dominating
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
