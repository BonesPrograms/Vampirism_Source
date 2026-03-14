using XRL.World;
using XRL.World.Effects;
using XRL.World.Parts;
using System.Collections.Generic;
using System;
using XRL.UI;
using XRL.World.Parts.Mutation;
using System.Linq;
using System.Collections;
using static XRL.World.Cell;
using XRL.World.Anatomy;

namespace VampirismSys.Core
{


	internal static class QudExtensions
	{
		readonly static Type[] UnawareFX =
		{
			typeof(VampiresKiss), typeof(KO), typeof(Stun), typeof(Paralyzed), typeof(Asleep), typeof(Exhausted)
		};

		#region Properties
		internal static bool TryGetZoneProperty(this Zone zone, string property, out string result)
		{
			result = zone.GetZoneProperty(property);
			return !result.IsNullOrEmpty();
		}

		/// <summary>
		/// Returns true/false values from object string properties. Default true.
		/// </summary>
		internal static bool CheckFlag(this GameObject theObject, string flag1, string flag2) => theObject.CheckFlag(flag1) || theObject.CheckFlag(flag2);

		/// <summary>
		/// Returns true/false values from object string properties. Default true.
		/// </summary>
		internal static bool CheckFlag(this GameObject theObject, string flag) => theObject.PropertyEquals(flag, bool.TrueString, StringComparison.OrdinalIgnoreCase);
		//we use OrdinalIgnore cause I dont care if it says "trUe" or "true" or "TRUE"

		//ordinal is apparently the default for string.Equals
		internal static bool PropertyEquals(this GameObject Object, string key, string value, StringComparison comparison = StringComparison.Ordinal)
		{
			if (Object.TryGetStringProperty(key, out string result))
				return result.Equals(value, comparison);
			return false;
		}
		internal static bool TryGetLongProperty(this GameObject Object, string key, string key2, out long value)
		{
			if (Object.TryGetLongProperty(key, out value) || Object.TryGetLongProperty(key2, out value))
				return true;
			return false;
		}

		internal static bool TryGetLongProperty(this GameObject Object, string property, out long value)
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

		#endregion
		/// <summary>
		/// Safe method for getting a target for an activated ability.
		/// </summary>
		/// 
		#region Target State
		internal static bool TryGetTarget(this GameObject Object, string ability, string text, out GameObject pick)
		{
			Cell Cell = Object.PickDirection(ability);
			pick = Cell?.GetCombatTarget(Object);
			bool value = pick != null && pick != Object;
			if (!value && Cell != null && Object.IsPlayer())
				Popup.ShowFail(Cell.HasObjectWithPart(nameof(Combat)) ? $"There is no one there you can {text}." : $"There is no one there to {text}.");
			return value;
		}


		/// <summary>
		/// Evaluates if the vampire is in a condition wherein they are incapable of activating Feed. Special evaluation for when frenzy is active.
		/// </summary>

		internal static bool Incap(this GameObject theVampire, bool frenzying)
		 =>
		 	theVampire != null &&
			 (theVampire.IsFrozen()
			|| theVampire.IsInStasis()
			|| Unaware(theVampire, false)
			|| (theVampire.IsConfused && frenzying) // specifically to end frenzy if confused
			|| (!theVampire.IsPlayer() && theVampire.HasEffect<StunGasStun>())) //stungasstun does not count as unawareness but does count as incapacitated only because i dont like being bitten by stun-gassed vampires. NOTE: this does nothing for True Undead, who cannot be stungassed
			|| !theVampire.CanMoveExtremities(XRL.World.Parts.Mutation.Vampirism.ABILITY_NAME);                                              //even with useenergy event, still had some bugs associated with effects and conditions that youd normally expect to end a feeding



		/// <summary>
		/// Evaluates if a target lacks awareness of their surroundings, such as stun, sleep, confusion, or paralysys.
		/// </summary>
		internal static bool Unaware(this GameObject Object, bool kissing)
		{
			if (Object.IsConfused && !Object.IsPlayer()) //normally confusion does not count as technical unawareness for the player
				return true;                            //the effect of this can be noticed in Incap()'s references; ie. feed does not end for a confused player but ends for a confused AI
			for (int i = 0; i < UnawareFX.Length; i++)
			{
				for (int x = 0; x < Object.Effects.Count; x++)
				{
					if (UnawareFX[i] == Object.Effects[x].GetType())
					{
						if (kissing && i == 0)
							continue;
						return true;
					}
				}
			}
			return false;
		}


		/// <summary>
		/// Evaluates alliance, love, and player control.
		/// </summary>
		internal static bool IsFriendly(this GameObject who, GameObject toWho)
		{
			if (toWho != null)
				return who.IsAlliedTowards(toWho) || who.IsInLoveWith(toWho) || who.InSamePartyAs(toWho) || (toWho.IsPlayer() && (who.IsPlayerControlled() || who.IsPlayerLed()));
			return false;
		}

		internal static bool IsSilver(this GameObject Object)
		{
			return Object.Blueprint.Contains("silver", StringComparison.OrdinalIgnoreCase);
		}

		internal static bool IsPolymorphed(this GameObject Object)
		{
			return Object.HasEffectDescendedFrom<BasePolymorphEffect>();
		}

		internal static bool IsVampire(this GameObject Object)
		{
			return Object.HasPart<Vampirism>();
		}

		internal static bool IsVampire(this GameObject Object, out Vampirism v)
		{
			v = Object.GetPart<Vampirism>();
			return v != null;
		}
		internal static bool IsGhoulOf(this GameObject Object, GameObject Target)
		{
			var e = Object.GetEffect<EnthralledGhoul>();
			return Target != null && (e?.IsGhoulOf(Target) ?? false);
		}
		internal static bool IsBeguiledBy(this GameObject Object, GameObject Target)
		{
			var e = Object.GetEffect<Beguiled>();
			return Target != null && e?.Beguiler == Target;
		}

		#endregion
		#region Faction

		internal static void SubtractFactionFeeling(this Brain Brain, string Faction, int Feeling)
		{
			Brain.Allegiance[Faction] -= Feeling;
		}


		internal static void AddFactionFeeling(this Brain Brain, string Faction, int Feeling)
		{
			Brain.Allegiance[Faction] += Feeling;
		}


		#endregion

		#region Mutation GameObject

		internal static T RequireMutation<T>(this GameObject Object, int level = 1) where T : BaseMutation, new()
		{
			var mutations = Object.RequirePart<Mutations>();
			if (mutations.TryGetMutation(out T obj))
				return obj;
			return mutations.AddMutation<T>(level);
		}

		internal static T AddMutation<T>(this GameObject Object, int level = 1) where T : BaseMutation, new()
		{
			var mutations = Object.GetPart<Mutations>();
			return mutations?.AddMutation<T>(level);
		}

		internal static T GetMutation<T>(this GameObject Object) where T : BaseMutation
		{
			var mutations = Object.GetPart<Mutations>();
			return mutations?.GetMutation<T>();
		}

		internal static bool TryGetMutation<T>(this GameObject Object, out T obj) where T : BaseMutation
		{
			obj = Object.GetMutation<T>();
			return obj != null;
		}

		internal static void RemoveMutation<T>(this GameObject Object) where T : BaseMutation
		{
			if (Object.TryGetPart(out Mutations part))
			{
				if (part.TryGetMutation(out T obj))
					part.RemoveMutation(obj);
			}
		}


		#endregion
		
		#region Mutations Part
		internal static T GetMutation<T>(this Mutations mutations) where T : BaseMutation
		{
			return mutations.MutationList?.FirstOrDefault(x => x.GetType() == typeof(T)) as T;
		}

		internal static bool TryGetMutation<T>(this Mutations mutations, out T obj) where T : BaseMutation
		{
			obj = mutations.GetMutation<T>();
			return obj != null;
		}

		internal static T AddMutation<T>(this Mutations mutations, int level = 1) where T : BaseMutation, new()
		{
			T obj = new();
			mutations.AddMutation(obj, level);
			return obj;
		}

		#endregion

		#region Zone/Cell
		internal static IEnumerable<GameObject> CombatObjects(this Zone zone, Func<GameObject, bool> expr = null)
		{
			for (int y = 0; y < zone.Height; y++)
			{
				for (int x = 0; x < zone.Width; x++)
				{
					var enumerable = zone.Map[x][y].CombatObjects(expr);
					foreach (var obj in enumerable)
						yield return obj;
				}
			}
		}
		internal static IEnumerable<GameObject> CombatObjects(this Cell cell, Func<GameObject, bool> expr = null)
		{
			return cell.HasCombatObject() ? cell.Objects.Where(expr == null ? x => x.IsCombatObject() : x => x.IsCombatObject() && expr(x)) : Enumerable.Empty<GameObject>();
		}

		internal static bool LocalCells(this GameObject Player, out List<Cell> cells)
		{
			cells = Player.CurrentCell?.GetLocalAdjacentCells();
			return cells != null;
		}

		#endregion

	}


	internal static class Extensions
	{
		#region IList<T>

		/// <summary>
		/// For when you dont feel like remaking your code to support a hash set. Don't use on a substantially large list.
		/// </summary>
		internal static void SafeAddReference<T>(this IList<T> obj, T add) where T : class
		{
			for (int i = 0; i < obj.Count; i++)
			{
				if (ReferenceEquals(obj[i], add))
					return;
			}
			obj.Add(add);
		}


		#endregion

		#region IEnumerable<T>

		internal static void ForEach<T>(this IEnumerable<T> objs, Action<T> action)
		{
			foreach (T obj in objs)
				action(obj);
		}

		internal static void SafeForEach<T>(this IEnumerable<T> objs, Action<T> action)
		{
			objs.ToArray().ForEach(action);
		}

		#endregion

	}
	internal static class Checks
	{
		/// <summary>
		/// Evaluates if a target is in a defenseless condition and plays unique messages for specific conditions.
		/// </summary>
		internal static bool Vulnerability(GameObject who, GameObject theVampire) //our vulnerability sheet
		{
			if (who.HasEffect<VampiresKiss>())
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

		internal static bool Prerequisites(GameObject ParentObject, string text, string text2)
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

		internal static bool AttackableForAI(GameObject Target)
		{
			return Applicable(Target) && IsNotASolidBlock(Target) && Target.IsVisible();
		}

		internal static bool IsNotASolidBlock(GameObject Target)
		{
			return !Target.IsFrozen() && !Target.IsInStasis();
		}

		internal static bool Attackable(GameObject Target, string text)
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
		/// Evaluates if a target can be fed on by a vampire. Important for any vampiric spell or vampire related feature.
		/// </summary>
		internal static bool Applicable(GameObject Victim) => !FailedSimpleChecks(Victim) && !Victim.IsWall() && !HasWrongAnatomy(Victim.Body?.Anatomy) && !Stealth.StealthCore.Inanimate(Victim);
		static bool FailedSimpleChecks(GameObject Victim) => !GameObject.Validate(ref Victim) || !Victim.IsCombatObject() || !Victim.IsOrganic || !Victim.IsAlive;

		//static bool CheckBleedLiquid(GameObject Object) => Object.TryGetStringProperty("BleedLiquid", out string result) && result is "blood-1000" or null or "";
		static bool HasWrongAnatomy(string body)
		=> body switch
		{
			null or "Star" or "Echinoid" or "Flower" or "Vine" or "Tree" or "Cactus" or "Bush" or "Ooze" or "Jelly" => true,
			_ => false
		};
	}
}