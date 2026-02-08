using XRL.World;
using XRL.World.Effects;
using XRL.World.Parts;
using System.Collections.Generic;
using System;
using XRL.UI;
using XRL.World.Parts.Mutation;
using System.Linq;
using XRL;
using System.Collections;

[HasCallAfterGameLoaded]
public static class VampirismStaticRefresh
{
	[CallAfterGameLoaded]
	public static void MyLoadGameCallback()
	{
		DeathHandler.Player = null; //ensures that the shared static Player object doesnt get drowned in the pool of objects when you swap saves
	}                               //resetting it to null starts the security chain again so it finds the right object
}                       //specifically, the player will update whenever a zone is loaded and its npcs/a Creature spawns in the zone and runs the BeforeTakeActionEvent
						//or when a DeathEvent is sent by any object

namespace Nexus.Core
{

	public static class QudExtensions
	{
		//RequiresPart (bool): if they already have the part, it returns false and does not assign obj. otherwise it returns true and assigns obj to the new part.

		//generic methods that take Type and lack the new() constraint are made to support casting reflection-created instances to abstract types using generic parameter
		/// <summary>
		/// Boolean RequirePart for Type instances, casts to T and outputs
		/// </summary>
		/// 
		public static bool TryGetZoneProperty(this Zone zone, string property, out string result)
		{
			result = zone.GetZoneProperty(property);
			return !result?.IsNullOrEmpty() ?? false;
		}

		/// <summary>
		/// Returns true/false values from object string properties. Default true.
		/// </summary>
		public static bool CheckFlag(this GameObject theObject, string flag1, string flag2) => theObject.CheckFlag(flag1) || theObject.CheckFlag(flag2);

		/// <summary>
		/// Returns true/false values from object string properties. Default true.
		/// </summary>
		public static bool CheckFlag(this GameObject theObject, string flag) => theObject.PropertyEquals(flag, Properties.FLAGS.TRUE) || theObject.PropertyEquals(flag, Properties.FLAGS.TRUE_LEGACY);

		public static bool PropertyEquals<TValue>(this GameObject Object, string key, TValue value)
		{
			if (value is string stringProp)
			{
				if (Object.TryGetStringProperty(key, out string result))
					return result == stringProp;
			}
			else if (value is long longProp)
			{
				if (Object.TryGetLongProperty(key, out long result))
					return result == longProp;
			}
			else if (value is int integer)
			{
				if (Object.TryGetIntProperty(key, out int result))
					return result == integer;
			}
			return false;
		}

		public static bool TryGetEitherLongProperty(this GameObject Object, string key, string key2, out long value)
		{
			if (Object.TryGetLongProperty(key, out value) || Object.TryGetLongProperty(key2, out value))
				return true;
			return false;
		}

		/// <summary>
		/// Safe method for getting a target for an activated ability.
		/// </summary>
		public static bool TryGetTarget(this GameObject Object, string ability, string text, out GameObject pick)
		{
			Cell Cell = Object.PickDirection(ability);
			pick = Cell?.GetCombatTarget(Object);
			bool value = pick != null && pick != Object;
			if (!value && Cell != null && Object.IsPlayer())
				Popup.ShowFail(Cell.HasObjectWithPart(nameof(Combat)) ? $"There is no one there you can {text}." : $"There is no one there to {text}");
			return value;
		}

		/// <summary>
		/// Evaluates alliance, love, and player control.
		/// </summary>
		public static bool IsFriendly(this GameObject who, GameObject toWho)
		{
			if (toWho != null)
				return who.IsAlliedTowards(toWho) || who.IsInLoveWith(toWho) || (toWho.IsPlayer() && (who.IsPlayerControlled() || who.IsPlayerLed()));
			return false;
		}

		/// <summary>
		/// Evaluates if the vampire is in a condition wherein they are incapable of activating Feed. Special evaluation for when frenzy is active.
		/// </summary>

		public static bool Incap(this GameObject theVampire, bool frenzying)
		 =>
		 	theVampire != null &&
			 (theVampire.IsFrozen()
			|| theVampire.IsInStasis()
			|| !theVampire.CanMoveExtremities(XRL.World.Parts.Mutation.Vampirism.ABILITY_NAME)
			|| Unaware(theVampire, false)
			|| (theVampire.IsConfused && frenzying) // specifically to end frenzy if confused
			|| (!theVampire.IsPlayer() && theVampire.HasEffect<StunGasStun>())); //stungasstun does not count as unawareness but does count as incapacitated only because i dont like being bitten by stun-gassed vampires
		//even with useenergy event, still had some bugs associated with effects and conditions that youd normally expect to end a feeding

		public readonly static Type[] UnawareFX =
		{
			typeof(Vampires_Kiss), typeof(KO), typeof(Stun), typeof(Paralyzed), typeof(Asleep), typeof(Exhausted)
		};

		/// <summary>
		/// Evaluates if a target lacks awareness of their surroundings, such as stun, sleep, confusion, or paralysys.
		/// </summary>
		public static bool Unaware(this GameObject Object, bool kissing)
		{
			if (Object.IsConfused && !Object.IsPlayer()) //normally confusion does not count as technical unawareness for the player
				return true;							//the effect of this can be noticed in Incap()'s references; ie. feed does not end for a confused player but ends for a confused AI
			for (int i = 0; i < UnawareFX.Length; i++)
			{
				for (int x = 0; x < Object.Effects.Count; x++)
				{
					if (Object.Effects[x].Duration > 0 && UnawareFX[i] == Object.Effects[x].GetType())
					{
						if (kissing && i == 0)
							continue;
						return true;
					}
				}
			}
			return false;
		}
		public static bool CanBeEmbraced(this GameObject Object) => Object.CheckFlag(Properties.FLAGS.EMBRACE.EMBRACEABLE);

		public static bool IsVampire(this GameObject Object)
		{
			return Object.HasPart<Vampirism>();
		}

		public static bool IsVampire(GameObject Object, out Vampirism v)
		{
			v = Object?.GetPart<Vampirism>();
			return v != null;
		}

		public static bool IsGhoulOf(this GameObject Object, GameObject Target)
		{
			var e = Object.GetEffect<EnthralledGhoul>();
			return e?.IsGhoulOf(Target) ?? false;
		}

		public static bool IsChildeOf(this GameObject Object, GameObject Target)
		{
			var p = Object.GetPart<Fledgling>();
			return p?.IsChildeOf(Target) ?? false;
		}

		public static bool IsBeguiledBy(this GameObject Object, GameObject Target)
		{
			var e = Object?.GetEffect<Beguiled>();
			return Target != null && e?.Beguiler == Target;
		}

		public static bool Embraced(this GameObject Object)
		{
			return Object.HasEffect<Embraced>();
		}

		public static bool LocalCells(this GameObject Player, out List<Cell> cells)
		{
			cells = Player.CurrentCell?.GetLocalAdjacentCells();
			return cells != null;
		}
		public static bool RequiresPart<T>(this GameObject Object, Type type, out T obj) where T : IPart
		{
			obj = Object.GetPart<T>(type);
			if (obj != null)
				return false;
			obj = type.ConvertToClass<T>();
			if (obj != null)
				Object.AddPart(obj);
			return obj != null;
		}

		/// <summary>
		/// Boolean RequirePart for T
		/// </summary>
		public static bool RequiresPart<T>(this GameObject Object) where T : IPart, new()
		{
			T obj = Object.GetPart<T>();
			if (obj != null)
				return false;
			Object.AddPart<T>();
			return true;
		}

		/// <summary>
		/// Boolean RequirePart for instances of T
		/// </summary>
		public static bool RequiresPart<T>(this GameObject Object, T obj) where T : IPart
		{
			T part = Object.GetPart<T>();
			if (part != null)
				return false;
			Object.AddPart(obj);
			return true;
		}

		/// <summary>
		/// RequiresPart by a Type instance. 
		/// </summary>
		public static T RequirePart<T>(this GameObject Object, Type t) where T : IPart
		{
			T obj = Object.GetPart<T>(t);
			if (obj != null)
				return obj;
			obj = t.ConvertToClass<T>();
			if (obj != null)
				return Object.AddPart(obj);
			return obj;
		}

		public static T RequirePart<T>(this GameObject Object, T obj) where T : IPart
		{
			T part = Object.GetPart<T>();
			if (part != null)
				return part;
			return Object.AddPart(obj);
		}

		/// <summary>
		/// RequiresPart by a Type instance. Does not add part if Type instance does not convert to an IPart.
		/// </summary>
		public static IPart RequirePart(this GameObject Object, Type t)
		{
			return Object.RequirePart<IPart>(t);
		}

		/// <summary>
		/// GetPart by Type instance that safely casts to the generic parameter.
		/// </summary>
		public static T GetPart<T>(this GameObject Object, Type t) where T : IPart
		{
			return Object.GetPart(t) as T;
		}

		/// <summary>
		/// Compares effects by reference rather than Type.
		/// </summary>
		public static bool HasEffect<T>(this GameObject Object, T obj) where T : Effect
		{
			if (obj != null)
				for (int i = 0; i < Object.Effects.Count; i++)
					if (Object.Effects[i] == obj)
						return true;
			return false;
		}
		public static T[] PartsArrayImplenenting<T>(this GameObject Object, int capacity) where T : class
		{
			return Object.PartsList.ArrayOfObjectsImplementing<T>(capacity);
		}

		public static T[] PartsArrayDescendedFrom<T>(this GameObject Object, int capacity) where T : IPart
		{
			return Object.PartsArrayImplenenting<T>(capacity);
		}

		public static VampiricSpell[] SpellArray(this GameObject Object)
		{
			return Object.PartsArrayDescendedFrom<VampiricSpell>(VampireBuilder.VampiricSpells.Length);
		}
		public static List<T> GetPartsAndEffectsImplementing<T>(this GameObject Object, bool GetEffects) where T : class
		{
			List<T> objs = Object.GetPartsDescendedFrom<T>();
			if (GetEffects)
			{
				List<T> effects = Object.GetEffectsImplementing<T>();
				objs.AddRange(effects);
			}
			return objs;
		}
		public static List<T> GetEffectsDescendedFrom<T>(this GameObject Object) where T : Effect
		{
			return Object.GetEffectsImplementing<T>();
		}
		public static List<T> GetEffectsImplementing<T>(this GameObject Object) where T : class
		{
			List<T> fxs = new();
			for (int i = 0; i < Object.Effects.Count; i++)
				if (Object.Effects[i].Duration > 0 && Object.Effects[i] is T t)
					fxs.Add(t);
			return fxs;
		}
		public static bool TryGetLongProperty(this GameObject Object, string property, out long value)
		{
			value = default;
			if (Object.Property.TryGetValue(property, out string num))
			{
				try
				{
					value = Convert.ToInt64(num);
					return true;
				}
				catch
				{
				}
			}
			return false;
		}
	}
	public static class Extensions
	{

		/// <summary>
		/// Creates instances from a Type instance and safely casts them to the generic parameter.
		/// </summary>
		public static T ConvertToClass<T>(this Type t) where T : class
		{
			return Activator.CreateInstance(t) as T;
		}

		public static TKey[] KeyArray<TKey, TValue>(this Dictionary<TKey, TValue> source)
		{
			return source.Keys.ToArray();
		}

		public static bool ContainsValue<TItem1, TItem2>(this (TItem1, TItem2)[] array, TItem2 value)
		{
			for (int i = 0; i < array.Length; i++)
				if (array[i].Item2.Equals(value))
					return true;
			return false;
		}
		public static void Reset<TItem1, TItem2>(this (TItem1, TItem2)[] array, TItem2 value = default) where TItem2 : struct
		{
			for (int i = 0; i < array.Length; i++)
				array[i].Item2 = value;
		}
		public static void Reset<TKey, TValue>(this Dictionary<TKey, TValue> source, TValue value = default) where TValue : struct
		{
			foreach (var obj in source.KeyArray())
				source[obj] = value;
		}


		//L is is the Type parameter for your IList
		public static T[] ArrayOfObjectsImplementing<T>(this IList objects, int capacity) where T : class
		{
			T[] array = new T[capacity];
			int index = 0;
			for (int i = 0; i < objects.Count; i++)
			{
				if (objects[i] is T t)
				{
					array[index] = t;
					index++;
				}
				if (index >= array.Length)
					break;
			}
			return array;
		}

		public static int CapacityByValue(this bool[] array, bool value)
		{
			int capacity = 0;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] == value)
					capacity++;
			}
			return capacity;
		}

		public static int CapacityByValue<TItem1>(this (TItem1, bool)[] array, bool value)
		{
			int capacity = 0;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].Item2 == value)
					capacity++;
			}
			return capacity;
		}

		public static int CapacityByValue<TItem1, TItem2>(this (TItem1, TItem2)[] array, TItem2 value)
		{
			int capacity = 0;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].Item2.Equals(value))
					capacity++;
			}
			return capacity;
		}

		public static int CapacityByValue<TItem1, TItem2>(this TItem1[] array, TItem2 value)
		{
			int capacity = 0;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].Equals(value))
					capacity++;
			}
			return capacity;
		}
	}
	static class Checks
	{
		/// <summary>
		/// Evaluates if a target is in a defenseless condition and plays unique messages for specific conditions.
		/// </summary>
		public static bool Vulnerability(GameObject who, GameObject theVampire) //our vulnerability sheet
		{
			if (who.HasEffect<Vampires_Kiss>())
			{
				return true; //the string for feeding on people who have vampire's kiss is handled by the Friendly variable and Sharing() in CommandHandler
			}
			if (who.HasEffect<KO>()) //should probably add a "predator" field that ensures you are the same person that originally fed on them
			{
				if (theVampire.IsPlayer())
					IComponent<GameObject>.AddPlayerMessage("This one was tasty. You dive in for more.");
				return true;
			}
			if (who.HasEffect<Stun>())
			{
				if (theVampire.IsPlayer())
					IComponent<GameObject>.AddPlayerMessage(who.t() + " is {{C sequence|stunned}} and cannot resist.");
				return true;
			}
			if (who.HasEffect<Paralyzed>())
			{
				if (theVampire.IsPlayer())
					IComponent<GameObject>.AddPlayerMessage(who.t() + " is {{C sequence|paralyzed}} and cannot resist.");
				return true;
			}
			if (who.HasEffect<Asleep>())
			{
				if (theVampire.IsPlayer())
					IComponent<GameObject>.AddPlayerMessage(who.t() + " is {{C sequence|asleep}} and cannot resist.");
				return true;
			}
			if (who.HasEffect<Exhausted>())
			{
				if (theVampire.IsPlayer())
					IComponent<GameObject>.AddPlayerMessage(who.t() + " is too {{C sequence|tired}} to fight back.");
				return true;
			}
			if (!who.CanMoveExtremities())
			{
				if (theVampire.IsPlayer())
					IComponent<GameObject>.AddPlayerMessage(who.t() + " is restrained and cannot resist.");
				return true;
			}
			if (who.IsGhoulOf(theVampire))
			{
				if (theVampire.IsPlayer())
					IComponent<GameObject>.AddPlayerMessage(who.t() + " is {{K sequence|enthralled}} and lives to feed you.");
				return true; //only the player can enthrall, so only the player gets related freebies for it
			}
			if (who.IsInLoveWith(theVampire))
			{
				if (theVampire.IsPlayer())
					IComponent<GameObject>.AddPlayerMessage(who.t() + " is in {{love|love}} with you and offers " + who.its + " neck openly.");
				return true;
			}
			if (who.IsBeguiledBy(theVampire))
			{
				if (theVampire.IsPlayer())
					IComponent<GameObject>.AddPlayerMessage(who.t() + " presents " + who.its + " neck willingly to you.");
				return true;
			}
			if (who.IsAlliedTowards(theVampire) || (theVampire.IsPlayer() && (who.HasEffect<Proselytized>() || who.IsPlayerLed())))
			{
				if (theVampire.IsPlayer())
					IComponent<GameObject>.AddPlayerMessage(who.t() + " exposes " + who.its + " neck reluctantly to you.");
				return true;
			}
			return false;
		}

		public static bool Prerequisites(GameObject ParentObject, string text, string text2)
		{
			if (!ParentObject.CanMoveExtremities(text, ShowMessage: true))
				return false;
			if (ParentObject.Incap(false))
			{
				ParentObject.ShowFailure($"You are incapacitated and cannot {text2} right now.");
				return false;
			}
			return true;
		}

		public static bool Attackable(GameObject Target, string text)
		{
			if (!Applicable(Target)) //invalid targets are those not from the animal kingdom
			{
				Popup.ShowFail($"You cannot {text} " + Target.t() + ".");
				return false;
			}
			if (Target.IsFrozen()) //cant bite ice block people
			{
				Popup.ShowFail(Target.t() + " is frozen solid!");
				return false;
			}
			if (Target.IsInStasis())
			{
				Popup.ShowFail(Target.t() + " is in stasis.");
				return false;
			}
			return true;
		}


		/// <summary>
		/// Evaluates if a target can be fed on by a vampire.
		/// </summary>
		public static bool Applicable(GameObject Victim) //our animal kingdom evaluator. checks to see if you have proper blood
		{
			if (FailedSimpleChecks(Victim))
				return false;
			if (Victim.IsWall())
				return false;
			if (HasBadTag(Victim))
				return false;
			if (HasWrongAnatomy(Victim))
				return false;
			if (Victim.HasPart<PlantProperties>() || Victim.HasPart<FungusProperties>() || Victim.HasPart<Harvestable>())
				return false;
			return true;
		}
		static bool FailedSimpleChecks(GameObject Victim) => !GameObject.Validate(ref Victim) || !Victim.IsCombatObject() || !Victim.IsOrganic;
		static bool HasBadTag(GameObject Victim)
		   =>
			 Victim.HasTagOrProperty("Plant") //i thought this simple check would be enough but
			|| Victim.HasTagOrProperty("LivePlant")
			|| Victim.HasTagOrProperty("Fungus")
			|| Victim.HasTagOrProperty("LiveFungus")
			|| Victim.HasTagOrProperty("Plank")
			|| !Victim.HasTagOrProperty("Bleeds");

		//static bool CheckBleedLiquid(GameObject Object) => Object.TryGetStringProperty("BleedLiquid", out string result) && result is "blood-1000" or null or "";
		static bool HasWrongAnatomy(GameObject Victim)
		=>
			Victim.Body.Anatomy == "Star"
			|| Victim.Body.Anatomy == "Echinoid"
			|| Victim.Body.Anatomy == "Flower"
			|| Victim.Body.Anatomy == "Vine"
			|| Victim.Body.Anatomy == "Tree"
			|| Victim.Body.Anatomy == "Cactus"
			|| Victim.Body.Anatomy == "Bush"
			|| Victim.Body.Anatomy == "Ooze"
			|| Victim.Body.Anatomy == "Jelly";
		//who wouldve thought i needed to be so specific
	}
}