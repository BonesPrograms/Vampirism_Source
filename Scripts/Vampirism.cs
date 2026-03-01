using System;
using ConsoleLib.Console;
using XRL.UI;
using XRL.World.Anatomy;
using XRL.World.Effects;
using Nexus.Properties;
using Nexus.Registry;
using Nexus.Core;
using Nexus.Attack;
using Nexus.Rules;
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
		public const string BodyPartType = "Face";
		public Guid FangsActivatedAbilityID = Guid.Empty;
		public GameObject FangsObject; //your actual fangs
		FeedCommand _FeedCommand;
		public FeedCommand FeedCommand => _FeedCommand ??= new FeedCommand(this);
		public string ManagerID => ParentObject.ID + "::Vampiric Fangs"; //i never really researched managerid yet. i assume that the fangs object counts as a bodypart and this is its manager
		public override bool CanSelectVariant => false;
		public override bool UseVariantName => false;
		public bool GameOver = default;
		public int bloodycounter = default;
		public bool Rotschrek => _Rotschrek;
		bool Immune = default;
		bool _Rotschrek = default;
		//bool AlreadyBurnedWithSilver = default;
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

		#region WantEvent
		public override bool WantEvent(int ID, int cascade)
		{
			if (ID == AfterPlayerBodyChangeEvent.ID || ID == SingletonEvent<BeginTakeActionEvent>.ID || ID == PooledEvent<CommandEvent>.ID || ID == AIGetOffensiveAbilityListEvent.ID || ID == PooledEvent<AfterDismemberEvent>.ID || ID == SingletonEvent<BeforeAbilityManagerOpenEvent>.ID)
				return true;
			if (ID == SingletonEvent<EndTurnEvent>.ID)
				return bloodycounter > 0 && HasFangs();
			if (ID == BeforeRenderEvent.ID)
				return CheckNightbeast();
			if (ID == TookDamageEvent.ID)
				return CheckFireOption() && !Immune;
			if (ID == EffectAppliedEvent.ID || ID == EffectRemovedEvent.ID)
				return Rotschrek || Immune;
			if (ID == EnteringZoneEvent.ID)
				return ParentObject.HasStringProperty(FLAGS.OLD_SAVE) && ParentObject.IsPlayer();
			if (ID == EquipperEquippedEvent.ID || ID == TookEvent.ID)
				return The.Game.Turns > 0;//will fire and go crazy if you spawn with silver items in your inventory or torches
			return base.WantEvent(ID, cascade);
		}

		#endregion

		#region Nightbeast
		public override bool HandleEvent(BeforeRenderEvent E)
		{
			AddLight(21, LightLevel.Dimvision);
			return base.HandleEvent(E);
		}

		#endregion


		#region Fluff

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
		#endregion

		#region [Debuff] Combined Event Handlers
		public override bool HandleEvent(EquipperEquippedEvent E)
		{
			if (E.Item.Blueprint == "Torch")
			{
				if (CheckFireOption() && Options.GetOptionBool(OPTIONS.TORCH)) //this event runs before the game loads and was causing serious hangups/crashes
				{                                                       //in tandem with the VampirismStartGame mutator that deletes torches
					var Torch = E.Item.GetPart<TorchProperties>();      //just a mess of null errors
					if (!Torch.IsUnlightableBecauseOfLiquidCovering())
						FakeDropRotschrek(E.Item);
					E.RequestInterfaceExit();
					return false;
				}
			}
			if (E.Item.IsSilver() && Options.GetOptionBool(OPTIONS.SILVER))
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
			if (E.Item.IsSilver() && Options.GetOptionBool(OPTIONS.SILVER))
			{
				Popup.Show(nameof(TookEvent));
				SilverAilment(E.Item);
				E.RequestInterfaceExit();
				return false;
			}
			if (CheckFireOption() && FireyObject(E.Item))
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
			if (CheckNightbeast() && IsDay() && (ParentObject.CurrentZone?.IsOutside() ?? false))
			{
				AddPlayerMessage("{{W|IT BURNS!!!}}");
				ParentObject.TakeDamage(WikiRng.Next(5, 10), null, null);
			}
			if (!Immune && CheckFireOption() && ParentObject.LocalCells(out var cells))
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
				_Rotschrek = false;
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
				if (obj != null)
				{
					Panic(obj, true);
					return;
				}
			}
		}

		bool FireyObject(GameObject obj)
		{
			return obj.IsAflame() || Flamelike($"{obj}") || (obj.Blueprint != "Campfire" && obj.HasPart<AnimatedMaterialFire>()) || LitTorch(obj);
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
			if (obj.Blueprint == "Torch" && Options.GetOptionBool(OPTIONS.TORCH))
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
			_Rotschrek = true;
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


		// public void ForceDrop(GameObject Object)
		// {
		// //	EquipmentAPI.DropObject(Object);
		// 	//	DidXToY("drop", Object, null, null, null, null, null, null, UseFullNames: false, IndefiniteSubject: false, IndefiniteObject: true);
		// 	//ParentObject.CurrentCell.AddObject(Object);
		// }

		//FAKEDROP EXPLANATION:
		//the reason we do this so funky is because for some reason, the torch was not considered valid, after being removed from inventory and forceunequipped, we could not add it to the players cell
		//this does not occur with silver ailment, which actually places the original object on the ground, only torches seem to be invalid

		//furthermore: when working with silver ailment, i found that using EquipmentAPI.DropObject, ForceUnequip, RemoveObjectFromInventory (any variation of these) would fire the TookEvent
		//at least 2-3 times when equipping, before actually firing the EquipperEquippedEvent (??? it doesnt haoppen in FakeDrop but for some reason the EquippedEvent-TookEvent chain fires it repeatedly)
		//this resulted in multiple silver ailment stacks
		//because i could not find any silver mods on the workshop, and all silver items are default blueprints, i figured it wouldnt be an issue to destroy and replace
		//considering that vampires cannot have silver anyways, it is unlikely it will destroy your favorite nugget that you painted with smiley faces

		//because I did not feel like doing an entire DeepCopy for this, though if it ever came down to it...

		//- Did a lot of experimenting with EquipmentAPI.DropObject, ForceUnequip, Unequip and Remove, RemoveFromInv (then add to cell), and the end result was the TookEvent firing 2-3 times in a row if done thru EquippedEvent, but these
		//fire before equippedevent even fires

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

		#endregion

		#region QuickOptionCheckers

		bool CheckNightbeast()
		{
			return ParentObject.IsPlayer() && Options.GetOptionBool(OPTIONS.NIGHTBEAST) && !ParentObject.OnWorldMap();
		}

		static bool CheckFireOption()
		{
			return Options.GetOptionBool(OPTIONS.FIRE);
		}


		#endregion

		#region Update
		public override bool HandleEvent(AfterPlayerBodyChangeEvent E) //potential issue here:
		{                                                               //players who are NOT a vampire and are playing old saves will not be able to use vampiric spells when dominating if the option is enabled.
			if (E.NewBody.IsVampire())                                  //because only players with vampire parts can request an update
			{
				//though this can be fixed by saving/loading as the dominatee
				Nexus.Update.Update.Spells(E.NewBody);                  //ALSO vampires wont get access to the new corpse type unless the player is a vampire that can update them
				if (E.OldBody?.HasStringProperty(FLAGS.OLD_SAVE) ?? false)
					E.NewBody.SetStringProperty(FLAGS.OLD_SAVE, null);
			}
			return base.HandleEvent(E);
		}
		public override bool HandleEvent(EnteringZoneEvent E)
		{
			Zone zone = E.Cell.ParentZone;
			if (zone.TryGetZoneProperty(FLAGS.MOD.VERSION_TAG, out string result)) //to prevent repeated sifting of zones already updated in old saves
			{
				if (result != MOD.VERSION)
					Update(zone);
			}
			else
				Update(zone);
			return base.HandleEvent(E);
		}
		static void Update(Zone zone)
		{
			zone.CombatObjects(x => x.IsVampire() && !x.IsPlayer()).ForEach(x => Nexus.Update.Update.DoUpdate(x));
			zone.SetZoneProperty(FLAGS.MOD.VERSION_TAG, MOD.VERSION);
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
					FeedCommand.Initialize(Target);
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
			&& !E.Target.HasEffect<Vampires_Kiss>() //this is so that they prefer to try and kill you instead of your victim
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
			stats.CollectCooldownTurns(MyActivatedAbility(ActivatedAbilityID), FEED.COOLDOWN);
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

		#endregion
	}
}
