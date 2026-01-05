using XRL.Wish;
using XRL.World.Effects;
using System.Collections.Generic;
using XRL.UI;
using XRL.World.Parts.Skill;
using Nexus.Core;
using Nexus.Properties;
using System;
using XRL.World.Parts.Mutation;

namespace XRL.World.Parts
{

    [HasWishCommand]

    [Serializable]
    public class cmd : IPart
    {
        public bool showvitae = false;
        public bool showStealthed = false;
        public bool ShowActiveStealthed = false;
        public bool showGO = false;
        public bool showFeed = false;
        public bool showFrenzy = false;
        public bool showStatus = false;
        public bool showStealthy = false;
        public bool showHumanity = false;
        public bool showCombat = false;
        public bool showWater = false;


        public bool SkipABeat = false;
        public int BeatSkipValue;
        public bool Skip = false;
        public int SkipValue;
        public bool BigSkip = false;
        public int BigSkipValue;

        public bool names = false;
        public bool refresh = false;
        public bool showturns = false;
        public bool showbloodtype = false;

        public override bool WantEvent(int ID, int cascade)
        {
            if (!base.WantEvent(ID, cascade))
                return ID == SingletonEvent<BeforeTakeActionEvent>.ID;
            return true;
        }

        public override bool HandleEvent(BeforeTakeActionEvent E)
        {
            Nightbeast n = ParentObject.GetPart<Nightbeast>();
            Properties(ParentObject);
            if (showStealthed || showStealthy || ShowActiveStealthed)
                Properties(ParentObject, n.StealthStage1, n.StealthStage2);
            if (names == true)
                ShowStealthList(n.ActiveWitnesses);
            if (refresh == true)
                Refresh();
            if (showturns == true)
                cmd.msg($"{The.Game.Turns}");
            if (showbloodtype == true)
                cmd.msg(ParentObject.GetStringProperty("BleedLiquid") + "bloodtype");
            if (showWater)
                cmd.msg($"{ParentObject.GetPart<Stomach>().Water}");
            return base.HandleEvent(E);
        }
        void SwitchFlipper(string nameOf) //nameof(Boolean)
        {
            var cmd = Get();
            var field = cmd.GetType().GetField(nameOf);

            bool value = (bool)field.GetValue(cmd);
            value = !value;

            msg($"{nameOf} is {(value ? "on" : "off")}.");
            field.SetValue(cmd, value);
        }

        [WishCommand("showwater")]

        public void showwater() => SwitchFlipper(nameof(showWater));

        [WishCommand("splatterme")]

        public void splatterme() => The.Player.Bloodsplatter();

        cmd Get()
        {

            if (!The.Player.HasPart<cmd>())
                The.Player.AddPart<cmd>();
            return The.Player.GetPart<cmd>();
        }


        bool Method(bool cmdbool, string text)
        {
            if (cmdbool == false)
            {
                msg(text + " on");
                return true;
            }
            else
            {
                msg(text + " off");
                return false;
            }
        }

        [WishCommand("showbloodtype")]

        public void ShowBloodType()
        {
            Get().showbloodtype = Method(Get().showbloodtype, "showbloodtype");
        }

        [WishCommand("showturns")]

        public void showTurns()
        {
            SwitchFlipper(nameof(showturns));
        }

        [WishCommand("refreshme")]

        public void refreshme()
        {
            SwitchFlipper(nameof(refresh));
        }

        [WishCommand("shownames")]

        public void shownames()
        {
            SwitchFlipper(nameof(names));
        }

        [WishCommand(Command = "admin")]

        public static void admin()
        {
            if (!The.Player.HasPart<Vampirism>())
                AddPlayerMessage("No");
            else
            {
                if (!The.Player.HasPart<cmd>())
                {
                    The.Player.AddPart<cmd>();
                    AddPlayerMessage("ADMN");
                }
                else
                    cmd.msg("AlreadyAdmin!");
            }
        }

        public void ShowStealthList(List<GameObject> ActiveWitnesses)
        {
            if (ActiveWitnesses.Count != 0)
            {
                foreach (GameObject obj in ActiveWitnesses)
                {
                    Names(obj, ParentObject, 'W');
                }
            }
        }

        public void Properties(GameObject tgt)
        {
            Properties(tgt, "");
        }

        //ADMN.Properties(ParentObject, Stealthed, "Stealthed", ActiveStealthFeed, "ActiveStealth");
        public void Properties(GameObject tgt, bool Stealthed, bool ActiveStealth)
        {
            string message = "";
            if (showStealthed)
                message += TextMaker(Stealthed, "Stage1", 'B');
            if (ShowActiveStealthed)
                message += TextMaker(ActiveStealth, "Stage2", 'b');
            Properties(tgt, message);
        }

        public void Properties(GameObject tgt, bool stealth, string type)
        {
            string message = "";
            message += TextMaker(stealth, type, 'b');
            Properties(tgt, message);
        }

        char MakeColor(bool state)
        {
            if (state == true)
                return 'G';
            else
                return 'R';
        }

        string TextMaker(int value, string text, char choosecolor)
        {
            string Other = TextMaker(text, choosecolor);
            string New = $"{value}" + " " + Other;
            return New;
        }

        string TextMaker(bool state, string text, char choosecolor)
        {
            string color = "{{" + MakeColor(state) + "|";
            string msg = color + state + "}}, " + TextMaker(text, choosecolor);
            return msg;
        }

        string TextMaker(string text, string text2, char choosecolor)
        {
            string other = TextMaker(text, choosecolor);
            string New = text2 + " " + other;
            return New;

        }

        string TextMaker(string text, char choosecolor)
        {
            string msg = "{{" + choosecolor + "|" + text + "}}; ";
            return msg;
        }

        void Properties(GameObject tgt, string text)
        {
            bool HumanityGameOver = Scan.ReturnProperty(tgt, Flags.GO);
            bool Feeding = Scan.ReturnProperty(tgt, Flags.FEED);
            bool Frenzying = Scan.ReturnProperty(tgt, Flags.FRENZY);
            bool Stealthy = Scan.ReturnProperty(tgt, Flags.STEALTH);
            string Blooddrinker = tgt.GetStringProperty(Flags.BLOOD_STATUS);
            int Vitae = tgt.GetIntProperty(Flags.BLOOD_VALUE);
            int Humanity = tgt.GetIntProperty(Flags.HUMANITY);
            bool combat = tgt.IsInCombat();

            if (showGO)
                text += TextMaker(HumanityGameOver, "GO", 'M');
            if (showFeed)
                text += TextMaker(Feeding, "feed", 'R');
            if (showFrenzy)
                text += TextMaker(Frenzying, "frenzy", 'O');
            if (showStatus)
                text += TextMaker(Blooddrinker, "status", 'w');
            if (showStealthy)
                text += TextMaker(Stealthy, "Stealthy", 'm');
            if (showvitae)
                text += TextMaker(Vitae, "Vitae", 'r');
            if (showHumanity)
                text += TextMaker(Humanity, "Humanity", 'G');
            if (showCombat)
                text += TextMaker(combat, "combat", 'W');
            if (BigSkip == true)
            {
                BigSkipValue++;
                if (BigSkipValue > 10)
                {
                    BigSkipValue = 0;
                    if (text != "")
                        IComponent<GameObject>.AddPlayerMessage(text);
                }
                return;
            }
            if (Skip == true)
            {
                SkipValue++;
                if (SkipValue > 3)
                {
                    SkipValue = 0;
                    if (text != "")
                        IComponent<GameObject>.AddPlayerMessage(text);
                }
                return;

            }
            if (SkipABeat == true)
            {
                BeatSkipValue++;
                if (BeatSkipValue == 2)
                {
                    BeatSkipValue = 0;
                    if (text != "")
                        IComponent<GameObject>.AddPlayerMessage(text);
                }
                return;
            }
            if (text != "")
                IComponent<GameObject>.AddPlayerMessage(text);
        }

        public void Names(GameObject witness, GameObject player, char color)
        {
            if (witness is not null && witness.ID is not null && witness.CurrentCell is not null)
            {
                bool los = witness.HasLOSTo(player, false);
                string name = witness.ToString();
                string msg = "{{" + color + "|_ID - }}" + $"{name}, " + "{{M|ID:}}" + $"{witness.ID}, " + "{{G|D:}}" + $"{witness.DistanceTo(player.CurrentCell)}, " + "{{O|L:}}" + $"{witness.CurrentCell.GetLight()}, " + "{{C|LOS:}}" + los;
                if (witness == player)
                {
                    msg = "{{R sequence|PLAYER}}" + "{{O|LIGHT_}}" + $"{witness.CurrentCell.GetLight()}";
                }
                IComponent<GameObject>.AddPlayerMessage(msg);
            }
        }


        void Out(out cmd cmd)
        {
            cmd = The.Player.GetPart<cmd>();
            if (cmd is null)
                AddPlayerMessage("not admin");
        }

        [WishCommand(Command = "bigskip")]

        public void bigskip()
        {
            Out(out cmd cmd);
            cmd.BigSkip = true;
            cmd.msg("Bigskip True");
        }
        [WishCommand(Command = "bigskipoff")]

        public void bigskipoff()
        {
            Out(out cmd cmd);
            cmd.BigSkip = false;
            cmd.msg("Bigskip false");
        }

        [WishCommand(Command = "showallstealth")]
        public void ShowAllSteath()
        {
            SwitchFlipper(nameof(showStealthed));
            SwitchFlipper(nameof(showStealthy));
            SwitchFlipper(nameof(ShowActiveStealthed));
            msg("ShowAlLStealth");
        }

        [WishCommand(Command = "showFrenzy")]

        public void ShowFrenzy()
        {
            Out(out cmd cmd);
            cmd.showFrenzy = true;
            cmd.msg("Showfrenzy");
        }

        [WishCommand(Command = "showvitae")]

        public void Showvitae()
        {
            Out(out cmd cmd);
            cmd.showvitae = true;
            cmd.msg("showvitae");
        }

        [WishCommand(Command = "showstealthed")]

        public void ShowStealthedMethod()
        {
            Out(out cmd cmd);
            cmd.showStealthed = true;
            cmd.msg("showstealthed");
        }

        [WishCommand(Command = "showasm")]

        public void ShowASM()
        {
            Out(out cmd cmd);
            cmd.ShowActiveStealthed = true;
            msg("showasm");
        }

        [WishCommand(Command = "showGO")]

        public void ShowGOTo()
        {
            Out(out cmd cmd);
            cmd.showGO = true;
            msg("showgo");
        }

        [WishCommand(Command = "showFeed")]

        public void FeedsHow()
        {
            Out(out cmd cmd);
            cmd.showFeed = true;
            msg("showfeed");
        }

        [WishCommand(Command = "ShowStatus")]

        public void ShowBloodStatus()
        {
            Out(out cmd cmd);
            cmd.showStatus = true;
            msg("showstatus");
        }

        [WishCommand(Command = "showstealthy")]

        public void ShowStealthyStatus()
        {
            Out(out cmd cmd);
            cmd.showStealthy = true;
            msg("showstealthy");
        }

        [WishCommand(Command = "showHumanity")]

        public void ShowHumanityValue()
        {
            Out(out cmd cmd);
            cmd.showHumanity = true;
            msg("showhumanity");
        }

        [WishCommand(Command = "showCombat")]

        public void showCombatValue()
        {
            Out(out cmd cmd);
            cmd.showCombat = true;
            msg("showcombat");
        }



        [WishCommand(Command = "skip")]

        public void skip()
        {
            Out(out cmd cmd);
            cmd.Skip = true;
            msg("skip");
        }

        [WishCommand(Command = "skipoff")]

        public void skipoff()
        {
            Out(out cmd cmd);
            cmd.Skip = false;
            cmd.msg("skipoff");
        }

        [WishCommand(Command = "skipabeat")]

        public void skipabeat()
        {
            Out(out cmd cmd);
            cmd.SkipABeat = true;
            cmd.msg("skipabeat");
        }

        [WishCommand(Command = "skipabeatoff")]

        public void skipabeatoff()
        {
            Out(out cmd cmd);
            cmd.SkipABeat = false;
            cmd.msg("skipabeatoff");
        }

        [WishCommand(Command = "spawnsleeper")]

        public void SpawnSleeper()
        {
            List<Cell> cells = The.Player.CurrentCell.GetAdjacentCells();
            int i = 0;
            foreach (Cell cell in cells)
            {
                i++;
                if (i < 3)
                {
                    GameObject Object = GameObject.Create("WatervineFarmerJoppa");
                    Object.ApplyEffect(new Asleep(100));
                    cell.AddObject(Object);
                }
                else
                    return;
            }
        }

        [WishCommand(Command = "removebleed")]

        public void removebleed()
        {
            Cell cell = The.Player.PickDirection("RemoveBleed");
            GameObject Victim = cell.GetCombatTarget(The.Player);
            Victim.RemoveEffect<Bleeding>();
            Victim.RemoveEffect<LiquidCovered>();
        }


        [WishCommand(Command = "refresh")]
        public void Refresh()
        {
            ActivatedAbilities activatedAbilities = The.Player.ActivatedAbilities;
            if (activatedAbilities is not null)
            {
                foreach (ActivatedAbilityEntry value in activatedAbilities.AbilityByGuid.Values)
                {
                    if (value.Cooldown != 0)
                        value.Cooldown = 0;


                }
            }
        }

        [WishCommand(Command = "switch")]

        public void SwitchHandler()
        {
            Out(out cmd cmd);
            cmd.showvitae = false;
            cmd.showStealthed = false;
            cmd.ShowActiveStealthed = false;
            cmd.showGO = false;
            cmd.showFeed = false;
            cmd.showFrenzy = false;
            cmd.showStatus = false;
            cmd.showStealthy = false;
            cmd.showHumanity = false;
            cmd.showCombat = false;
            cmd.showturns = false;
            cmd.showbloodtype = false;
            cmd.names = false;
            msg("Everything off");
        }

        [WishCommand(Command = "reswitch")]

        public void Reswitch()
        {
            Out(out cmd cmd);
            cmd.showvitae = true;
            cmd.showStealthed = true;
            cmd.ShowActiveStealthed = true;
            cmd.showGO = true;
            cmd.showFeed = true;
            cmd.showFrenzy = true;
            cmd.showStatus = true;
            cmd.showStealthy = true;
            cmd.showHumanity = true;
            cmd.showCombat = true;
            msg("Everythign true");
        }

        public static void msg(string text) => IComponent<GameObject>.AddPlayerMessage(text);
        public void msg(string text, char color)
        {
            string message = "{{" + color + "|" + text + "}}";
            msg(message);
        }
        public void msg(string text, char color, string text2)
        {
            string message = "{{" + color + "|" + text + "}} " + text2;
            msg(message);
        }

        [WishCommand("bloodify")]

        public void bloodify()
        {
            Cell cell = The.Player.PickDirection("bloodify");
            GameObject obj = cell.GetCombatTarget(The.Player);
            obj.ApplyEffect(new LiquidCovered("blood", 10, 10, false));
        }

        [WishCommand(Command = "hurt")]
        public void Hurt()
        {
            Cell cell = The.Player.PickDirection("RemoveBleed");
            GameObject Victim = cell.GetCombatTarget(The.Player);
            Victim.ApplyEffect(new Bleeding("1", 20));
        }

        [WishCommand(Command = "mod")]
        public void Developer()
        {
            Popup.Suppress = true;
            Mutations m = The.Player.RequirePart<Mutations>();
            m.AddMutation("Domination");
            m.AddMutation("Beguiling");
            m.AddMutation("Phasing");
            m.AddMutation("Sunder Mind");
            The.Player.GetStat("Ego").AddShift(100, null, false);
            The.Player.GetStat("Intelligence").AddShift(100, null, false);
            The.Player.GetStat("Agility").AddShift(100, null, false);
            The.Player.GetStat("SP").AddShift(10000, null, false);
            The.Player.AddSkill<ShortBlades_Bloodletter>();
            //ParentObject.GetStat("Level").Value += 10;
            IComponent<GameObject>.AddPlayerMessage("Developer");
            Popup.Suppress = false;
        }

        [WishCommand(Command = "boxmein")]

        public void Boxmein()
        {
            List<Cell> cells = The.Player.CurrentCell.GetAdjacentCells();
            foreach (Cell cell in cells)
            {
                cell.AddObject("GlassWall");
            }
        }

        [WishCommand(Command = "lavawall")]

        public void LavaWall()
        {
            List<Cell> cells = The.Player.CurrentCell.GetAdjacentCells();
            foreach (Cell cell in cells)
            {
                cell.AddObject("LavaPuddle");
            }
        }

        [WishCommand(Command = "confuseme")]

        public void ConfuseMe()
        {
            The.Player.ApplyEffect(new Confused(10, 1, 1));
            IComponent<GameObject>.AddPlayerMessage("Confuseme");
        }

        [WishCommand(Command = "tough")]
        public void Tough()
        {
            The.Player.GetStat("Toughness").AddShift(100, null, false);
            IComponent<GameObject>.AddPlayerMessage("Tough");
        }


        [WishCommand(Command = "onehum")]

        public void Onehum() => The.Player.GetPart<Humanity>().Score = 1;

    }
}