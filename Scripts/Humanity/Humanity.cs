using System;
using XRL.UI;
using VampirismSys.Properties;
using VampirismSys.Extensions;
using VampirismSys.Registry;
using VampirismSys.Rules;
using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Parts
{
	/// <summary>
	/// Source for Humanity that controls score deduction and regeneration. Does not decide when to remove humanity itself, usually invoked by other types (see DeathEvents nad IFeeding).
	/// </summary>

	[Serializable]

	public class Humanity : IPart //AI do not experience humanity on their own, but if dominated, 
								  //they can lose humanity by killing people via feeding, and enter a gameover state.
	{                               //Other forms of humanity loss covered by DeathEvents all track back to the original player instead.
		bool State_GO => Score <= VampirismSys.Rules.Hum.GAMEOVER;

		public int Score
		{
			get => _score;
			set
			{
				_score = 
				  GameOver ? 0
				: value > Hum.MAX ? Hum.MAX
				: value < Hum.GAMEOVER ? Hum.GAMEOVER
				: value;
			}
		}
		public int _score = VampirismSys.Rules.Hum.MAX;
		public int _regen;
		public bool GameOver;

		public override void Register(GameObject Object, IEventRegistrar Registrar) => Registrar.Register(Events.WISH_HUMANITY);
		public override bool FireEvent(Event E)
		{
			if (E.ID == Events.WISH_HUMANITY)
			{
				Score = VampirismSys.Rules.Hum.MAX;
				GameOver = false;
			}
			return base.FireEvent(E);
		}

		/// <summary>
		/// Removes one point of humanity.
		/// </summary>

		public void VampireKilled()
		{
			Score -= VampirismSys.Rules.Hum.LOSS_PER_KILL;
			if (Score > VampirismSys.Rules.Hum.GAMEOVER)
				AddPlayerMessage("{{R|HUMANITY LOST!}}\nYou have " + strings() + " {{G sequence|Humanity}}.");
		}
		public override bool WantEvent(int ID, int cascade)
		{
			if (ID == SingletonEvent<BeginTakeActionEvent>.ID && !GameOver && ParentObject.IsPlayer() && Options.GetOptionBool(ModOptions.HUMANITY) && !ParentObject.CheckFlag(Flags.FEED, Flags.FRENZY))
				return true;
			return base.WantEvent(ID, cascade);
		}
		public override bool HandleEvent(BeginTakeActionEvent E)
		{
			if (!State_GO)
				Regenerate();
			else
				HumanityGameOver();
			ParentObject.SetIntProperty(Flags.HUMANITY, Score);
			ParentObject.SetIntProperty(Flags.REGEN, _regen);
			return base.HandleEvent(E);
		}

		public void AddHumanity()
		{
			Score += VampirismSys.Rules.Hum.REGEN;
			AddPlayerMessage("{{G sequence|Humanity}} gained!\nYou have " + strings() + " {{G sequence|Humanity.}}");
		}

		void Regenerate()
		{
			if (Score < VampirismSys.Rules.Hum.MAX)
			{
				_regen++;
				if (_regen >= VampirismSys.Rules.Hum.REGEN_TIME)
				{
					AddHumanity();
					_regen = 0;
				}
			}

		}
		void HumanityGameOver()
		{
			Popup.ShowFail("Your {{G sequence|Humanity}} is lost forever.\nYou succumb to {{R sequence|the Beast}}.");
			ParentObject.SetStringProperty(Flags.GO, Flags.TRUE);
			GameOver = true;
			ParentObject.FireEvent(Event.New(Events.GAMEOVER)); //everybody changes their state after gameover, disabling all code related to humanity, and pretty much everythign related to blood (as of right now) except metabolism. frenzycore however becomes extremely active and begins checking the world each turn for targets, while stealth disables itself and stops foreaching the world each turn because it becomes impossible for you to use it.
		}

		string strings()
		 =>
			Score switch
			{
				VampirismSys.Rules.Hum.CRIT => "{{R sequence|1}}{{Y sequence|/5}}",
				VampirismSys.Rules.Hum.LOW => "{{W sequence|2}}{{Y sequence|/5}}",
				VampirismSys.Rules.Hum.MID => "{{W sequence|3}}{{Y sequence|/5}}",
				VampirismSys.Rules.Hum.HIGH => "{{G sequence|4}}{{Y sequence|/5}}",
				VampirismSys.Rules.Hum.MAX => "{{G sequence|5}}{{Y sequence|/5}}",
				VampirismSys.Rules.Hum.GAMEOVER => "{{R sequence|0}}",
				_ => OutOfRange()
			};

		static string OutOfRange()
		{
			MetricsManager.LogModError(XRL.ModManager.GetMod("vampirism"), "Error @ Humanity.strings() -- player humanity value is out of range!");
			return "Error - see Player.log";
		}

	}
}