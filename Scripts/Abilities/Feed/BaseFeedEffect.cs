using System;
using XRL.Core;
using XRL.World.Parts;
using VampirismSys.Properties;
using VampirismSys.Core;
using XRL.World.Parts.Mutation;

namespace XRL.World.Effects
{

	/// <summary>
	/// Base class for feeding that handles events, blood consumption, removal, humanity deduction, and provides premade methods for inheritors to invoke.
	/// </summary>

	[Serializable]
	public abstract class BaseFeedEffect : BeastScribedEffect
	{

		internal static bool AutoLevel;
		public GameObject Other { get => _other; private init { _other = value; } }
		GameObject _other;
		VampireBloodMetabolism Vitae => _Vitae ??= Object.GetPart<VampireBloodMetabolism>();

		[NonSerialized]
		VampireBloodMetabolism _Vitae;

		[NonSerialized]
		protected int Amount;

		public bool IsAttacker { get => _isAttacker; protected init { _isAttacker = value; } }

		protected string Damage { get => _damage; init { _damage = value; } }

		//Victim flags
		protected bool IsGhoul { get => _ghoul; init { _ghoul = value; } }

		protected bool IsFriendly { get => _friendly; init { _friendly = value; } }

		protected bool IsVampire { get => _vampire; init { _vampire = value; } }
		bool _isAttacker;

		string _damage;

		bool _ghoul;

		bool _friendly;

		bool _vampire;
		int VictimHP => IsAttacker ? Other.GetHPPercent() : base.Object.GetHPPercent();

		public BaseFeedEffect()
		{
		}
		protected BaseFeedEffect(GameObject other) : base()
		{
			Duration = VampirismSys.Rules.Feed.DURATION;
			Other = other;
		}
		protected BaseFeedEffect(GameObject other, bool isAttacker, string DamagePerRound, bool Ghoul, bool friendly, bool vampire) : this(other)
		{
			Damage = DamagePerRound;
			IsAttacker = isAttacker;
			IsGhoul = Ghoul;
			IsFriendly = friendly;
			IsVampire = vampire;
		}
		public override string GetDescription() => IsAttacker ? "{{R sequence|feeding}}" : "";
		public override string GetDetails() => Damage + " damage per turn.";
		bool InvalidActor() => Object?.IsInvalid() ?? true || !Other.InSameZone(Object);
		protected abstract void Attack();
		public override bool WantEvent(int ID, int cascade)
		{
			if (ID == KilledEvent.ID)
				return IsAttacker;
			if (ID == SingletonEvent<UseEnergyEvent>.ID)
				return IsAttacker && Object.IsPlayer();
			if (ID == TookDamageEvent.ID)
				return IsAttacker && !Object.IsPlayer();
			if (ID == SingletonEvent<EndTurnEvent>.ID)
				return Duration > 0 && Security() && !FeedBroken();
			return base.WantEvent(ID, cascade);
		}
		public override bool HandleEvent(EndTurnEvent E)
		{
			UI.Popup.Show($"{Duration}");
			if (IsAttacker && Feed())
				Attack();
			else if (!IsAttacker)
				Bloodloss();
			return base.HandleEvent(E);
		}
		public override bool HandleEvent(KilledEvent E) //cannot be KilledEvent because StealthFeed does not count as an actual kill for the feeder
		{
			if (E.Killer == Object)
			{
				if (E.Dying != null && E.Dying == Other)
				{
					KilledEventHandler();
				}
				Duration = 0;
			}
			return base.HandleEvent(E);
		}
		public override bool HandleEvent(TookDamageEvent E) // this handler is for the AI - they will not act while feeding, but if attacked, they will react. 
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

		public override bool HandleEvent(UseEnergyEvent E) // this is the thing from sunder mind that ends the effect if you move
		{
			if (!E.Passive || (!E.Type?.Contains("Pass") ?? false))
				Duration = 0;
			return base.HandleEvent(E);
		}

		bool Feed()
		{
			int damage = Damage.RollCached();
			Amount = IsGhoul ? damage / 2 : damage;
			if (base.Object.IsPlayer())
			{
				if (Vitae.PukeWarning(true))
				{
					Duration = 0;
					return false;
				}
				Vitae.Drink(VampirismSys.Rules.Metab.BLOOD_PER_FEED);
			}
			Strings();
			base.Object.Heal(Amount, Message: true, FloatText: true, RandomMinimum: true);
			return ThrallCheck();
		}

		void KilledEventHandler()
		{
			if (IsVampire)
				Diablerie();
			else if (Object.IsPlayer() && UI.Options.GetOptionBool(VampirismSys.Rules.ModOptions.HUMANITY) && !Object.CheckFlag(Flags.GO) && (IsFriendly || Other.CheckFlag(Flags.INNOCENT)))
			{
				if (UI.Options.GetOptionBool(VampirismSys.Rules.ModOptions.DOUG) && IsFriendly && !Other.IsGhoulOf(Object) && !Other.IsBeguiledBy(Object))
					return;
				else
					VampireKilled();
			}
		}

		void Diablerie()
		{
			if (Object != null && (!Other?.HasStringProperty(Flags.FLEDGLING) ?? false))
			{
				if (AutoLevel || WikiRng.Next(1, 20) == 1)
				{

					if (Object.IsPlayer())
					{
						string msg = WikiRng.Next(1, 2) == 2 ? "Diablerie!" : "Amaranth!";
						UI.Popup.Show($"{msg} You consume {Other.t()}'s soul!");
					}
					var e = Object.GetPart<Vampirism>();
					e.BaseLevel++;
					e.ChangeLevel(e.Level);
				}
			}
		}

		bool Security()
		{
			if (InvalidActor())
			{
				_other = null;
				Duration = 0;
				return false;
			}
			if (!Other.HasHitpoints())
			{
				Duration = 0;
				return false;
			}
			if (!Other.HasEffectDescendedFrom<BaseFeedEffect>())
			{
				Duration = 0;
				return false;
			}
			if (Object.DistanceTo(Other) > 1)
			{
				Duration = 0;
				return false;
			}
			return true;

		}


		void CheckIfRecognized(GameObject Object)
		{
			if (!IsGhoul && Object.TryGetLongProperty(Flags.VICTIM, Flags.VICTIM_HOSTILE, out var value) && value > 1000)
				AddPlayerMessage("You recognize the flavor of this one.");
		}
		bool ThrallCheck()
		{
			if (IsGhoul)
			{
				if (Other.hitpoints - Amount <= 0)
				{
					Duration = 0;
					AddPlayerMessage($"{Other.t()} has no more blood to give.");
					return false;
				}
			}
			return true;
		}
		void Bloodloss()
		{
			if (base.Object is not null)
			{
				if (VictimHP <= 75 && VictimHP > 50 && !base.Object.HasEffect<Woozy>() && !base.Object.HasEffect<Asleep>())
					base.Object.ApplyEffect(new Woozy(5));
				if (VictimHP <= 50 && !base.Object.HasEffect<Pale>())
					base.Object.ApplyEffect(new Pale());
				if (VictimHP <= 25 && !base.Object.HasEffect<KO>() && !base.Object.HasEffect<Asleep>())
					base.Object.ApplyEffect(new KO());
			}
		}

		bool FeedBroken()
		{
			if (IsAttacker && (Object?.Incap(false) ?? true))
			{
				Duration = 0;
				return true;
			}
			return false;
		}

		void VampireKilled()
		{
			PlayHumanityMessages(IsFriendly);
			Other.SetStringProperty(Flags.DEAD, null); //checking for if they have Hitpoints in Remove() did not work. causes a humanity loss dupe bug because victim = true on death.
			Object.GetPart<Humanity>().VampireKilled();
		}

		public override bool Apply(GameObject Object)
		{
			if (IsAttacker)
			{
				Object.SetStringProperty(Flags.FEED, Flags.TRUE);
				CheckIfRecognized(Object);
			}
			return true;
		}
		public override void Remove(GameObject Object)
		{
			if (IsAttacker)
			{
				EndingStrings(Object);
				MakeFangsBloody(Object.GetPart<Vampirism>());
			}
			else if (!Other?.HasEffect<Dominated>() ?? false)
				MarkVictim(Object);
		}

		void MakeFangsBloody(Vampirism v)
		{
			v.FangsObject.DisplayName = "{{r|bloody}} fangs";
			v.BloodyFangsCounter = 1;
			base.Object.SetStringProperty(Flags.FEED, Flags.FALSE);
		}

		void MarkVictim(GameObject Object)
		{
			if (Object.CheckFlag(Flags.INNOCENT))
				Object.SetLongProperty(Flags.VICTIM, The.Game.Turns);
			else if (Object.IsFriendly(Other))
				Object.SetLongProperty(Flags.VICTIM_HOSTILE, The.Game.Turns);
		}

		void Strings()
		{
			if (base.Object is not null && Other is not null)
			{
				if (!base.Object.IsPlayer() && !Other.IsPlayer())
				{
					IComponent<GameObject>.AddPlayerMessage(base.Object.t() + " {{R sequence|feeds}}" + " on " + Other.t() + ".");
				}
				else if (!base.Object.IsPlayer() && Other.IsPlayer())
				{
					IComponent<GameObject>.AddPlayerMessage(base.Object.t() + " {{R sequence|feeds}} on you!");
				}
				else if (base.Object.IsPlayer())
				{
					IComponent<GameObject>.AddPlayerMessage(base.Object.t() + " {{R sequence|feed}}" + " on " + Other.t() + ".");
				}
			}
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


		void EndingStrings(GameObject Object)
		{
			bool victimNotNull = Other != null; //unfortunately cant really know if its ending because the other is null or not
			if (Object.IsPlayer())
			{
				if (victimNotNull)
					AddPlayerMessage("You release " + Other.t() + "'s neck.");
				else
					AddPlayerMessage("You release your victim's neck.");
			}
			else if (victimNotNull && Other.IsPlayer())
			{
				if (Object.HasHitpoints())
					AddPlayerMessage(Object.t() + " releases your neck.");
				else
					AddPlayerMessage(Object.t() + " 's grip on your neck goes slack.");
			}
			else if (Object.HasHitpoints())
			{
				if (victimNotNull)
					AddPlayerMessage(base.Object.t() + " releases " + Other.t() + "'s neck.");
			}
			else if (victimNotNull)
				AddPlayerMessage($"{Object.t()}'s grip on {Other.t()}'s neck goes slack.");
		}

		public override bool UseStandardDurationCountdown()
		{
			return true;
		}

		public override bool SameAs(Effect e)
		{
			return false;
		}

		public override bool Render(RenderEvent E)
		{
			if (!IsAttacker)
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
