using XRL;
using XRL.World;
using XRL.World.Effects;
using XRL.World.Parts;

namespace Nexus.Core
{
	/// <summary>
	/// Static helper class for evaluating GameObject states to help determine outcomes.
	/// </summary>
	static class Scan
	{

		/// <summary>
		/// Evaluates alliance, love, and player control.
		/// </summary>
		public static bool IsFriendly(GameObject who, GameObject theVampire)
		{
			if (who is not null && theVampire is not null)
				return who.IsAlliedTowards(theVampire) || who.IsInLoveWith(theVampire) || (theVampire.IsPlayer() && (who.IsPlayerControlled() || who.IsPlayerLed()));
			else
			{
				string msg;
				if (who is null && theVampire is null)
				{
					msg = "who is null and theVampire is null";
				}
				else if (who is null)
					msg = "who is null";
				else
					msg = "theVampire is null";
				MetricsManager.LogModError(ModManager.GetMod("vampirism"), $"IsFriendly received null value, returning false. Null parameter: {msg}");
				return false;
			}
		}

		/// <summary>
		/// Evaluates if a target is in a defenseless condition and plays unique messages for specific conditions.
		/// </summary>
		public static bool Vulnerability(GameObject who, GameObject theVampire) //our vulnerability sheet
		{
			if (who.HasEffect<Vampires_Kiss>())
			{
				return true; //the string for feeding on people who have vampire's kiss is handled by the Friendly variable and Sharing() in CommandHandler
			}
			if (who.HasEffect<KO>())
			{
				IComponent<GameObject>.AddPlayerMessage("This one was tasty.You dive in for more.");
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
			if (who.IsInLoveWith(theVampire))
			{
				if (theVampire.IsPlayer())
					IComponent<GameObject>.AddPlayerMessage(who.t() + " is in {{love|love}} with you and offers " + who.its + " neck openly.");
				return true;
			}
			if (theVampire.IsPlayer() && who.HasEffect<Beguiled>())
			{
				IComponent<GameObject>.AddPlayerMessage(who.t() + " presents " + who.its + " neck willingly to you.");
				return true;
			}
			if (who.IsAlliedTowards(theVampire) || (theVampire.IsPlayer() && (who.HasEffect<Proselytized>() || who.IsPlayerLed())))
			{
				if (theVampire.IsPlayer())
					IComponent<GameObject>.AddPlayerMessage(who.t() + " exposes " + who.its + " neck reluctantly to you.");
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
			else
				return false;
		}
		/// <summary>
		/// Returns true/false values from object string properties. Default true.
		/// </summary>
		public static bool SafeReturnProperty(GameObject theVampire, string flag1, string flag2) => SafeReturnProperty(theVampire, flag1) || SafeReturnProperty(theVampire, flag2);


		/// <summary>
		/// Returns true/false values from object string properties. Default true. Returns false if object is null.
		/// </summary>
		public static bool SafeReturnProperty(GameObject theVampire, string flag)
		{
			if (theVampire is null)
			{
				MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), "Error @ Scan.SafeReturnProperty: theVampire is null, returning false.");
				return false;
			}
			else
				return theVampire.GetStringProperty(flag) == Properties.Flags.TRUE;
		}


		/// <summary>
		/// Evaluates if the vampire is in a condition wherein they are incapable of activating Feed. Special evaluation for when frenzy is active.
		/// </summary>

		public static bool Incap(GameObject theVampire, bool frenzying)
		 =>
			 theVampire.IsFrozen()
			|| theVampire.IsInStasis()
			|| theVampire.CanMoveExtremities(XRL.World.Parts.Mutation.Vampirism.ABILITY_NAME) is false
			|| Unaware(theVampire, false)
			|| (theVampire.IsConfused && (theVampire.IsPlayer() is false || frenzying))
			|| (theVampire.HasEffect<StunGasStun>() && !theVampire.IsPlayer());
		//even with useenergy event, still had some bugs associated with effects and conditions that youd normally expect to end a feeding

		/// <summary>
		/// Evaluates if a target lacks awareness of their surroundings, such as stun, sleep, or paralysis.
		/// </summary>
		public static bool Unaware(GameObject Victim, bool kissing)
		  =>
			 (Victim.HasEffect<Vampires_Kiss>() && kissing is false) //the person being attacked is technically considered aware except in stealth attacks
			|| Victim.HasEffect<KO>()                               //which is handled by completely different logic in Nightbeast.Stealthfeeding()
			|| Victim.HasEffect<Stun>()
			|| Victim.HasEffect<Paralyzed>()
			|| Victim.HasEffect<Asleep>()
			|| Victim.HasEffect<Exhausted>();


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
			if (Victim.HasPart<PlantProperties>())
				return false;
			return true;
		}
		static bool FailedSimpleChecks(GameObject Victim) => !GameObject.Validate(ref Victim) || !Victim.IsCombatObject() || !Victim.IsOrganic;
		static bool HasBadTag(GameObject Victim)
		   =>
			 Victim.HasTagOrProperty("Plant") //i thought this simple check would be enough but
			|| Victim.HasTagOrProperty("LivePlant")
			|| Victim.HasTagOrProperty("Fungus")
			|| Victim.HasTagOrProperty("Plank")
			|| Victim.HasTagOrProperty("Bleeds") is false;

		//static bool CheckBleedLiquid(GameObject Object) => Object.TryGetStringProperty("BleedLiquid", out string result) && result is "blood-1000" or null or "";
		static bool HasWrongAnatomy(GameObject Victim)
		=>
			 Victim.Body.Anatomy is "Star"
			|| Victim.Body.Anatomy is "Echinoid"
			|| Victim.Body.Anatomy is "Flower"
			|| Victim.Body.Anatomy is "Vine"
			|| Victim.Body.Anatomy is "Tree"
			|| Victim.Body.Anatomy is "Cactus"
			|| Victim.Body.Anatomy is "Bush"
			|| Victim.Body.Anatomy is "Ooze"
			|| Victim.Body.Anatomy is "Jelly";
		//who wouldve thought i needed to be so specific
	}
}