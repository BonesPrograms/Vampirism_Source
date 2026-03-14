using System;
using ConsoleLib.Console;
using XRL.UI;
using XRL.World.Anatomy;
using XRL.World.Effects;
using VampirismSys.Properties;
using VampirismSys.Registry;
using VampirismSys.Core;
using VampirismSys.Attack;
using VampirismSys.Rules;
using System.Collections.Generic;
using Qud.API;
using System.Linq;


namespace XRL.World.Parts.Mutation
{

	[Serializable]
	public class Vampirism : BaseDefaultEquipmentMutation
	{
		public const string COMMAND_NAME = "CommandFeedBlood";
		public const string ABILITY_NAME = "Feed";
		public const string BODYPART_TYPE = "Face";
		public Guid FangsActivatedAbilityID = Guid.Empty;
		public GameObject FangsObject; //your actual fangs
		internal FeedAbility FeedAbility => _feedCommand ??= new(this);
		FeedAbility _feedCommand;
		public string ManagerID => ParentObject.ID + "::Vampiric Fangs"; //i never really researched managerid yet. i assume that the fangs object counts as a bodypart and this is its manager
		public override bool CanSelectVariant => false;
		public override bool UseVariantName => false;
		public bool GameOver;
		public int BloodyFangsCounter;
		public bool Rotschrek
		{
			get => _rotschrek;
			private set
			{
				_rotschrek = value;
			}
		}
		bool _rotschrek;
		bool Immune;
		int TimeOnWorldMap = 0; //problem with this not serializing is if you quit/save while on world map then it will not advance time. to solve this problem i would probably
								//map this value to Stomach.WasOnWorldMap but for now its local				 
		bool WasOnWorldMap => TimeOnWorldMap > 0;


		#region FireEvent/Register
		//though many of these are duplicates of minevent calls, i added them incase any modders out there preferred string events over minevents for their
		//custom diseases and spores - see True Undead

		//the only one were missing is CanApplyEffect fireEvent - we should check E.GetStringParameter("Name")
		static readonly string[] RegisteredEvents =
		{ "LungedTarget", Events.GAMEOVER, Events.WISH_HUMANITY, "CanApplySpores", "ApplySpores", "ApplyDiseaseOnset", "ApplyDisease", "CanApplyAshPoison" };
		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			RegisteredEvents.ForEach(x => Registrar.Register(x));
		}
		public override bool FireEvent(Event E)
		{
			switch (E.ID)
			{
				case "ApplyDisease" or "ApplyDiseaseOnset" or "ApplySpores" or "CanApplySpores" or "CanApplyAshPoison":
					return !Options.GetOptionBool(ModOptions.TRUE_UNDEAD);
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
		#endregion
		#region WantEvent
		public override bool WantEvent(int ID, int cascade)
		{
			if (ID == RespiresEvent.ID || ID == ApplyEffectEvent.ID || ID == CanApplyEffectEvent.ID || ID == CheckGasCanAffectEvent.ID || ID == BeforeApplyDamageEvent.ID) //the confusion between ApplyEffectEvent and CanApplyEffectEvent was painful
				return Options.GetOptionBool(ModOptions.TRUE_UNDEAD);
			if (ID == AfterPlayerBodyChangeEvent.ID || ID == SingletonEvent<BeginTakeActionEvent>.ID || ID == PooledEvent<CommandEvent>.ID || ID == AIGetOffensiveAbilityListEvent.ID || ID == PooledEvent<AfterDismemberEvent>.ID || ID == SingletonEvent<BeforeAbilityManagerOpenEvent>.ID)
				return true;
			if (ID == EffectAppliedEvent.ID)
				return Options.GetOptionBool(ModOptions.FIRE);
			if (ID == EnteredCellEvent.ID)
				return Options.GetOptionBool(ModOptions.NIGHTBEAST) && ParentObject.IsPlayer();
			if (ID == SingletonEvent<EndTurnEvent>.ID)
				return BloodyFangsCounter > 0 && HasFangs();
			if (ID == BeforeRenderEvent.ID)
				return CheckNightbeast();
			if (ID == TookDamageEvent.ID)
				return Options.GetOptionBool(ModOptions.FIRE) && !Immune;
			if (ID == EffectRemovedEvent.ID)
				return Rotschrek || Immune;
			if (ID == EnteringZoneEvent.ID)
				return ParentObject.HasStringProperty(Flags.Mod.OLD_SAVE) && ParentObject.IsPlayer();
			if (ID == EquipperEquippedEvent.ID || ID == TookEvent.ID)
				return The.Game.Turns > 0;//will fire and go crazy if you spawn with silver items in your inventory or torches
			return base.WantEvent(ID, cascade);
		}

		#endregion

		#region True Undead

		//funny thing i noticed while making this: vampires and undead are similar mechanically to Robots
		//inorganics also do not breathe and are immune to disease... interesting stuff
		//and vampires cannot feed on robots, even if theyre a wight they can interract without a problem... vampire + robot alliance

		public override bool HandleEvent(CheckGasCanAffectEvent E)
		{
			return GasCheck(E);
		}
		public override bool HandleEvent(BeforeApplyDamageEvent E)
		{
			if (E.Object == ParentObject && E.Damage.HasAttribute("Asphyxiation")) //if we add poison immunity well want to add it here as well
			{
				NotifyTargetImmuneEvent.Send(E.Weapon, E.Object, E.Actor, E.Damage, this);
				E.Damage.Amount = 0;
				return false;
			}
			return base.HandleEvent(E);
		}
		public override bool HandleEvent(CanApplyEffectEvent E)
		{
			return EffectCheck(E);
		}
		public override bool HandleEvent(ApplyEffectEvent E)
		{
			return EffectCheck(E);
		}
		public override bool HandleEvent(RespiresEvent E)
		{
			if (E.Object == ParentObject)
				return false;
			return base.HandleEvent(E);
		}

		bool GasCheck(CheckGasCanAffectEvent E) =>
		E.Gas.GasType switch
		{
			"Poison" or "Ash" or "Disease" or "FungalSpores" or "Confusion" or "Sleep" or "Stun" => false,
			_ => base.HandleEvent(E)
		};
		bool EffectCheck(IEffectCheckEvent E) =>
		E.Name switch
		{
			"DiseaseOnset" or "Disease" or "AshPoison" or "CardiacArrest" or "PoisonGasPoison" => false,
			_ => base.HandleEvent(E)
		};

		// or "Poison" or "ToxicConfusion" //not sure about these
		#endregion


		#region Nightbeast

		public override bool HandleEvent(EnteredCellEvent E)
		{
			if (!ParentObject.OnWorldMap())
			{
				if (WasOnWorldMap)
					AdvanceTimeToNight();
				TimeOnWorldMap = 0;
			}
			else
				TimeOnWorldMap++;
			return base.HandleEvent(E);
		}
		public override bool HandleEvent(BeforeRenderEvent E)
		{
			AddLight(21, LightLevel.Dimvision);
			return base.HandleEvent(E);
		}

		bool IsOutsideDuringTheDay() => CheckNightbeast() && IsDay() && (ParentObject.CurrentZone?.IsOutside() ?? false);

		bool CheckNightbeast() => Options.GetOptionBool(ModOptions.NIGHTBEAST) && ParentObject.IsPlayer() && !ParentObject.OnWorldMap();

		public static void AdvanceTimeToNight()
		{
			while (Calendar.IsDay())
				The.Game.TimeTicks++;
		}

		//this method is used across the board by everyone except this type itself
		public static bool SunlightInterference(GameObject ParentObject)
		{
			if (Options.GetOptionBool(ModOptions.NIGHTBEAST))
			{
				if (Calendar.IsDay() && (ParentObject.CurrentZone?.IsOutside() ?? false))
					return true;
			}
			return false;
		}


		#endregion


		#region Fluff

		public override bool HandleEvent(AfterDismemberEvent E)
		{
			if (E.Part?.Type == BODYPART_TYPE)
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
			if (WikiRng.Next(1, 10) == 10 && !ParentObject.CheckFlag(Flags.FEED))
			{
				AddPlayerMessage("{{r|Blood}} drips from your fangs.");
				if (!ParentObject.OnWorldMap())
					ParentObject.CurrentCell?.AddObject("FangBloodDrop");
			}
			BloodyFangsCounter++;
			if (BloodyFangsCounter >= 25)
			{
				FangsObject.DisplayName = "fangs";
				BloodyFangsCounter = 0;
			}
			return base.HandleEvent(E);
		}
		#endregion

		#region [Debuff] Combined Event Handlers (Silver Ailment, Rotschrek, Nightbeast)
		public override bool HandleEvent(EquipperEquippedEvent E)
		{
			if (E.Item.Blueprint == "Torch")
			{
				if (Options.GetOptionBool(ModOptions.FIRE) && Options.GetOptionBool(ModOptions.TORCH)) //this event runs before the game loads and was causing serious hangups/crashes
				{                                                       //in tandem with the VampirismStartGame mutator that deletes torches
					var Torch = E.Item.GetPart<TorchProperties>();      //just a mess of null errors
					if (!Torch.IsUnlightableBecauseOfLiquidCovering())
						FakeDropRotschrek(E.Item);
					E.RequestInterfaceExit();
					return false;
				}
			}
			if (E.Item.IsSilver() && Options.GetOptionBool(ModOptions.SILVER))
			{
				Popup.Show(nameof(EquipperEquippedEvent));
				E.Item.ForceUnequip(true);
				//	SilverAilment(E.Item);
				E.RequestInterfaceExit();
				return false;
			}
			return base.HandleEvent(E);
		}
		public override bool HandleEvent(TookEvent E)
		{
			if (E.Item.IsSilver() && Options.GetOptionBool(ModOptions.SILVER))
			{
				SilverAilment(E.Item);
				E.RequestInterfaceExit();
				return false;
			}
			if (Options.GetOptionBool(ModOptions.FIRE) && FireyObject(E.Item))
			{
				Panic(E.Item, true);
				E.RequestInterfaceExit();
				return false;
			}
			return base.HandleEvent(E);
		}
		//if you place a vampire inbetween the two torches infront of elder irudads house, he is locked in a permanent state of terror if he already has terrified                  /
		//because terrified removes the old one if you try to apply a new one and it just cycles between the two and hes unable to move anywhere cause every empty adjacent cell
		// borders a flame object
		//however if its the player it doesnt matter because you can move yourself a bit so rotschrek can chain on the player
		public override bool HandleEvent(BeginTakeActionEvent E)
		{
			if (IsOutsideDuringTheDay())
			{
				AddPlayerMessage("{{W|IT BURNS!!!}}");
				ParentObject.TakeDamage(WikiRng.Next(5, 10), null, null);
			}
			if (!Immune && Options.GetOptionBool(ModOptions.FIRE) && ParentObject.LocalCells(out var cells))
			{
				if (ParentObject.IsPlayer() || !Rotschrek)
					SearchForFire(cells);
			}
			return base.HandleEvent(E);
		}
		#endregion

		#region [Debuff] Silver Ailment 
		void SilverAilment(GameObject obj)
		{
			UI.Popup.Show("{{Y|IT BURNS!!!}}");
			FakeDrop(obj, obj.Blueprint, false);
			ParentObject.TakeDamage(WikiRng.Next(1, 10), obj, null);
		}
		#endregion

		#region [Debuff] Rotschrek

		public override bool HandleEvent(EffectRemovedEvent E)
		{
			if (E.Effect.GetType() == typeof(Terrified)) //tried to match by effect.Object, but it always shows up null
				Rotschrek = false;
			else if (E.Effect.GetType() == typeof(Blaze_Tonic))
				Immune = false;
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(EffectAppliedEvent E)
		{
			if (E.Effect.GetType() == typeof(Blaze_Tonic))
			{
				Immune = true;
				if (Rotschrek)
					ParentObject.RemoveEffect<Terrified>();
			}
			return base.HandleEvent(E);
		}


		public override bool HandleEvent(TookDamageEvent E)
		{
			if (E.Object == ParentObject && E.Damage.Attributes.Contains("Fire"))
			{
				Panic(E?.Actor, true);
				E.Damage.Amount *= 2;
			}
			return base.HandleEvent(E);
		}
		void SearchForFire(List<Cell> cells)
		{
			foreach (var cell in cells)
			{
				var obj = cell.Objects.FirstOrDefault(FireyObject);
				if (obj != null && obj.PhaseMatches(ParentObject) && ConsiderFlight(obj))
				{
					Panic(obj, true);
					return;
				}
			}
		}

		bool ConsiderFlight(GameObject obj) //because you cannot make physical attacks (aside from swoop) if flight is not synced i dont care if you are adjacent to eachother
		{                                   //i may have to implement a caveat for swoop
			return obj.IsFlying == ParentObject.IsFlying;
		}

		bool FireyObject(GameObject obj)
		{                                                                       //Temporarily disabled until I make it slightly more complex with a timer
			return obj.IsAflame() || Flamelike($"{obj}") || LitTorch(obj); //|| (obj.Blueprint != "Campfire" && obj.HasPart<AnimatedMaterialFire>());
		}

		// bool HoldingFlamingObject(GameObject obj)
		// {
		// 	return obj.HasEquippedItem(x => LitTorch(x) || x.IsAflame());
		// }
		bool Flamelike(string obj) =>
		 obj switch
		 {
			 "LavaPuddle" or "SmallLavaPuddle" or "LavaPool" or "Shimmering Heat" => true,
			 _ => false
		 };

		bool LitTorch(GameObject obj) //im pretty sure this cannot actually happen (cannot drop lit torches) but ive included it anyways
		{
			if (obj.Blueprint == "Torch" && Options.GetOptionBool(ModOptions.TORCH))
			{
				LightSource source = obj.GetPart<LightSource>(); //private field in TorchProperties, but accessible thru the PartsList, no reflection required
				if (source.Lit)
				{     //thanks for parts lists, developers!
					return true;
				}
			}
			return false;
		}

		void Panic(GameObject FireSource, bool showmessage)
		{
			Rotschrek = true;
			Capabilities.AutoAct.Interrupt();
			if (showmessage)
			{
				if (ParentObject.IsPlayer())
				{
					AddPlayerMessage("{{R|ROTSCHREK!!!}}");
				}
			}
			if (FireSource == null)
				ParentObject.ApplyEffect(new Terrified(WikiRng.Next(5, 10), ParentObject.CurrentCell, true));
			else
				ParentObject.ApplyEffect(new Terrified(WikiRng.Next(5, 10), FireSource, false));
		}

		public void FakeDropRotschrek(GameObject Item)
		{
			TryLight(FakeDrop(Item, "Torch"));
		}
		void TryLight(GameObject Object)
		{
			var Part = Object.GetPart<TorchProperties>();
			Part.Light();
			if (!Part.IsUnlightableBecauseOfLiquidCovering() && !Part.IsUnlightableBecauseOfSubmersion())
			{
				if (ParentObject.IsPlayer())
					Popup.Show("{{R|ROTSCHREK!!!}}");
				Panic(Object, false);
			}
			else
				Part.Extinguish();
		}

		#endregion

		#region Structural/Helpers

		GameObject FakeDrop(GameObject Item, string blueprint, bool accessInv = true)
		{
			Item.Obliterate();
			if (accessInv)
			{
				Item.ForceUnequip(true);
				ParentObject.Inventory.RemoveObjectFromInventory(Item);
			}
			return ReplaceObject(blueprint);
		}
		GameObject ReplaceObject(string blueprint)
		{
			GameObject replacement = GameObject.Create(blueprint);
			ParentObject.CurrentCell.AddObject(replacement);
			DidXToY("drop", replacement, null, null, null, null, null, null, UseFullNames: false, IndefiniteSubject: false, IndefiniteObject: true);
			return replacement;
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
		public bool HasFangs() => FangsObject is not null && ParentObject.HasBodyPart(BODYPART_TYPE);
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

		#endregion

		#region Update
		public override bool HandleEvent(AfterPlayerBodyChangeEvent E)
		{
			if (DeathHandler.Security())
			{
				GameObject player = DeathHandler.Player;
				if (E.NewBody != player && E.NewBody.IsVampire()) //will throw errors on gamestart
				{
					string version = player.GetStringProperty(Flags.Mod.GAMEOBJECT_VERSION_TAG);
					E.NewBody.SetStringProperty(Flags.Mod.GAMEOBJECT_VERSION_TAG, version);
					if (player.TryGetStringProperty(Flags.Mod.OLD_SAVE, out var oldSave))
					{
						E.NewBody.SetStringProperty(Flags.Mod.OLD_SAVE, oldSave);
					}
				}
				if (E.OldBody != player && (E.OldBody?.IsVampire() ?? false))
				{
					E.OldBody.RemoveStringProperty(Flags.Mod.GAMEOBJECT_VERSION_TAG);
					E.OldBody.RemoveStringProperty(Flags.Mod.OLD_SAVE);
				}
			}
			return base.HandleEvent(E);
		}

		//bug:
		//AI vampires wont get access to the new corpse type unless the player is a vampire that can update them, because only player vampires can request zone updates when entering zones
		public override bool HandleEvent(EnteringZoneEvent E)
		{
			Zone zone = E.Cell.ParentZone;
			if (zone.TryGetZoneProperty(Flags.Mod.ZONE_VERSION_TAG, out string result)) //to prevent repeated sifting of zones already updated in old saves.
			{
				if (result != Mod.VERSION)
					Update(zone);
			}
			else
				Update(zone);
			return base.HandleEvent(E);
		}
		static void Update(Zone zone)
		{
			zone.CombatObjects(x => x.IsVampire() && !x.IsPlayer()).SafeForEach(x => VampirismSys.Update.Update.TryUpdateNPC(x));
			zone.SetZoneProperty(Flags.Mod.ZONE_VERSION_TAG, Mod.VERSION);
		}
		#endregion

		#region Mutation functionality

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
					FeedAbility.Initialize(Target);
			}
			return base.HandleEvent(E);
		}

		bool AITargetting(AIGetOffensiveAbilityListEvent E)
		 =>
			E.Distance <= 1
			&& HasFangs()
			&& IsMyActivatedAbilityAIUsable(FangsActivatedAbilityID)
			&& E.Target.CurrentCell?.GetCombatTarget(E.Actor) != null
			&& !E.Actor.Incap(false)
			&& !E.Target.HasEffect<VampiresKiss>() //this is so that they prefer to try and kill you instead of your victim
			&& Checks.AttackableForAI(E.Target);

		public override string GetDescription() => "You feed on the blood of living creatures.";
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
			stats.CollectCooldownTurns(MyActivatedAbility(ActivatedAbilityID), Feed.COOLDOWN);
		}
		public override string GetLevelText(int Level)
		=> "Feeds {{rules|" + GetDamageDice() + "}} blood per round, for up to {{rules|5}} rounds.\n" +
		"Success roll: {{rules|mutation rank}} or Agility mod (whichever is higher) + character level + 1d8 VS. Defender DV + character level.\n";

		public override IPart DeepCopy(GameObject Parent, Func<GameObject, GameObject> MapInv)
		{
			Vampirism obj = base.DeepCopy(Parent, MapInv) as Vampirism;
			obj.FangsObject = null;
			return obj;
		}

		public override bool Mutate(GameObject GO, int Level = 1)
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
			if (!TryGetRegisteredSlot(body, BODYPART_TYPE, out BodyPart BodyPart))
			{
				BodyPart = body.GetFirstPart(BODYPART_TYPE);
				if (BodyPart is not null)
					RegisterSlot(BODYPART_TYPE, BodyPart);
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

		#endregion
	}
}
