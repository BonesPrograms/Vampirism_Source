using System;
using XRL.UI;
using Nexus.Properties;
using Nexus.Core;
using Nexus.Registry;
using Nexus.Rules;
using XRL.World.Parts.Mutation;

namespace XRL.World.Parts
{
	/// <summary>
	/// Source for Humanity that controls score deduction and regeneration. Does not decide when to remove humanity itself, usually invoked by other types (see DeathEvents nad IFeeding).
	/// </summary>

	[Serializable]

	public class Humanity : IPart //AI do not experience humanity on their own, but if dominated, 
								  //they can lose humanity by killing people via feeding, and enter a gameover state.
	{                               //Other forms of humanity loss covered by DeathEvents all track back to the original player instead.

		public int Score = HUMANITY.MAX;
		public int RegenTimer;
		public bool GameOver;
		public bool State_GO => Score <= HUMANITY.GAMEOVER;
		public override void Register(GameObject Object, IEventRegistrar Registrar) => Registrar.Register(Events.WISH_HUMANITY);
		public override bool FireEvent(Event E)
		{
			if (E.ID == Events.WISH_HUMANITY)
			{
				Score = HUMANITY.MAX;
				GameOver = false;
			}
			return base.FireEvent(E);
		}

		/// <summary>
		/// Removes one point of humanity.
		/// </summary>

		public void VampireKilled()
		{
			Score -= HUMANITY.LOSS_PER_KILL;
			ParentObject.SetIntProperty(FLAGS.HUMANITY, Score);
			if (Score > HUMANITY.GAMEOVER)
				AddPlayerMessage("{{R|HUMANITY LOST!}}\nYou have " + strings() + " {{G sequence|Humanity}}.");
		}
		public override bool WantEvent(int ID, int cascade)
		{
			if (!GameOver && ParentObject.IsPlayer() && Options.GetOptionBool(OPTIONS.HUMANITY) && !ParentObject.CheckFlag(FLAGS.FEED, FLAGS.FRENZY) && ID == SingletonEvent<BeginTakeActionEvent>.ID)
				return true;
			return base.WantEvent(ID, cascade);
		}
		public override bool HandleEvent(BeginTakeActionEvent E)
		{
			if (!State_GO)
				Regenerate();
			else
				HumanityGameOver();
			ParentObject.SetIntProperty(FLAGS.HUMANITY, Score);
			ParentObject.SetIntProperty(FLAGS.REGEN, RegenTimer);
			return base.HandleEvent(E);
		}

		void Regenerate()
		{
			if (Score < HUMANITY.MAX)
			{
				RegenTimer++;
				if (RegenTimer >= HUMANITY.REGEN_TIME)
				{
					Score += HUMANITY.REGEN;
					AddPlayerMessage("{{G sequence|Humanity}} gained!\nYou have " + strings() + " {{G sequence|Humanity.}}");
					RegenTimer = 0;
				}
			}

		}
		void HumanityGameOver()
		{
			Popup.ShowFail("Your {{G sequence|Humanity}} is lost.\nYou succumb to {{R sequence|the Beast}}.");
			ParentObject.SetStringProperty(FLAGS.GO, FLAGS.TRUE);
			GameOver = true;
			ParentObject.FireEvent(Event.New(Events.GAMEOVER)); //everybody changes their state after gameover, disabling all code related to humanity, and pretty much everythign related to blood (as of right now) except metabolism. frenzycore however becomes extremely active and begins checking the world each turn for targets, while stealth disables itself and stops foreaching the world each turn because it becomes impossible for you to use it.
		}

		string strings()
		 =>
			Score switch
			{
				HUMANITY.CRIT => "{{R sequence|1}}{{Y sequence|/5}}",
				HUMANITY.LOW => "{{W sequence|2}}{{Y sequence|/5}}",
				HUMANITY.MID => "{{W sequence|3}}{{Y sequence|/5}}",
				HUMANITY.HIGH => "{{G sequence|4}}{{Y sequence|/5}}",
				HUMANITY.MAX => "{{G sequence|5}}{{Y sequence|/5}}",
				HUMANITY.GAMEOVER => "{{R sequence|0}}",
				_ => OutOfRange()
			};

		static string OutOfRange()
		{
			MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), "Error @ Humanity.strings() -- player humanity value is out of range!");
			return "Error - see Player.log";
		}


	}
}