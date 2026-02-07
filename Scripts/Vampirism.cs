using System;
using ConsoleLib.Console;
using XRL.UI;
using XRL.World.Anatomy;
using XRL.World.Effects;
using Nexus.Properties;
using Nexus.Registry;
using Nexus.Core;
using Nexus.Biting;
using Nexus.Attack;
using Nexus.Rules;
using System.Collections.Generic;
using Nexus.Powers;
using System.Reflection.Metadata;


namespace XRL.World.Parts.Mutation
{

	[Serializable]
	public class Vampirism : BaseDefaultEquipmentMutation
	{
		public const string COMMAND_NAME = "CommandFeedBlood";
		public const string ABILITY_NAME = "Feed";
		public const string BodyPartType = "Face";
		public Guid FangsActivatedAbilityID = Guid.Empty;
		public string ManagerID => ParentObject.ID + "::Vampiric Fangs";
		public GameObject FangsObject;
		public override bool CanSelectVariant => false;
		public override bool UseVariantName => false;
		public bool GameOver;
		public int bloodycounter;
		FeedCommand _FeedCommand;
		public FeedCommand FeedCommand => _FeedCommand ??= new FeedCommand(this);
		
		[NonSerialized]
		public bool WasTerrifiedByFlames;

		public override string GetDescription() => "You feed on the blood of living creatures.";

		public override bool ChangeLevel(int NewLevel)
		{
			SyncLevels(NewLevel);
			return base.ChangeLevel(NewLevel);
		}

		void SyncLevels(int NewLevel)
		{
			List<IVampiricSpell> abilities = ParentObject.GetPartsAndEffectsImplementing<IVampiricSpell>(false);
			for (int i = 0; i < abilities.Count; i++)
				abilities[i].SyncLevels(NewLevel);

		}
		public string GetDamageDice()
		 =>
			Level switch
			{
				< 3 => Level % 2 == 1 ? "2d3" : "2d4",
				_ => Level % 2 == 1 ? $"2d3+ {Level / 2}" : $"2d4+ {(Level - 1) / 2}",
			};

		public override void CollectStats(Templates.StatCollector stats, int Level)
		{
			int num = Math.Max(ParentObject.StatMod("Agility"), Level) + ParentObject.GetStat("Level").Value;
			switch (num)
			{
				case 0:
					stats.Set("Attack", "1d8", !stats.mode.Contains("ability"));
					break;
				case > 0:
					stats.Set("Attack", "1d8+" + num, !stats.mode.Contains("ability"));
					break;
				default:
					stats.Set("Attack", "1d8" + num, !stats.mode.Contains("ability"));
					break;
			}
			stats.Set("HP", GetDamageDice() + " blood");
			stats.CollectCooldownTurns(MyActivatedAbility(ActivatedAbilityID), FEED.COOLDOWN);
		}
		public override string GetLevelText(int Level)
		=> "Feeds {{rules|" + GetDamageDice() + "}} blood per round, for up to {{rules|5}} rounds.\n" +
		"Success roll: {{rules|mutation rank}} or Agility mod (whichever is higher) + character level + 1d8 VS. Defender DV + character level.\n";

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Registrar.Register("LungedTarget");
			Registrar.Register(Events.GAMEOVER);
			Registrar.Register(Events.WISH_HUMANITY);
		}
		public override bool FireEvent(Event E)
		{
			switch (E.ID)
			{
				case Events.UPDATE:
					Nexus.Update.Update.Check(ParentObject);
					break;
				case Events.GAMEOVER:
					GameOver = true;
					break;
				case Events.WISH_HUMANITY:
					GameOver = false;
					break;
				case "LungedTarget":
					if (HasFangs() && !ParentObject.Body.IsPrimaryWeapon(FangsObject))
						BiteATK(FangsObject, E.GetGameObjectParameter("Defender"));
					break;
			}
			return base.FireEvent(E);
		}
		public override bool WantEvent(int ID, int cascade)
		{
			if (bloodycounter > 0 && HasFangs() && ID == SingletonEvent<EndTurnEvent>.ID)
				return true;
			if (ID == PooledEvent<CommandEvent>.ID || ID == AIGetOffensiveAbilityListEvent.ID || ID == PooledEvent<AfterDismemberEvent>.ID || ID == SingletonEvent<BeforeAbilityManagerOpenEvent>.ID)
				return true;
			if (ParentObject.IsPlayer() && Options.GetOptionBool(OPTIONS.NIGHTBEAST) && !ParentObject.OnWorldMap()) //this is very very restrcited to player only
			{
				if (ID == SingletonEvent<BeginTakeActionEvent>.ID && IsDay() && (ParentObject.CurrentZone?.IsOutside() ?? false)) //Albino
					return true;
				if (ID == BeforeRenderEvent.ID)
					return true;
			}
			if (Options.GetOptionBool(OPTIONS.FIRE) && !ParentObject.CheckFlag(FLAGS.FRENZY))//force passing turn does not play well with the Terrified effect and//likely wont work at all with frenzy
			{
				//if (WasTerrifiedByFlames && (ID == SingletonEvent<BeginTakeActionEvent>.ID || ID == EffectRemovedEvent.ID))
				//	return true; i was using these to forcepassturn and end forcepassturn with a bool WasScaredByFire
				if (!ParentObject.HasEffect<Blaze_Tonic>())
				{
					if (ID == TookDamageEvent.ID)
						return true;
					if (Options.GetOptionBool(OPTIONS.TORCH) && ID == EquipperEquippedEvent.ID)
						return true;
					if (ID == SingletonEvent<BeforeTakeActionEvent>.ID)
						return true;
				}
				if (WasTerrifiedByFlames)
				{
					if (ID == EffectAppliedEvent.ID || ID == EffectRemovedEvent.ID)
						return true;
				}
			}
			return base.WantEvent(ID, cascade);
		}

		public override bool HandleEvent(EffectRemovedEvent E)
		{
			if (E.Effect is Terrified)
				WasTerrifiedByFlames = false;
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(EffectAppliedEvent E)
		{
			if (E.Effect is Blaze_Tonic tonic && tonic.Object == ParentObject)
				ParentObject.RemoveEffect<Terrified>();
			return base.HandleEvent(E);
		}
		public override bool HandleEvent(BeforeTakeActionEvent E)
		{
			if (!ParentObject.HasEffect<Terrified>()) //to avoid the effect stacking itself in flame intense environments
			{
				if (ParentObject.LocalCells(out var cells))
					Search(cells);
			}
			return base.HandleEvent(E);
		}

		void Search(List<Cell> cells)
		{
			for (int i = 0; i < cells.Count; i++)
			{
				for (int x = 0; x < cells[i].Objects.Count; x++)
				{
					GameObject obj = cells[i].Objects[x];
					if (obj.IsAflame() || (obj.Blueprint != "Campfire" && obj.HasPart<AnimatedMaterialFire>()))
					{
						FirePanic(obj, true);
						return;
					}
					else if (LitTorch(obj))
						return;
				}
			}
		}

		bool LitTorch(GameObject obj)
		{
			if (Options.GetOptionBool(OPTIONS.TORCH) && obj.HasPart<TorchProperties>())
			{
				LightSource source = obj.GetPart<LightSource>(); //private field in TorchProperties, but accessible thru the PartsList, no reflection required
				if (source.Lit)
				{     //thanks for parts lists, developers!
					FirePanic(obj, true);
					return true;
				}
			}
			return false;
		}
		public override bool HandleEvent(EquipperEquippedEvent E)
		{
			if (E.Item.Blueprint == "Torch" && The.Game.Turns > 0) //this event runs before the game loads and was causing serious hangups/crashes
			{                                                       //in tandem with the VampirismStartGame mutator that deletes torches
				var Torch = E.Item.GetPart<TorchProperties>();      //just a mess of null errors
				if (!Torch.IsUnlightableBecauseOfLiquidCovering())
					FakeDropTorch(E.Item);

			}
			return base.HandleEvent(E);
		}
		public override bool HandleEvent(TookDamageEvent E)
		{
			if (E.Object == ParentObject && E.Damage.Attributes.Contains("Fire"))
			{
				FirePanic(E?.Actor, true);
				E.Damage.Amount *= 2;
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(BeforeRenderEvent E)
		{
			AddLight(21, LightLevel.Dimvision);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(BeginTakeActionEvent E)
		{
			AddPlayerMessage("{{W|I HATE SUNLIGHT!!!}}");
			ParentObject.TakeDamage(WikiRng.Next(5, 10), null, null);
			return base.HandleEvent(E);
		}
		public override bool HandleEvent(AfterDismemberEvent E)
		{
			if (E.Part?.Type == BodyPartType)
			{
				if (E.Actor != null && E.Object != null)
				{
					if (E.Object.IsPlayer())
						Popup.Show($"You are defanged by {E.Actor.t()}!");
					else if (E.Actor.IsPlayer())
						AddPlayerMessage($"You defang {E.Object.t()}!");
					else
						AddPlayerMessage($"{E.Object.t()} is defanged by {E.Actor.t()}!");
				}
				else
					Popup.Show("You defang yourself!");
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(EndTurnEvent E)
		{
			if (WikiRng.Next(1, 10) == 10 && !ParentObject.CheckFlag(FLAGS.FEED))
			{
				AddPlayerMessage("{{r|Blood}} drips from your fangs.");
				if (!ParentObject.OnWorldMap())
					ParentObject.CurrentCell?.AddObject("FangBloodDrop");
			}
			bloodycounter++;
			if (bloodycounter >= 25)
			{
				FangsObject.DisplayName = "fangs";
				bloodycounter = 0;
			}
			return base.HandleEvent(E);
		}
		public override bool HandleEvent(BeforeAbilityManagerOpenEvent E)
		{
			DescribeMyActivatedAbility(FangsActivatedAbilityID, this.CollectStats);
			return base.HandleEvent(E);
		}
		public override bool HandleEvent(AIGetOffensiveAbilityListEvent E)
		{
			if (AITargetting(E))
				E.Add(COMMAND_NAME);
			return base.HandleEvent(E);
		}
		public override bool HandleEvent(CommandEvent E)
		{
			if (E.Command == COMMAND_NAME && Prerequisites())
			{
				if (ParentObject.TryGetTarget(ABILITY_NAME, "feed from", out GameObject Target))
					FeedCommand.Initialize(Target);
			}
			return base.HandleEvent(E);
		}
		bool Prerequisites()
		{
			if (!HasFangs())
			{
				ParentObject.ShowFailure("You have been defanged and cannot feed right now.");
				return false;
			}
			return Checks.Prerequisites(ParentObject, ABILITY_NAME, "feed");
		}
		public bool HasFangs() => FangsObject is not null && ParentObject.HasBodyPart(BodyPartType);
		public void BiteATK(GameObject Fangs, GameObject Defender, bool Auto = false)
		 =>
			Combat.MeleeAttackWithWeapon
			(ParentObject, Defender, Fangs, ParentObject.GetBodyPartByManager(ManagerID), Auto ? "Autohit,Autopen,Biting" : "Biting");
		public void BiteActivate(GameObject Target)
		{
			if (ParentObject.IsPlayer())
				DidX("sink your fangs into", Target.the + Target.ShortDisplayName + "'s neck", "!", null, null, ParentObject);
			else
				DidX("sinks " + ParentObject.its + " fangs into", Target.the + Target.ShortDisplayName + "'s neck", "!", null, null, ParentObject);
			BiteATK(FangsObject, Target, Auto: true);
			Target?.Bloodsplatter();
		}
		bool AITargetting(AIGetOffensiveAbilityListEvent E)
		 =>
			E.Distance <= 1
			&& HasFangs()
			&& IsMyActivatedAbilityAIUsable(FangsActivatedAbilityID)
			&& !E.Actor.Incap(false)
			&& !E.Target.HasEffect<Vampires_Kiss>()
			&& !E.Target.IsFlying
			&& !E.Target.IsFrozen()
			&& !E.Target.IsInStasis()
			&& Checks.Applicable(E.Target);

		void FirePanic(GameObject FireSource, bool external)
		{
			if (!ParentObject.HasEffect<Terrified>())
			{
				WasTerrifiedByFlames = true;
				Capabilities.AutoAct.Interrupt();
				if (FireSource == null)
				{
					if (external)
						AlreadyOnFire("You flee from the fire!");
					ParentObject.ApplyEffect(new Terrified(WikiRng.Next(10, 15), ParentObject.CurrentCell, true));
				}
				else
				{
					if (external)
						AlreadyOnFire("{{R|I HATE FIRE!!!}}");
					ParentObject.ApplyEffect(new Terrified(WikiRng.Next(10, 15), FireSource, false));
				}
			}
		}

		public void FakeDropTorch(GameObject Torch)
		{
			if (ParentObject.CurrentCell != null)
			{
				if (ParentObject.IsPlayer())
					Popup.Show("{{R|I HATE FIRE!!!}}");
				GameObject replacement = GameObject.Create("Torch");
				Torch.Obliterate();
				ParentObject.CurrentCell.AddObject(replacement);
				DidXToY("drop", replacement, null, null, null, null, null, null, UseFullNames: false, IndefiniteSubject: false, IndefiniteObject: true);
				var Part = replacement.GetPart<TorchProperties>();
				Part.Light();
				if (!Part.IsUnlightableBecauseOfLiquidCovering() && !Part.IsUnlightableBecauseOfSubmersion())
					FirePanic(replacement, false);
				else
					Part.Extinguish();
			}
		}
		void AlreadyOnFire(string text)
		{
			if (ParentObject.IsPlayer())
			{
				text = ParentObject.IsAflame() ? "{{R|I'M ON FIRE!!!}}" : text;
				AddPlayerMessage(text);
			}
		}
		public override IPart DeepCopy(GameObject Parent, Func<GameObject, GameObject> MapInv)
		{
			Vampirism obj = base.DeepCopy(Parent, MapInv) as Vampirism;
			obj.FangsObject = null;
			return obj;
		}

		public override bool Mutate(GameObject GO, int Level)
		{
			VampireBuilder.Make(GO);
			FangsActivatedAbilityID = AddMyActivatedAbility(ABILITY_NAME, COMMAND_NAME, "Physical Mutations", null, "\u009f");
			return base.Mutate(GO, Level);
		}

		public override bool Unmutate(GameObject GO)
		{
			VampireBuilder.Unmake(GO);
			RemoveMyActivatedAbility(ref FangsActivatedAbilityID);
			CleanUpMutationEquipment(GO, ref FangsObject);
			return base.Unmutate(GO);
		}
		public override void OnRegenerateDefaultEquipment(Body body) //this is straight up beak code that i stole and didnt even research for a second
		{
			if (!TryGetRegisteredSlot(body, BodyPartType, out BodyPart BodyPart))
			{
				BodyPart = body.GetFirstPart(BodyPartType);
				if (BodyPart is not null)
					RegisterSlot(BodyPartType, BodyPart);
			}
			if (BodyPart is not null)
				Create(BodyPart);
			base.OnRegenerateDefaultEquipment(body);
		}

		void Create(BodyPart BodyPart)
		{
			FangsObject = GameObjectFactory.Factory.CreateObject("Vampiric Fangs");
			MeleeWeapon wep = FangsObject.GetPart<MeleeWeapon>();
			Armor armor = FangsObject.GetPart<Armor>();
			wep.Skill = "ShortBlades";
			wep.BaseDamage = "1";
			wep.Slot = BodyPart.Type;
			armor.WornOn = BodyPart.Type;
			armor.AV = 0;
			BodyPart.DefaultBehavior = FangsObject;
			BodyPart.DefaultBehavior.SetStringProperty("TemporaryDefaultBehavior", "Vampiric Fangs");
			FangsObject.SetStringProperty("HitSound", "Sounds/Abilities/sfx_ability_mutation_beak_peck");
			ResetDisplayName();
		}

		public override IRenderable GetIcon() => MutationFactory.TryGetMutationEntry(this, out var Entry) ? Entry.GetRenderable() : null;
		public static bool IsUnmanagedPart(BodyPart Part) => Part.Manager.IsNullOrEmpty();
		public override bool GeneratesEquipment() => true;
		public override bool AllowStaticRegistration() => true;
	}
}
