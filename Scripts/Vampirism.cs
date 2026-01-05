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
		public override string GetDescription() => "You feed on the blood of living creatures.";
		public static int GetCooldown(int Level) => FEED.COOLDOWN;
		public override bool ChangeLevel(int NewLevel) => base.ChangeLevel(NewLevel);
		public static string GetDamageDice(int Level)
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
			stats.Set("HP", GetDamageDice(Level) + " blood");
			stats.CollectCooldownTurns(MyActivatedAbility(ActivatedAbilityID), GetCooldown(Level));
		}
		public override string GetLevelText(int Level)
		=> "Feeds {{rules|" + GetDamageDice(Level) + "}} blood per round, for up to {{rules|5}} rounds.\n" +
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
				case Events.GAMEOVER:
					GameOver = true;
					break;
				case Events.WISH_HUMANITY:
					GameOver = false;
					break;
				case "LungedTarget":
					if (HasFangs() && !ParentObject.Body.IsPrimaryWeapon(FangsObject))
						Bite(FangsObject, E.GetGameObjectParameter("Defender"));
					break;
			}
			return base.FireEvent(E);
		}
		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade) && ID != PooledEvent<CommandEvent>.ID && ID != AIGetOffensiveAbilityListEvent.ID && ID != SingletonEvent<EndTurnEvent>.ID && ID != PooledEvent<AfterDismemberEvent>.ID)
				return ID == SingletonEvent<BeforeAbilityManagerOpenEvent>.ID;
			return true;
		}

		public override bool HandleEvent(AfterDismemberEvent E)
		{
			if (E.Part?.Name is "face")
			{
				if (E.Actor is not null)
				{
					if (E.Object.IsPlayer())
						Popup.Show("You are defanged by " + E.Actor.t() + "!");
					else
						AddPlayerMessage(E.Object + " is defanged by " + E.Actor.t() + "!");
				}
				else
					Popup.Show("You defang yourself!");
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(EndTurnEvent E)
		{
			if (bloodycounter > 0 && HasFangs())
			{
				if (WikiRng.Next(1, 10) == 10 && !Scan.ReturnProperty(ParentObject, Flags.FEED))
				{
					AddPlayerMessage("{{r|Blood}} drips from your fangs.");
					if (!ParentObject.OnWorldMap())
						ParentObject.CurrentCell?.AddObject("FangBloodDrop");
				}
				if (bloodycounter++ == 25)
				{
					FangsObject.DisplayName = "fangs";
					bloodycounter = 0;
				}
			}
			return base.HandleEvent(E);
		}
		public override bool HandleEvent(BeforeAbilityManagerOpenEvent E)
		{
			DescribeMyActivatedAbility(FangsActivatedAbilityID, CollectStats);
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
				Cell cell = PickDirection(ABILITY_NAME);
				if (TryGetTarget(cell, out GameObject Target))
					new HandleCommand(Target, this, new Bite(ParentObject, Target)).Initialize();
				else if (ParentObject.IsPlayer() && cell is not null)
					Popup.ShowFail(cell.HasObjectWithPart(nameof(Combat)) ? "There is no one there you can feed from." : "There is no one there to feed from.");
			}
			return base.HandleEvent(E);
		}
		bool TryGetTarget(Cell Cell, out GameObject pick)
		{
			pick = Cell?.GetCombatTarget(ParentObject);
			return pick is not null && pick != ParentObject;
		}
		bool Prerequisites()
		{
			if (!HasFangs())
			{
				ParentObject.ShowFailure("You have been defanged and cannot feed right now.");
				return false;
			}
			if (!ParentObject.CanMoveExtremities(ABILITY_NAME, ShowMessage: true))
				return false;
			if (Scan.Incap(ParentObject, false))
			{
				ParentObject.ShowFailure("You are incapacitated and cannot feed right now.");
				return false;
			}
			return true;

		}
		public bool HasFangs() => FangsObject is not null && ParentObject.HasBodyPart(BodyPartType);
		public void Bite(GameObject Fangs, GameObject Defender, bool Auto = false)
		 =>
			Combat.MeleeAttackWithWeapon
			(ParentObject, Defender, Fangs, ParentObject.GetBodyPartByManager(ManagerID), Auto ? "Autohit,Autopen,Biting" : "Biting");
		public void BiteActivate(GameObject Target)
		{
			if (ParentObject.IsPlayer())
				DidX("sink your fangs into", Target.the + Target.ShortDisplayName + "'s neck", "!", null, null, ParentObject);
			else
				DidX("sinks " + ParentObject.its + " fangs into", Target.the + Target.ShortDisplayName + "'s neck", "!", null, null, ParentObject);
			Bite(FangsObject, Target, Auto: true);
			Target?.Bloodsplatter();
		}
		bool AITargetting(AIGetOffensiveAbilityListEvent E)
		 =>
			E.Distance <= 1
			&& HasFangs()
			&& IsMyActivatedAbilityAIUsable(FangsActivatedAbilityID)
			&& !Scan.Incap(E.Actor, false)
			&& !E.Target.HasEffect<Vampires_Kiss>()
			&& !E.Target.IsFlying
			&& !E.Target.IsFrozen()
			&& !E.Target.IsInStasis()
			&& Scan.Applicable(E.Target);

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
