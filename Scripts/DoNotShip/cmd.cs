using XRL.Wish;
using XRL.World.Effects;
using System.Collections.Generic;
using XRL.UI;
using XRL.World.Parts.Skill;
using Nexus.Core;
using Nexus.Properties;
using System;
using XRL.World.Parts.Mutation;
using XRL.World;
using XRL.World.Parts;

namespace Nexus.Core
{
    static class cmd_extensions
    {
        public static bool CmdTarget(this GameObject Object, string text, out GameObject pick)
        {
            Cell Cell = Object.PickDirection(text);
            pick = Cell?.GetCombatTarget(Object);
            bool value = pick != null;
            if (!value && Cell != null)
                Popup.ShowFail(Cell.HasObjectWithPart(nameof(Combat)) ? $"There is no one there you can {text}." : $"There is no one there to {text}");
            return value;
        }
    }
}


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
            SwitchFlipper(nameOf, cmd);
        }

        static void SwitchFlipper<T>(string nameOf, T obj) where T : class
        {
            Type Obj = obj.GetType();
            var field = Obj.GetField(nameOf);
            if (field?.GetValue(obj) is bool value)
            {
                value = !value;
                msg($"{nameOf} is {(value ? "on" : "off")}.");
                field.SetValue(obj, value);
            }
            else
                AddPlayerMessage($"field {nameOf} does not exist in {Obj.FullName} or is not bool");
        }

        [WishCommand("getstaticplayer")]

        public static void getstaticplayer() => GetStaticPlayer();

        [WishCommand("showstaticplayer")]
        
        public static void GetStaticPlayer()
        {
            cmd.msg($"{DeathHandler.Player?.DisplayName} sent");
        }

        [WishCommand("forcelight")]

        public static void ForceLight()
        {
            Cell cell = The.Player.PickDirection("ForceLight");
            if (cell != null)
            {
                var obj = cell.GetFirstObjectPart<TorchProperties>();
                if (obj != null)
                    obj.Light();
                else
                    msg("No torch in cell");
            }
        }

        [WishCommand("removepart", null)]

        public static void removepart(string value)
        {
            if (The.Player.RemovePart(value))
                msg("removed");
            else
                msg("does not exist");
        }

        [WishCommand("embrace")]

        public static void embraceable()
        {
            Cell cell = The.Player.PickDirection("Embraceable");
            if (cell != null)
            {
                msg("CheckEmbrace");
                foreach (var obj in cell.Objects)
                {
                    if (obj.TryGetStringProperty(FLAGS.EMBRACE.EMBRACEABLE, out var result))
                        msg($"{obj}, {result}");
                }
            }
        }

        [WishCommand("addpart", null)]

        public static void addpart(string value)
        {
            value = "XRL.World.Parts." + value;
            Type type = Type.GetType(value, false);
            if (type != null && Activator.CreateInstance(type) is IPart obj)
            {
                msg("requirepart " + value);
                The.Player.RequirePart(obj);
            }
            else
                msg($"{value} is not IPart");
        }

        [WishCommand("scanfor", null)]

        public static void scanfor(string value)
        {
            Cell cell = The.Player.PickDirection("scanfor");
            if (cell != null)
            {
                msg("scanfor " + value);
                foreach (var obj in cell.Objects)
                {
                    if (obj.HasPart(value))
                    {
                        msg($"{obj} haspart {value}");
                    }
                }
            }
        }

        [WishCommand("scan")]
        public static void Scan()
        {
            GameObject GO = The.Player;
            Cell cell = GO.PickDirection("scan");
            if (cell != null)
            {
                List<GameObject> objects = cell.GetObjects();
                foreach (var obj in objects)
                {
                    Log($"\nSTART {obj}, {obj.ID}");
                    Log($"Blueprint, {obj.Blueprint}");
                    Log("\n--STRINGPROPS--");
                    foreach (var strng in obj.Property)
                        Log(strng);
                    Log("\n-INTPROPS");
                    foreach (var integer in obj.IntProperty)
                        Log(integer);
                    Log("\n--PARTS--");
                    foreach (var part in obj.PartsList)
                        Log(part);
                    Log("\n-EFFECTS-");
                    foreach (var effect in obj.Effects)
                        Log(effect);
                    Log($"\nTemp {obj.Temperature}");
                    Log($"END {obj}, {obj.ID}");
                }
                AddPlayerMessage("ScanComplete");
            }
        }

        static void Log<TKey, TValue>(KeyValuePair<TKey, TValue> obj) => Log($"{obj.Key}, {obj.Value}");
        static void Log<T>(T obj) => Log($"{obj}");
        static new void Log(string text) => MetricsManager.LogInfo(text);

        [WishCommand("autowin")]

        public static void autowin()
        {
            if (The.Player.CmdTarget("autowin", out var pick))
            {
                Mutations m = pick.RequirePart<Mutations>();
                if (!m.HasMutation(nameof(Vampirism)))
                    m.AddMutation(nameof(Vampirism));
                if (pick.HasPart<Vampirism>())
                    IComponent<GameObject>.AddPlayerMessage("Vampirized");
                Vampirism v = pick.GetPart<Vampirism>();
                v.FeedCommand.AutoWin = true;
                AddPlayerMessage("Autowinner " + pick);
            }
        }

        [WishCommand("badliquid")]

        public static void badliquid()
        {
            if (The.Player.CmdTarget("badliquid", out var pick))
            {
                Vampirism v = new();
                Nexus.Biting.Bite bite = v.FeedCommand.Bite;
                int range = WikiRng.Next(0, bite.BadLiquids.Length - 1);
                string liquid = bite.BadLiquids[range].Item1;
                pick.ApplyEffect(new LiquidCovered(liquid, 2, 9999));
                cmd.msg($"badliquified {pick} {liquid} {range}");
            }
        }

        [WishCommand("liquify")]
        public static void liquify(string liquid)
        {
            if (The.Player.CmdTarget("liquify", out var pick))
            {
                Vampirism v = new();
                Nexus.Biting.Bite bite = v.FeedCommand.Bite;
                bool valid = false;
                for (int i = 0; i < bite.BadLiquids.Length; i++)
                {
                    if (bite.BadLiquids[i].Item1 == liquid)
                        valid = true;
                }
                if (valid)
                {
                    pick.ApplyEffect(new LiquidCovered(liquid, 2, 9999));
                    cmd.msg($"Liquified {liquid} {pick}");
                }
                else
                    cmd.msg($"{liquid} is invalid for vampirism");
            }
        }

        [WishCommand("slimify")]
        public static void slimify() => liquify("slime");

        [WishCommand("lust")]

        public static void lust()
        {
            Vitae v = The.Player.GetPart<Vitae>();
            SwitchFlipper(nameof(v.AntiPuke), v);

        }

        [WishCommand("kill")]

        public static void kill()
        {
            if (The.Player.TryGetTarget("kill", "kill", out var pick))
            {
                pick.TakeDamage(100000, The.Player, "Killed");
            }
        }

        [WishCommand("farmer")]
        public static void Farmer() => Spawn(The.Player.CurrentCell, "WatervineFarmerJoppa");
        public static GameObject Spawn(Cell cell, string param) => cell.getClosestEmptyCell().AddObject(GameObject.Create(param));

        [WishCommand("checkfx")]

        public static void Checkfx() => showfx();

        [WishCommand("showfx")]

        public static void showfx()
        {
            GameObject GO = The.Player;
            Cell Cell = GO.PickDirection("showfx");
            List<GameObject> objects = Cell?.GetObjectsWithPart(nameof(Combat));
            if (objects != null)
            {
                AddPlayerMessage("FX printed");
                foreach (var obj in objects)
                {
                    foreach (var fx in obj.Effects)
                        MetricsManager.LogInfo($"{obj}, {fx}");
                }
            }
            else
                AddPlayerMessage("NoFX");
            if (objects?.Count == 0)
                AddPlayerMessage("NoFX");
        }

        [WishCommand("getzlevel")]
        public static void GetZ()
        {
            IComponent<GameObject>.AddPlayerMessage($"{The.Player.CurrentZone.GetZoneZ()}");
        }

        [WishCommand("showtypes")]

        public static void ShowTypes()
        {
            GameObject GO = The.Player;
            Cell Cell = GO.PickDirection("showtypes");
            List<GameObject> objects = Cell?.GetObjects();
            if (objects != null)
            {
                foreach (var obj in objects)
                {
                    var type = obj.GetType();
                    MetricsManager.LogInfo($"{type}, {type.Name}, {type.Namespace}, {type.BaseType}, {type.GUID}");
                }
            }
        }

        [WishCommand("vampirize")]

        public static void Vampirize()
        {
            GameObject GO = The.Player;
            if (GO.CmdTarget("vampirize", out var pick))
            {
                Mutations m = pick.RequirePart<Mutations>();
                if (!m.HasMutation(nameof(Vampirism)))
                    m.AddMutation(nameof(Vampirism));
                if (pick.HasPart<Vampirism>())
                    IComponent<GameObject>.AddPlayerMessage("Vampirized");
            }
        }

        [WishCommand("killall")]

        public static void KillAll()
        {
            GameObject GO = The.Player;
            List<GameObject> combatobjects = GO.CurrentZone.GetObjectsWithPart(nameof(Combat));
            foreach (var obj in combatobjects)
                if (obj != GO)
                    obj.TakeDamage(1000, GO, "KillAll");
            AddPlayerMessage("AllKilled");
        }

        [WishCommand("unvampirize")]

        public static void Unvampirize()
        {
            GameObject GO = The.Player;
            if (GO.CmdTarget("unvampirize", out var pick) && pick.HasPart<Vampirism>())
            {
                Mutations m = pick.GetPart<Mutations>();
                var v = m.GetMutation(nameof(Vampirism));
                m.RemoveMutation(v);
                if (!pick.HasPart<Vampirism>())
                    IComponent<GameObject>.AddPlayerMessage("unVampirized");
            }
        }

        [WishCommand("checkprops")]
        public static void CheckStringProperties()
        {
            GameObject GO = The.Player;
            Cell cell = GO.PickDirection("checkprops");
            List<GameObject> objects = cell.GetObjects();
            foreach (var obj in objects)
            {
                foreach (var prop in obj.Property)
                    MetricsManager.LogInfo($"{obj}, {prop.Key}, {prop.Value}");
            }
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
            bool HumanityGameOver = tgt.CheckFlag(FLAGS.GO);
            bool Feeding = tgt.CheckFlag(FLAGS.FEED);
            bool Frenzying = tgt.CheckFlag(FLAGS.FRENZY);
            bool Stealthy = tgt.CheckFlag(FLAGS.STEALTH);
            string Blooddrinker = tgt.GetStringProperty(FLAGS.BLOOD_STATUS);
            int Vitae = tgt.GetIntProperty(FLAGS.BLOOD_VALUE);
            int Humanity = tgt.GetIntProperty(FLAGS.HUMANITY);
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

        public static void log(string text) => MetricsManager.LogInfo(text);
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

        [WishCommand(Command = "coffin")]

        public static void coffin()
        {
            CoffinSpell.AutoWin = !CoffinSpell.AutoWin;
            msg($"{CoffinSpell.AutoWin}");
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