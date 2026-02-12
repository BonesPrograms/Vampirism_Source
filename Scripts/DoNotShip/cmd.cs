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
using System.Reflection;
using HarmonyLib;
using XRL;

namespace Nexus.Core
{
    static class cmd_extensions
    {
        //this version of TryGetTarget lets you target yourself
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

namespace Nexus.Patches
{
    [HarmonyPatch(typeof(BaseMutation), nameof(BaseMutation.CompatibleWith))]
    public static class CompatDbg
    {
        [HarmonyPrefix]

        public static void Prefix(BaseMutation __instance, GameObject go)
        {
            List<MutationEntry> mutationEntries = MutationFactory.GetMutationEntries(__instance);
            if (mutationEntries == null)
            {
                return;
            }

            foreach (MutationEntry item in mutationEntries)
            {
                string[] exclusions = item.GetExclusions();
                cmd.Log($"{item}");
                cmd.Log(exclusions);
                foreach (string name in exclusions)
                {
                    if (MutationFactory.HasMutation(name))
                    {
                        cmd.Log(name);
                        string name2 = MutationFactory.GetMutationEntryByName(name).Class;
                        if (go.HasPart(name2))
                        {
                            cmd.Log(name2);
                        }
                    }
                }
            }

        }
    }
}


namespace XRL.World.Parts
{

    [HasWishCommand]

    [Serializable]
    public class cmd : IPart
    {

        public bool IsVampire;

        public cmd(bool IsVampire)
        {
            this.IsVampire = IsVampire;
        }
        bool wantsVampirism => showvitae || showStealthed || ShowActiveStealthed || showGO || showFeed || showFrenzy || showStatus || showStealthy || showHumanity;

        #region Vampirism
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
        public bool showbloodtype = false;

        #endregion

        #region Message Timers

        public bool SkipABeat = false;
        public int BeatSkipValue;
        public bool Skip = false;
        public int SkipValue;
        public bool BigSkip = false;
        public int BigSkipValue;
        #endregion

        #region Stealth

        public bool names = false;

        #endregion


        #region other

        public bool refresh = false;
        public bool showturns = false;

        #endregion
        public override bool WantEvent(int ID, int cascade)
        {
            if (!base.WantEvent(ID, cascade))
                return ID == SingletonEvent<BeforeTakeActionEvent>.ID;
            return true;
        }
        Nightbeast _n = null;
        Nightbeast n => _n ??= ParentObject.GetPart<Nightbeast>();
        public override bool HandleEvent(BeforeTakeActionEvent E)
        {
            if (IsVampire)
            {
                if (showStealthed || showStealthy || ShowActiveStealthed)
                    Properties(ParentObject, n.StealthStage1, n.StealthStage2);
                else
                    Properties();
                if (names == true)
                    ShowStealthList(n.Witnesses);
            }
            else if (wantsVampirism)
            {
                msg("Making into vampire due to wish");
                IsVampire = true;
                ParentObject.RequireMutation<Vampirism>();
            }

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


        #region Switches

        [WishCommand(Command = "switch")]

        public static void SwitchHandler()
        {
            cmd cmd = Get();
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

        public static void Reswitch()
        {
            cmd cmd = Get();
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

        [WishCommand(Command = "bigskip")]

        public static void bigskip()
        {
            cmdSwitchFlipper(nameof(BigSkip));
        }

        [WishCommand(Command = "showallstealth")]
        public static void ShowAllSteath()
        {
            cmdSwitchFlipper(nameof(showStealthed));
            cmdSwitchFlipper(nameof(showStealthy));
            cmdSwitchFlipper(nameof(ShowActiveStealthed));
            msg("ShowAlLStealth");
        }

        [WishCommand(Command = "showFrenzy")]

        public static void ShowFrenzy()
        {
            cmdSwitchFlipper(nameof(showFrenzy));
        }

        [WishCommand(Command = "showvitae")]

        public static void Showvitae()
        {
            cmdSwitchFlipper(nameof(showvitae));
        }

        [WishCommand(Command = "showstealthed")]

        public static void ShowStealthedMethod()
        {
            cmdSwitchFlipper(nameof(showStealthed));
        }

        [WishCommand(Command = "showasm")]

        public static void ShowASM()
        {
            cmdSwitchFlipper(nameof(ShowActiveStealthed));
        }

        [WishCommand(Command = "showGO")]

        public static void ShowGOTo()
        {
            cmdSwitchFlipper(nameof(showGO));
        }

        [WishCommand(Command = "showFeed")]

        public static void FeedsHow()
        {
            cmdSwitchFlipper(nameof(showFeed));
        }

        [WishCommand(Command = "ShowStatus")]

        public static void ShowBloodStatus()
        {
            cmdSwitchFlipper(nameof(showStatus));
        }

        [WishCommand(Command = "showstealthy")]

        public static void ShowStealthyStatus()
        {
            cmdSwitchFlipper(nameof(showStealthy));
        }

        [WishCommand(Command = "showHumanity")]

        public static void ShowHumanityValue()
        {
            cmdSwitchFlipper(nameof(showHumanity));
        }

        [WishCommand(Command = "showCombat")]

        public static void showCombatValue()
        {
            cmdSwitchFlipper(nameof(showCombat));
        }

        [WishCommand(Command = "skip")]

        public static void skip()
        {
            cmdSwitchFlipper(nameof(Skip));
        }


        [WishCommand(Command = "skipabeat")]

        public static void skipabeat()
        {
            cmdSwitchFlipper(nameof(SkipABeat));
        }


        [WishCommand("showturns")]

        public static void showTurns()
        {
            cmdSwitchFlipper(nameof(showturns));
        }

        [WishCommand("refreshme")]

        public static void refreshme()
        {
            cmdSwitchFlipper(nameof(refresh));
        }

        [WishCommand("shownames")]

        public static void shownames()
        {
            cmdSwitchFlipper(nameof(names));
        }

        [WishCommand("showbloodtype")]

        public static void ShowBloodType()
        {
            cmdSwitchFlipper(nameof(showbloodtype));
        }

        [WishCommand("showwater")]

        public static void showwater() => cmdSwitchFlipper(nameof(showWater));

        #endregion




        #region Vampirism Wishes

        [WishCommand("splatterme")]

        public static void splatterme() => The.Player.Bloodsplatter();

        [WishCommand("checkfrenzy")]

        public static void checkfrenzy()
        {
            var frenzy = The.Player.GetPart<TheBeast>();
            if (frenzy != null)
            {
                msg("CHecking frenzy registry");
                Log("\nBREAK:::::");
                Log(frenzy.TargetRegistry);
            }
        }

        [WishCommand(Command = "onehum")]

        public static void Onehum() => The.Player.GetPart<Humanity>().Score = 1;

        [WishCommand("vampirize")]

        public static void Vampirize()
        {
            GameObject GO = The.Player;
            if (GO.CmdTarget("vampirize", out var pick))
            {
                GO.RequireMutation<Vampirism>();
                IComponent<GameObject>.AddPlayerMessage("Vampirized");
            }
        }



        [WishCommand("unvampirize")]

        public static void Unvampirize()
        {
            GameObject GO = The.Player;
            if (GO.CmdTarget("unvampirize", out var pick))
            {
                pick.RemoveMutation<Vampirism>();
                IComponent<GameObject>.AddPlayerMessage("UnVampirized");
            }
        }

        [WishCommand("autowin")]

        public static void autowin()
        {
            StaticSwitchFlipper<Nexus.Attack.FeedCommand>(nameof(Nexus.Attack.FeedCommand.AutoWin));
        }

        [WishCommand("badliquid")]
        public static void badliquid()
        {
            if (The.Player.CmdTarget("badliquid", out var pick))
            {
                var BadLiquids = new Nexus.Bite.Bite().BadLiquids.Copy();
                int range = WikiRng.Next(0, BadLiquids.Length - 1);
                string liquid = BadLiquids[range].Item1;
                pick.ApplyEffect(new LiquidCovered(liquid, 2, 50));
                cmd.msg($"badliquified {pick} {liquid} {range}");
            }
        }


        [WishCommand("lust")]

        public static void lust()
        {
            StaticSwitchFlipper<Vitae>(nameof(Vitae.AntiPuke));

        }


        [WishCommand("autolevel")]

        public static void AutoLevel()
        {
            StaticSwitchFlipper<IFeeding>(nameof(IFeeding.AutoLevel));
        }


        [WishCommand("ReadCopy")]
        public static void ReadCopy()
        {
            Cell cell = The.Player.PickDirection("ReadCopy");
            if (cell != null)
            {
                int copies = 0;
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    if (cell.Objects[i].TryGetPart<GameObjectCopy>(out var copy))
                    {
                        copies++;
                        copy.Read();
                    }
                }
                if (copies > 0)
                    AddPlayerMessage($"Read {copies} copies. See Player.log");
                else
                    AddPlayerMessage($"No objects found with GameObjectCopy part in cell.");
            }
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

        [WishCommand("getstaticplayer")]

        public static void getstaticplayer() => GetStaticPlayer();

        [WishCommand("showstaticplayer")]

        public static void GetStaticPlayer()
        {
            cmd.msg($"{DeathHandler.Player?.DisplayName} sent");
        }

        #endregion

        #region Scanneras

        [WishCommand("scanfor", null)]

        public static void scanfor(string value)
        {
            Cell cell = The.Player.PickDirection("scanfor");
            if (cell != null)
            {
                msg("scanfor " + value);
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    GameObject obj = cell.Objects[i];
                    for (int x = 0; x < obj.PartsList.Count; x++)
                    {
                        IPart part = obj.PartsList[i];
                        if (part.Name == value)
                        {
                            msg($"{obj} haspart {value}");
                            return;
                        }
                    }
                }
            }
        }

        [WishCommand("scan")]
        public static void ScanWish()
        {
            GameObject GO = The.Player;
            Cell cell = GO.PickDirection("scan");
            if (cell != null)
            {
                for (int i = 0; i < cell.Objects.Count; i++)
                    ScanObject(cell.Objects[i]);
                AddPlayerMessage("ScanComplete");
            }
        }


        [WishCommand("checkfx")]

        public static void Checkfx() => showfx();

        [WishCommand("showfx")]

        public static void showfx()
        {
            GameObject GO = The.Player;
            Cell Cell = GO.PickDirection("showfx");
            if (Cell != null)
            {
                msg("Checking FX! see log");
                for (int i = 0; i < Cell.Objects.Count; i++)
                {
                    Log($"\n {Cell.Objects[i]}. {Cell.Objects[i].ID} EFFECTS");
                    Log(Cell.Objects[i].Effects);
                }
            }
        }

        [WishCommand("getzlevel")]
        public static void GetZ()
        {
            IComponent<GameObject>.AddPlayerMessage($"{The.Player.CurrentZone.GetZoneZ()}");
        }

        [WishCommand("showtypes")]

        public static void ShowTypes()
        {
            Cell Cell = The.Player.PickDirection("showtypes");
            if (Cell != null)
            {
                AddPlayerMessage("Logging types");
                for (int i = 0; i < Cell.Objects.Count; i++)
                {
                    var type = Cell.Objects[i].GetType();
                    MetricsManager.LogInfo($"{type}, {type.Namespace}, {type.BaseType}, {type.GUID}");
                }
            }
        }

        [WishCommand("checktags")]

        public static void CheckTags()
        {
            if (The.Player.CmdTarget("checktags", out var pick))
            {
                var bp = pick.GetBlueprint();
                Log($"LOGGING BLUEPRINT OF {pick}, {pick.ID}");
                Log("TAGS");
                foreach (var obj in bp.Tags)
                    Log(obj);
                Log("PROPS");
                foreach (var obj in bp.Props)
                    Log(obj);
            }
        }

        [WishCommand("checkprops")]
        public static void CheckStringProperties()
        {
            GameObject GO = The.Player;
            Cell cell = GO.PickDirection("checkprops");
            List<GameObject> objects = cell.GetObjects();
            if (cell != null)
            {
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    GameObject obj = cell.Objects[i];
                    Log($"\n {obj}, {obj.ID} CHECKING STRING PROPS");
                    Log(obj.Property);
                }
            }
        }

        #endregion





        #region Liquids



        [WishCommand("liquify")]
        public static void liquify(string liquid)
        {
            if (The.Player.CmdTarget("liquify", out var pick))
            {
                pick.ApplyEffect(new LiquidCovered(liquid, 2, 50));
                cmd.msg($"Liquified {liquid} {pick}");
            }
        }

        [WishCommand("slimify")]
        public static void slimify() => liquify("slime");

        [WishCommand("bloodify")]
        public void bloodify() => liquify("blood");

        #endregion


        #region Spawn and Kill



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

        [WishCommand("killall")]

        public static void KillAll()
        {
            GameObject GO = The.Player;
            Zone zone = GO.CurrentZone;
            for (int y = 0; y < zone.Height; y++)
            {
                for (int x = 0; x < zone.Width; x++)
                {
                    Cell cell = zone.Map[x][y];
                    for (int i = 0; i < cell.Objects.Count; i++)
                    {
                        GameObject obj = cell.Objects[i];
                        if (obj != GO && obj.HasPart<Combat>())
                        {
                            obj.TakeDamage(1000, GO, "KIllAll");
                        }
                    }
                }

            }
            AddPlayerMessage("AllKilled");
        }

        #endregion



        [WishCommand(Command = "spawnsleeper")]

        public static void SpawnSleeper()
        {
            if (The.Player.LocalCells(out var cells))
            {
                for (int i = 0; i < 3; i++)
                {
                    GameObject Object = GameObject.Create("WatervineFarmerJoppa");
                    Object.ApplyEffect(new Asleep(100));
                    cells[i].AddObject(Object);
                }
            }
        }

        #region Misc

        [WishCommand(Command = "removebleed")]

        public static void removebleed()
        {
            Cell cell = The.Player.PickDirection("RemoveBleed");
            if (cell != null)
            {
                GameObject Victim = cell.GetCombatTarget(The.Player);
                Victim.RemoveEffect<Bleeding>();
                Victim.RemoveEffect<LiquidCovered>();
            }
        }


        [WishCommand(Command = "refresh")]
        public static void Refresh()
        {
            ActivatedAbilities activatedAbilities = The.Player.ActivatedAbilities;
            if (activatedAbilities != null)
            {
                foreach (ActivatedAbilityEntry value in activatedAbilities.AbilityByGuid.Values)
                {
                    value.Cooldown = 0;
                }
            }
        }

        [WishCommand("removepart", null)]

        public static void removepart(string value)
        {
            if (The.Player.RemovePart(value))
                msg("removed");
            else
                msg(value + " not on player");
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


        [WishCommand(Command = "hurt")]
        public static void Hurt()
        {
            Cell cell = The.Player.PickDirection("RemoveBleed");
            GameObject Victim = cell.GetCombatTarget(The.Player);
            Victim.ApplyEffect(new Bleeding("1", 20));
        }

        [WishCommand(Command = "mod")]
        public static void Developer()
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

        public static void Boxmein()
        {
            if (The.Player.LocalCells(out var cells))
            {
                for (int i = 0; i < cells.Count; i++)
                {
                    cells[i].AddObject("GlassWall");
                }
            }
        }

        [WishCommand(Command = "lavawall")]

        public static void LavaWall()
        {
            if (The.Player.LocalCells(out var cells))
            {
                for (int i = 0; i < cells.Count; i++)
                {
                    cells[i].AddObject("LavaPuddle");
                }
            }
        }

        [WishCommand(Command = "confuseme")]

        public static void ConfuseMe()
        {
            The.Player.ApplyEffect(new Confused(10, 1, 1));
            IComponent<GameObject>.AddPlayerMessage("Confuseme");
        }

        [WishCommand(Command = "tough")]
        public static void Tough()
        {
            The.Player.GetStat("Toughness").AddShift(100, null, false);
            IComponent<GameObject>.AddPlayerMessage("Tough");
        }


        #endregion



        #region Helpers (not commands)

        public static GameObject Spawn(Cell cell, string param) => cell.getClosestEmptyCell().AddObject(GameObject.Create(param));

        static void ScanObject(GameObject obj)
        {
            Log($"\nSTART {obj.DisplayName}, ID_{obj.ID}");
            Log($"Blueprint, {obj.Blueprint}");
            Log($"Level, {obj.Level}");
            Log("\n--STRING AND LONG PROPS--");
            Log(obj.Property);
            Log("\n-INTPROPS");
            Log(obj.IntProperty);
            Log("\n--PARTS--");
            Log(obj.PartsList);
            Log("\n-EFFECTS-");
            Log(obj.Effects);
            Log($"END {obj.DisplayName}, ID_{obj.ID}");
        }

        static void cmdSwitchFlipper(string nameOf) //nameof(Boolean)
        {
            var cmd = Get();
            InstanceSwitchFlipper(nameOf, cmd);
        }

        static void StaticSwitchFlipper<T>(string nameOf) where T : class
        {
            var field = typeof(T).GetField(nameOf, BindingFlags.Static | BindingFlags.Public);
            SwitchFlipper<T>(field, nameOf, null);
        }

        static void InstanceSwitchFlipper<T>(string nameOf, T obj) where T : class
        {
            var field = typeof(T).GetField(nameOf, BindingFlags.Instance | BindingFlags.Public);
            SwitchFlipper(field, nameOf, obj);
        }

        static void SwitchFlipper<T>(FieldInfo field, string nameOf, T obj) where T : class
        {
            if (field?.GetValue(obj) is bool value)
            {
                value = !value;
                msg($"{nameOf} is {(value ? "on" : "off")}.");
                field.SetValue(obj, value);
            }
            else
                AddPlayerMessage($"field {nameOf} does not exist in {typeof(T)} or is not bool");
        }

        static cmd Get()
        {
            return The.Player.RequirePart(new cmd(The.Player.IsVampire()));
        }

        public static void Log<T>(IList<T> obj)
        {
            for (int i = 0; i < obj.Count; i++)
            {
                Log($"{obj[i]}");
            }
        }

        public static void Log<TKey, TValue>(IDictionary<TKey, TValue> obj)
        {
            foreach (var item in obj)
            {
                Log(item);
            }
        }

        public static void Log<TKey, TValue>(KeyValuePair<TKey, TValue> obj) => Log($"{obj.Key}, {obj.Value}");

        public static new void Log(string text) => MetricsManager.LogInfo(text);

        public static void msg(string text) => IComponent<GameObject>.AddPlayerMessage(text);
        public static void msg(string text, char color)
        {
            string message = "{{" + color + "|" + text + "}}";
            msg(message);
        }
        public static void msg(string text, char color, string text2)
        {
            string message = "{{" + color + "|" + text + "}} " + text2;
            msg(message);
        }

        public void Properties()
        {
            Properties("");
        }

        //ADMN.Properties(ParentObject, Stealthed, "Stealthed", ActiveStealthFeed, "ActiveStealth");
        public void Properties(GameObject tgt, bool Stealthed, bool ActiveStealth)
        {
            string message = "";
            if (showStealthed)
                message += TextMaker(Stealthed, "Stage1", 'B');
            if (ShowActiveStealthed)
                message += TextMaker(ActiveStealth, "Stage2", 'b');
            Properties(message);
        }

        public void Properties(GameObject tgt, bool stealth, string type)
        {
            string message = "";
            message += TextMaker(stealth, type, 'b');
            Properties(message);
        }

        static char MakeColor(bool state)
        {
            if (state == true)
                return 'G';
            else
                return 'R';
        }

        static string TextMaker(int value, string text, char choosecolor)
        {
            string Other = TextMaker(text, choosecolor);
            string New = $"{value}" + " " + Other;
            return New;
        }

        static string TextMaker(bool state, string text, char choosecolor)
        {
            string color = "{{" + MakeColor(state) + "|";
            string msg = color + state + "}}, " + TextMaker(text, choosecolor);
            return msg;
        }

        static string TextMaker(string text, string text2, char choosecolor)
        {
            string other = TextMaker(text, choosecolor);
            string New = text2 + " " + other;
            return New;

        }

        static string TextMaker(string text, char choosecolor)
        {
            string msg = "{{" + choosecolor + "|" + text + "}}; ";
            return msg;
        }

        void Properties(string text)
        {
            var tgt = ParentObject;
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
                if (BeatSkipValue >= 2)
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

        public void Names(GameObject witness, char color)
        {
            if (witness is not null && witness.ID is not null && witness.CurrentCell is not null)
            {
                bool los = witness.HasLOSTo(ParentObject, false);
                string name = witness.ToString();
                string msg = "{{" + color + "|_ID - }}" + $"{name}, " + "{{M|ID:}}" + $"{witness.ID}, " + "{{G|D:}}" + $"{witness.DistanceTo(ParentObject.CurrentCell)}, " + "{{O|L:}}" + $"{witness.CurrentCell.GetLight()}, " + "{{C|LOS:}}" + los;
                if (witness == ParentObject)
                {
                    msg = "{{R sequence|PLAYER}}" + "{{O|LIGHT_}}" + $"{witness.CurrentCell.GetLight()}";
                }
                IComponent<GameObject>.AddPlayerMessage(msg);
            }

        }

        public void ShowStealthList(Dictionary<GameObject, bool> ActiveWitnesses)
        {
            foreach (var obj in ActiveWitnesses)
            {
                if(obj.Value == true)
                Names(obj.Key, 'W');
            }
        }

        #endregion


    }
}