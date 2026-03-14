using System;
using XRL.Core;
using XRL.World.Parts;
using VampirismSys.Properties;
using VampirismSys.Core;
using XRL.World.Parts.Mutation;
using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Effects
{

	/// <summary>
	/// Base class for feeding that handles events, blood consumption, removal, humanity deduction, and provides premade methods for inheritors to invoke.
	/// </summary>

	[Serializable]
	public abstract class BaseFeedEffect : IScribedEffect
	{

		internal static bool AutoLevel;
		public GameObjectReference other; // a long time ago, this was life drain
		VampireBloodMetabolism _Vitae;
		VampireBloodMetabolism Vitae => _Vitae ??= Object.GetPart<VampireBloodMetabolism>();
		int VictimHP => isAttacker ? other.Object.GetHPPercent() : base.Object.GetHPPercent();
		protected int Amount;
		public bool isAttacker
		{
			get => _isAttacker;
			protected init
			{
				_isAttacker = value;
			}
		}
		public string Damage
		{
			get => _damage;
			protected init
			{
				_damage = value;
			}
		}

		public bool Ghoul
		{
			get => _ghoul;
			protected init
			{
				_ghoul = value;
			}
		}

		public bool friendly
		{
			get => _friendly;
			protected init
			{
				_friendly = value;
			}
		}

		public bool vampire
		{
			get => _vampire;
			protected init
			{
				_vampire = value;
			}
		}

		bool _isAttacker;

		string _damage;

		bool _ghoul;

		bool _friendly;

		bool _vampire;

		public override void Write(GameObject Basis, SerializationWriter Writer)
		{
			Writer.Write(_isAttacker);
			Writer.Write(_damage);
			Writer.Write(_ghoul);
			Writer.Write(_friendly);
			Writer.Write(_vampire);
			base.Write(Basis, Writer);
		}

		public override void Read(GameObject Basis, SerializationReader Reader)
		{
			_isAttacker = Reader.ReadBoolean();
			_damage = Reader.ReadString();
			_ghoul = Reader.ReadBoolean();
			_friendly = Reader.ReadBoolean();
			_vampire = Reader.ReadBoolean();
			base.Read(Basis, Reader);
		}

		public BaseFeedEffect()
		{
			DisplayName = "";
		}
		internal BaseFeedEffect(GameObject other, bool isAttacker, string DamagePerRound, int Duration, bool Ghoul, bool friendly, bool vampire) : this()
		{
			this.Damage = DamagePerRound;
			this.Duration = Duration;
			this.other = other.Reference();
			this.isAttacker = isAttacker;
			this.Ghoul = Ghoul;
			this.friendly = friendly;
			this.vampire = vampire;
		}
		public override string GetDescription() => isAttacker ? "{{R sequence|feeding}}" : "";
		public override string GetDetails() => Damage + " damage per turn.";
		bool InvalidActor() => other?.Object?.IsInvalid() ?? true || !other.Object.InSameZone(Object);
		protected abstract void Attack();
		public override bool WantEvent(int ID, int cascade)
		{
			if (ID == KilledEvent.ID)
				return isAttacker;
			if (ID == SingletonEvent<UseEnergyEvent>.ID)
				return isAttacker && Object.IsPlayer();
			if (ID == TookDamageEvent.ID)
				return isAttacker && !Object.IsPlayer();
			if (ID == SingletonEvent<EndTurnEvent>.ID)
				return Duration > 0 && Security() && !FeedBroken();
			return base.WantEvent(ID, cascade);
		}
		public override bool HandleEvent(EndTurnEvent E)
		{
			if (isAttacker && Feed())
				Attack();
			else if (!isAttacker)
				Bloodloss();
			return base.HandleEvent(E);
		}
		public override bool HandleEvent(KilledEvent E) //cannot be KilledEvent because StealthFeed does not count as an actual kill for the feeder
		{
			if (E.Killer == Object)
			{
				if (E.Dying != null && E.Dying == other?.Object)
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
			Amount = Ghoul ? damage / 2 : damage;
			if (base.Object.IsPlayer())
			{
				if (Vitae.PukeWarning(true))
				{
					Duration = 0;
					return false;
				}
				Vitae.Drink(VampirismSys.Rules.Vitae.BLOOD_PER_FEED);
			}
			Strings();
			base.Object.Heal(Amount, Message: true, FloatText: true, RandomMinimum: true);
			return ThrallCheck();
		}

		void KilledEventHandler()
		{
			if (vampire)
				Diablerie();
			else if (Object.IsPlayer() && UI.Options.GetOptionBool(VampirismSys.Rules.ModOptions.HUMANITY) && !Object.CheckFlag(Flags.GO) && (friendly || other.Object.CheckFlag(Flags.INNOCENT)))
			{
				if (UI.Options.GetOptionBool(VampirismSys.Rules.ModOptions.DOUG) && friendly && !other.Object.IsGhoulOf(Object) && !other.Object.IsBeguiledBy(Object))
					return;
				else
					VampireKilled();
			}
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

		bool Security()
		{
			if (InvalidActor())
			{
				other = null;
				Duration = 0;
				return false;
			}
			if (!other.Object.HasHitpoints())
			{
				Duration = 0;
				return false;
			}
			if (!other.Object.HasEffectDescendedFrom<BaseFeedEffect>())
			{
				Duration = 0;
				return false;
			}
			if (Object.DistanceTo(other.Object) > 1)
			{
				Duration = 0;
				return false;
			}
			return true;

		}


		void CheckIfRecognized(GameObject Object)
		{
			if (!Ghoul && Object.TryGetLongProperty(Flags.VICTIM, Flags.VICTIM_HOSTILE, out var value) && value > 1000)
				AddPlayerMessage("You recognize the flavor of this one.");
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
			if (isAttacker && (Object?.Incap(false) ?? true))
			{
				Duration = 0;
				return true;
			}
			return false;
		}

		void VampireKilled()
		{
			PlayHumanityMessages(friendly);
			other.Object.SetStringProperty(Flags.DEAD, null); //checking for if they have Hitpoints in Remove() did not work. causes a humanity loss dupe bug because victim = true on death.
			Object.GetPart<Humanity>().VampireKilled();
		}

		public override bool Apply(GameObject Object)
		{
			if (isAttacker)
			{
				Object.SetStringProperty(Flags.FEED, Flags.TRUE);
				CheckIfRecognized(Object);
			}
			return true;
		}
		public override void Remove(GameObject Object)
		{
			if (isAttacker)
			{
				EndingStrings(Object);
				MakeFangsBloody(Object.GetPart<Vampirism>());
			}
			else if (!other?.Object?.HasEffect<Dominated>() ?? false)
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
			else if (Object.IsFriendly(other.Object))
				Object.SetLongProperty(Flags.VICTIM_HOSTILE, The.Game.Turns);
		}

		void Strings()
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
			bool victimNotNull = other?.Object != null; //unfortunately cant really know if its ending because the other is null or not
			if (Object.IsPlayer())
			{
				if (victimNotNull)
					AddPlayerMessage("You release " + other.Object.t() + "'s neck.");
				else
					AddPlayerMessage("You release your victim's neck.");
			}
			else if (victimNotNull && other.Object.IsPlayer())
			{
				if (Object.HasHitpoints())
					AddPlayerMessage(Object.t() + " releases your neck.");
				else
					AddPlayerMessage(Object.t() + " 's grip on your neck goes slack.");
			}
			else if (Object.HasHitpoints())
			{
				if (victimNotNull)
					AddPlayerMessage(base.Object.t() + " releases " + other.Object.t() + "'s neck.");
			}
			else if (victimNotNull)
				AddPlayerMessage($"{Object.t()}'s grip on {other.Object.t()}'s neck goes slack.");
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
