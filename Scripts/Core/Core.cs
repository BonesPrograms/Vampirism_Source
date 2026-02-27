using XRL.World;
using XRL.World.Effects;
using XRL.World.Parts;
using System.Collections.Generic;
using System;
using XRL.UI;
using XRL.World.Parts.Mutation;
using System.Linq;
using System.Collections;

namespace Nexus.Core
{


	[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
	public struct Reinterpreter<TFrom, TTo>
	{
		[System.Runtime.InteropServices.FieldOffset(0)]
		public TFrom From;

		[System.Runtime.InteropServices.FieldOffset(0)]
		public TTo To;
	}

	public static class Unsafe
	{
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static TTo As<TFrom, TTo>(TFrom from)
		{
			Reinterpreter<TFrom, TTo> r = default;
			r.From = from;
			return r.To;
		}
	}


	public static class QudExtensions
	{
		//RequiresPart (bool): if they already have the part, it returns false and does not assign obj. otherwise it returns true and assigns obj to the new part.

		//generic methods that take Type and lack the new() constraint are made to support casting reflection-created instances to abstract types using generic parameter
		/// <summary>
		/// Boolean RequirePart for Type instances, casts to T and outputs
		/// </summary>
		/// 
		/// 
		#region Properties
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
		public static bool CheckFlag(this GameObject theObject, string flag) => theObject.PropertyEquals(flag, Properties.FLAGS.TRUE);
		public static bool PropertyEquals(this GameObject Object, string key, long value)
		{
			if (Object.TryGetLongProperty(key, out long result))
				return result == value;
			return false;
		}

		public static bool PropertyEquals(this GameObject Object, string key, string value)
		{
			if (Object.TryGetStringProperty(key, out string result))
				return result == value;
			return false;
		}

		public static bool PropertyEquals(this GameObject Object, string key, int value)
		{
			if (Object.TryGetIntProperty(key, out int result))
				return result == value;
			return false;
		}

		public static bool TryGetLongProperty(this GameObject Object, string key, string key2, out long value)
		{
			if (Object.TryGetLongProperty(key, out value) || Object.TryGetLongProperty(key2, out value))
				return true;
			return false;
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

		#endregion
		/// <summary>
		/// Safe method for getting a target for an activated ability.
		/// </summary>
		/// 
		#region Target state
		public static bool TryGetTarget(this GameObject Object, string ability, string text, out GameObject pick)
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

		public static bool Incap(this GameObject theVampire, bool frenzying)
		 =>
		 	theVampire != null &&
			 (theVampire.IsFrozen()
			|| theVampire.IsInStasis()
			|| Unaware(theVampire, false)
			|| (theVampire.IsConfused && frenzying) // specifically to end frenzy if confused
			|| (!theVampire.IsPlayer() && theVampire.HasEffect<StunGasStun>())) //stungasstun does not count as unawareness but does count as incapacitated only because i dont like being bitten by stun-gassed vampires
			|| !theVampire.CanMoveExtremities(XRL.World.Parts.Mutation.Vampirism.ABILITY_NAME);                                              //even with useenergy event, still had some bugs associated with effects and conditions that youd normally expect to end a feeding

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
				return true;                            //the effect of this can be noticed in Incap()'s references; ie. feed does not end for a confused player but ends for a confused AI
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


		/// <summary>
		/// Evaluates alliance, love, and player control.
		/// </summary>
		public static bool IsFriendly(this GameObject who, GameObject toWho)
		{
			if (toWho != null)
				return who.IsAlliedTowards(toWho) || who.IsInLoveWith(toWho) || who.InSamePartyAs(toWho) || (toWho.IsPlayer() && (who.IsPlayerControlled() || who.IsPlayerLed()));
			return false;
		}

		public static bool IsSilver(this GameObject Object)
		{
			return Object.Blueprint.Contains("silver", StringComparison.OrdinalIgnoreCase);
		}

		public static bool IsInBatForm(this GameObject Object)
		{
			return Object.Blueprint == "Bat";
		}

		public static bool IsVampire(this GameObject Object)
		{
			return Object.HasPart<Vampirism>();
		}

		public static bool IsVampire(GameObject Object, out Vampirism v)
		{
			v = Object.GetPart<Vampirism>();
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
			var e = Object.GetEffect<Beguiled>();
			return Target != null && e?.Beguiler == Target;
		}


		#endregion

		#region IPart
		public static bool RequiresPart<T>(this GameObject Object, Type type, out T obj) where T : IPart
		{
			obj = Object.GetPart<T>(type);
			if (obj != null)
				return false; //very specific use case - i have parts that run initialization methods after adding, but you wouldnt want to run them twice
			obj = type.InstanceAs<T>(); //merely checking if the obj != null isnt sufficient for knowing if its new or old
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
		/// RequiresPart by a Type instance. 
		/// </summary>
		public static T RequirePart<T>(this GameObject Object, Type t) where T : IPart
		{
			T obj = Object.GetPart<T>(t);
			if (obj != null)
				return obj;
			obj = t.InstanceAs<T>();
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
		/// GetPart by Type instance that casts to the generic parameter. Explodes if the type does not convert.
		/// </summary>
		public static T GetPart<T>(this GameObject Object, Type t) where T : IPart
		{
			return (T)Object.GetPart(t);
		}

		public static T[] PartsArrayImplenenting<T>(this GameObject Object, int capacity) where T : class
		{
			return Object.PartsList.ObjectsImplementing<T>(capacity);
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

		#endregion

		#region Effect
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

		#endregion

		#region Mutation

		public static T RequireMutation<T>(this GameObject Object, int level = 1) where T : BaseMutation, new()
		{
			var mutations = Object.RequirePart<Mutations>();
			if (mutations.TryGetMutation(out T obj))
				return obj;
			return mutations.AddMutation<T>(level);
		}

		public static T AddMutation<T>(this GameObject Object, int level = 1) where T : BaseMutation, new()
		{
			var mutations = Object.GetPart<Mutations>();
			return mutations?.AddMutation<T>(level);
		}

		public static T GetMutation<T>(this GameObject Object) where T : BaseMutation
		{
			var mutations = Object.GetPart<Mutations>();
			return mutations?.GetMutation<T>();
		}

		public static bool TryGetMutation<T>(this GameObject Object, out T obj) where T : BaseMutation
		{
			obj = Object.GetMutation<T>();
			return obj != null;
		}

		public static T GetMutation<T>(this Mutations mutations) where T : BaseMutation
		{
			if (mutations.MutationList != null)
			{
				for (int i = 0; i < mutations.MutationList.Count; i++)
				{
					var mutation = mutations.MutationList[i];
					if (mutation.Name == typeof(T).Name)
					{
						return (T)mutation;
					}
				}
			}
			return null;
		}

		public static bool TryGetMutation<T>(this Mutations mutations, out T obj) where T : BaseMutation
		{
			obj = mutations.GetMutation<T>();
			return obj != null;
		}

		public static T AddMutation<T>(this Mutations mutations, int level = 1) where T : BaseMutation, new()
		{
			T obj = new();
			mutations.AddMutation(obj, level);
			return obj;
		}
		//	obj.Mutate(mutations.ParentObject);
		public static void RemoveMutation<T>(this GameObject Object) where T : BaseMutation
		{
			if (Object.TryGetPart(out Mutations part))
			{
				if (part.TryGetMutation(out T obj))
					part.RemoveMutation(obj);
			}
		}

		#endregion

		#region Zone/Cell
		public static List<GameObject> ListCombatObjects(this Zone zone, Func<GameObject, bool> expr)
		{
			List<GameObject> list = new();
			zone.ForEachCombatObject(x => { if (expr(x)) list.Add(x); });
			return list;
		}
		public static int ObjectCount(this Zone zone, Func<GameObject, bool> expr)
		{
			int count = 0;
			zone.ForEachCombatObject(x => { if (expr(x)) count++; });
			return count;
		}
		public static void ForEachCombatObject(this Zone zone, Action<GameObject> action)
		{
			void func(GameObject x) { if (x.IsCombatObject()) action(x); }
			zone.Mapper(x => { if (x.HasObjectWithPart(nameof(Combat))) x.Objects.ForEach(func); });
		}
		public static void Mapper(this Zone zone, Action<Cell> action)
		{
			for (int y = 0; y < zone.Height; y++)
				for (int x = 0; x < zone.Width; x++)
					action(zone.Map[x][y]);
		}
		public static bool LocalCells(this GameObject Player, out List<Cell> cells)
		{
			cells = Player.CurrentCell?.GetLocalAdjacentCells();
			return cells != null;
		}

		#endregion

		#region Faction/Stat

		public static void SubtractFactionFeeling(this Brain Brain, string Faction, int Feeling)
		{
			if (Brain.Allegiance.ContainsKey(Faction))
				Brain.Allegiance[Faction] -= Feeling;
		}


		public static void AddFactionFeeling(this Brain Brain, string Faction, int Feeling)
		{
			Brain.Allegiance[Faction] += Feeling;
		}

		public static void SetBaseStat(this GameObject obj, string Name, int Amount)
		{
			if (!Name.IsNullOrEmpty() && obj.Statistics != null && obj.Statistics.TryGetValue(Name, out var value))
			{
				value.BaseValue = Amount;
			}
		}

		public static bool TryGetFactionMembership(this Brain Brain, string Faction, out int value)
		{
			value = default;
			if (Brain.Allegiance.ContainsKey(Faction))
			{
				value = Brain.Allegiance[Faction];
				return true;
			}
			return false;
		}

		#endregion

		#region Serialization

		public static T[] ReadPrimitiveArray<T>(this SerializationReader Reader)
		{
			T[] array = new T[Reader.ReadInt32()];
			array.AssignEach(delegate () { return Reader.ReadPrimitive<T>(); });
			return array;
		}
		public static (T1, T2)[] ReadPrimitiveArray<T1, T2>(this SerializationReader Reader)
		{
			(T1, T2)[] array = new (T1, T2)[Reader.ReadInt32()];
			array.AssignEach(delegate () { (T1, T2) tuple = new() { Item1 = Reader.ReadPrimitive<T1>(), Item2 = Reader.ReadPrimitive<T2>() }; return tuple; });
			return array;
		}

		public static T ReadPrimitive<T>(this SerializationReader Reader)
		{
			if (typeof(T) == typeof(sbyte))
				return Unsafe.As<sbyte, T>(Reader.ReadSByte());
			else if (typeof(T) == typeof(byte))
				return Unsafe.As<byte, T>(Reader.ReadByte());
			else if (typeof(T) == typeof(short))
				return Unsafe.As<short, T>(Reader.ReadInt16());
			else if (typeof(T) == typeof(ushort))
				return Unsafe.As<ushort, T>(Reader.ReadUInt16());
			else if (typeof(T) == typeof(int))
				return Unsafe.As<int, T>(Reader.ReadInt32());
			else if (typeof(T) == typeof(uint))
				return Unsafe.As<uint, T>(Reader.ReadUInt32());
			else if (typeof(T) == typeof(long))
				return Unsafe.As<long, T>(Reader.ReadInt64());
			else if (typeof(T) == typeof(ulong))
				return Unsafe.As<ulong, T>(Reader.ReadUInt64());
			else if (typeof(T) == typeof(float))
				return Unsafe.As<float, T>(Reader.ReadSingle());
			else if (typeof(T) == typeof(double))
				return Unsafe.As<double, T>(Reader.ReadDouble());
			else if (typeof(T) == typeof(decimal))
				return Unsafe.As<decimal, T>(Reader.ReadDecimal());
			else if (typeof(T) == typeof(bool))
				return Unsafe.As<bool, T>(Reader.ReadBoolean());
			else if (typeof(T) == typeof(char))
				return Unsafe.As<char, T>(Reader.ReadChar());
			else if (typeof(T) == typeof(string))
				return (T)(object)Reader.ReadString();
			return default;
		}

		public static void WritePrimitiveArray<T>(this SerializationWriter Writer, T[] array)
		{
			Writer.Write(array.Length);
			array.ForEach(delegate (T obj) { Writer.WritePrimitive(obj); });
		}
		public static void WritePrimitiveArray<T1, T2>(this SerializationWriter Writer, (T1, T2)[] array)
		{
			Writer.Write(array.Length);
			array.ForEach(delegate ((T1, T2) obj) { Writer.WritePrimitive(obj.Item1); Writer.WritePrimitive(obj.Item2); });
			for (int i = 0; i < array.Length; i++)
			{
				Writer.WritePrimitive(array[i].Item1);
				Writer.WritePrimitive(array[i].Item2);
			}
		}

		public static void WritePrimitive<T>(this SerializationWriter Writer, T obj)
		{
			switch (obj)
			{
				case sbyte bite:
					Writer.Write(bite);
					break;
				case byte bite:
					Writer.Write(bite);
					break;
				case short shrt:
					Writer.Write(shrt);
					break;
				case ushort ushrt:
					Writer.Write(ushrt);
					break;
				case int intgr:
					Writer.Write(intgr);
					break;
				case uint uintgr:
					Writer.Write(uintgr);
					break;
				case long lng:
					Writer.Write(lng);
					break;
				case ulong ulng:
					Writer.Write(ulng);
					break;
				case float flt:
					Writer.Write(flt);
					break;
				case double dbl:
					Writer.Write(dbl);
					break;
				case decimal dcml:
					Writer.Write(dcml);
					break;
				case bool bln:
					Writer.Write(bln);
					break;
				case char chr:
					Writer.Write(chr);
					break;
				case string strng:
					Writer.Write(strng);
					break;
			}
		}

		#endregion
	}


	public static class Extensions
	{
		#region IList

		/// <summary>
		/// For when you dont feel like remaking your code to support a hash set. Don't use on a substantially large list.
		/// </summary>
		public static void SafeAddReference<T>(this IList<T> obj, T add) where T : class
		{
			for (int i = 0; i < obj.Count; i++)
			{
				if (ReferenceEquals(obj[i], add))
					return;
			}
			obj.Add(add);
		}

		public static void AssignEach<T>(this IList<T> objs, Func<T> expr)
		{
			for (int i = 0; i < objs.Count; i++)
				objs[i] = expr();
		}

		public static void AssignEachIndexed<T>(this IList<T> objs, Func<int, T> expr)
		{
			for (int index = 0; index < objs.Count; index++)
				objs[index] = expr(index);
		}
		// Indexed iterators are handy for when you are capturing an array and want to interract with objects in both arrays at the same time in one loop
		public static void IfEachCountIndexed<T>(this IList<T> objs, ref int count, int cap, Func<T, int, bool> expr)
		{
			for (int index = 0; index < objs.Count; index++)
			{
				if (expr(objs[index], index))
					count++;
				if (count >= cap)
					return;
			}
		}
		//Counting iterators are good for when you're making a smaller array out of a larger array.
		public static void IfEachCount<T>(this IList<T> objs, ref int count, int cap, Func<T, bool> expr)
		{
			for (int i = 0; i < objs.Count; i++)
			{
				if (expr(objs[i]))
					count++;
				if (count >= cap)
					return;
			}
		}
		public static void IfEachCount(this IList objs, ref int count, int cap, Func<object, bool> expr) //copy pasted this one specifically for ObjectsImplementing
		{
			for (int i = 0; i < objs.Count; i++)
			{
				if (expr(objs[i]))
					count++;
				if (count >= cap)
					return;
			}
		}

		// public static bool IfEachReturnIndexed<T>(this IList<T> objs, Func<T, int, bool> expr)
		// {
		// 	for (int index = 0; index < objs.Count; index++)
		// 	{
		// 		if (expr(objs[index], index))
		// 			return true;
		// 	}
		// 	return false;
		// }
		public static bool IfEachReturn<T>(this IList<T> objs, Func<T, bool> expr)
		{
			for (int i = 0; i < objs.Count; i++)
			{
				if (expr(objs[i]))
					return true;
			}
			return false;
		}

		public static void IfEachBreak<T>(this IList<T> objs, Func<T, bool> expr)
		{
			for (int i = 0; i < objs.Count; i++)
				if (expr(objs[i]))
					return;
		}

		public static void ForEach<T>(this IList<T> objs, Action<T> action)
		{
			for (int i = 0; i < objs.Count; i++)
				action(objs[i]);
		}

		public static int ObjectCount<T>(this IList<T> objs, Func<T, bool> expr)
		{
			int count = 0;
			for (int i = 0; i < objs.Count; i++)
				if (expr(objs[i]))
					count++;
			return count;
		}
		#endregion

		#region Type
		public static T InstanceAs<T>(this Type t) where T : class
		{
			return (T)Activator.CreateInstance(t);
		}
		#endregion

		#region IDictionary

		public static (T1, T2)[] TupleArray<T1, T2>(this IDictionary<T1, T2> dic)
		{
			(T1, T2)[] array = new (T1, T2)[dic.Count];
			int index = 0;
			dic.ForEach(delegate (KeyValuePair<T1, T2> obj) { array[index].Item1 = obj.Key; array[index].Item2 = obj.Value; index++; });
			return array;
		}

		public static void ForEach<TKey, TValue>(this IDictionary<TKey, TValue> objs, Action<KeyValuePair<TKey, TValue>> action)
		{
			foreach (var obj in objs)
				action(obj);
		}
		public static bool AnyDoesntEqual<TKey, TValue>(this IDictionary<TKey, TValue> objs, TValue value) where TValue : IEquatable<TValue>
		{
			return objs.IfEachReturn(x => !x.Value.Equals(value));
		}

		public static bool IfEachReturn<TKey, TValue>(this IDictionary<TKey, TValue> dic, Func<KeyValuePair<TKey, TValue>, bool> expr)
		{
			foreach (var obj in dic)
			{
				if (expr(obj))
					return true;
			}
			return false;
		}
		public static KeyValuePair<TKey, TValue> PickFirst<TKey, TValue>(this IDictionary<TKey, TValue> dic, Func<KeyValuePair<TKey, TValue>, bool> expr)
		{
			foreach (var obj in dic)
			{
				if (expr(obj))
					return obj;
			}
			return default;
		}

		//you should ensure your dictionary has a count > 0 before using this. it does not check on its own because i expect you to send in something like a Min() which requires you to check before using anyways
		public static KeyValuePair<TKey, TValue> PickFirstEqualTo<TKey, TValue>(this IDictionary<TKey, TValue> obj, TValue value) where TValue : IEquatable<TValue> //similar to LINQ First, get first keyvalue == value
		{
			return obj.PickFirst(delegate (KeyValuePair<TKey, TValue> pair) { return pair.Value.Equals(value); });
		}
		public static TKey[] KeyArray<TKey, TValue>(this IDictionary<TKey, TValue> source)
		{
			return source.Keys.ToArray();
		}

		#endregion

		#region T[]
		public static void CheckEach<T>(this (T, bool)[] array, Func<T, bool> expr)
		{
			for (int i = 0; i < array.Length; i++)
				array[i].Item2 = expr(array[i].Item1);
		}
		// public static bool ContainsElement<T>(this T[] array, T value) where T : IEquatable<T>
		// {
		// 	return array.IfEachReturn(x => x.Equals(value));
		// }
		public static bool ContainsElement<T1, T2>(this (T1, T2)[] array, T2 value) where T2 : IEquatable<T2>
		{
			return array.IfEachReturn(x => x.Item2.Equals(value));
		}
		public static void Reset<T1, T2>(this (T1, T2)[] array, T2 value = default) where T2 : struct
		{
			for (int i = 0; i < array.Length; i++)
				array[i].Item2 = value;
		}

		public static int CountElementsEqualTo<T1, T2>(this (T1, T2)[] array, T2 value) where T2 : IEquatable<T2>
		{
			return array.ObjectCount(x => x.Item2.Equals(value));
		}

		public static int CountElementsEqualTo<T>(this T[] array, T value) where T : IEquatable<T>
		{
			return array.ObjectCount(x => x.Equals(value));
		}

		#endregion

		#region IList
		public static T[] ObjectsImplementing<T>(this IList objects, int capacity) where T : class
		{
			T[] array = new T[capacity];
			int index = 0;
			objects.IfEachCount(ref index, capacity, delegate (object obj)
			{
				if (obj is T t)
				{
					array[index] = t;
					return true;
				}
				return false;
			});
			return array;
		}
		#endregion

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

		public static bool AttackableForAI(GameObject Target)
		{
			return Applicable(Target) && IsNotASolidBlock(Target);
		}

		public static bool IsNotASolidBlock(GameObject Target)
		{
			return !Target.IsFrozen() && !Target.IsInStasis();
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
		/// Evaluates if a target can be fed on by a vampire. Important for any vampiric spell or vampire related feature.
		/// </summary>
		public static bool Applicable(GameObject Victim) => !FailedSimpleChecks(Victim) && !Victim.IsWall() && !HasWrongAnatomy(Victim.Body?.Anatomy) && !Stealth.StealthCore.Inanimate(Victim);
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