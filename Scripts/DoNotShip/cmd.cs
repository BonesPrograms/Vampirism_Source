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
using Nexus.Stealth;
using System.Linq;
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


    [HarmonyPatch(typeof(VampireBuilder), nameof(VampireBuilder.Make))]
    static class cmd_patch
    {
        [HarmonyPostfix]
        public static void Postfix(GameObject GO)
        {
            if (GO.IsPlayer())
            {
                GO.AddPart(new cmd(true));
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

        public bool IsVampire = default;

        public cmd()
        {

        }

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
        public int BeatSkipValue = default;
        public bool Skip = false;
        public int SkipValue = default;
        public bool BigSkip = false;
        public int BigSkipValue = default;
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
        public override bool HandleEvent(BeforeTakeActionEvent E)
        {
            if (IsVampire)
            {
                if (showStealthed || showStealthy || ShowActiveStealthed)
                    Properties(ParentObject, Nightbeast.StealthStage1, Nightbeast.StealthStage2);
                else
                    Properties();
                if (names == true)
                    ShowStealthList(Nightbeast.Witnesses);
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
            cmdSwitch(nameof(BigSkip));
        }

        [WishCommand(Command = "showallstealth")]
        public static void ShowAllSteath()
        {
            cmdSwitch(nameof(showStealthed));
            cmdSwitch(nameof(showStealthy));
            cmdSwitch(nameof(ShowActiveStealthed));
            msg("ShowAlLStealth");
        }

        [WishCommand(Command = "showFrenzy")]

        public static void ShowFrenzy()
        {
            cmdSwitch(nameof(showFrenzy));
        }

        [WishCommand(Command = "showvitae")]

        public static void Showvitae()
        {
            cmdSwitch(nameof(showvitae));
        }

        [WishCommand(Command = "showstealthed")]

        public static void ShowStealthedMethod()
        {
            cmdSwitch(nameof(showStealthed));
        }

        [WishCommand(Command = "showasm")]

        public static void ShowASM()
        {
            cmdSwitch(nameof(ShowActiveStealthed));
        }

        [WishCommand(Command = "showGO")]

        public static void ShowGOTo()
        {
            cmdSwitch(nameof(showGO));
        }

        [WishCommand(Command = "showFeed")]

        public static void FeedsHow()
        {
            cmdSwitch(nameof(showFeed));
        }

        [WishCommand(Command = "ShowStatus")]

        public static void ShowBloodStatus()
        {
            cmdSwitch(nameof(showStatus));
        }

        [WishCommand(Command = "showstealthy")]

        public static void ShowStealthyStatus()
        {
            cmdSwitch(nameof(showStealthy));
        }

        [WishCommand(Command = "showHumanity")]

        public static void ShowHumanityValue()
        {
            cmdSwitch(nameof(showHumanity));
        }

        [WishCommand(Command = "showCombat")]

        public static void showCombatValue()
        {
            cmdSwitch(nameof(showCombat));
        }

        [WishCommand(Command = "skip")]

        public static void skip()
        {
            cmdSwitch(nameof(Skip));
        }


        [WishCommand(Command = "skipabeat")]

        public static void skipabeat()
        {
            cmdSwitch(nameof(SkipABeat));
        }


        [WishCommand("showturns")]

        public static void showTurns()
        {
            cmdSwitch(nameof(showturns));
        }

        [WishCommand("refreshme")]

        public static void refreshme()
        {
            cmdSwitch(nameof(refresh));
        }

        [WishCommand("shownames")]

        public static void shownames()
        {
            cmdSwitch(nameof(names));
        }

        [WishCommand("showbloodtype")]

        public static void ShowBloodType()
        {
            cmdSwitch(nameof(showbloodtype));
        }

        [WishCommand("showwater")]

        public static void showwater() => cmdSwitch(nameof(showWater));

        #endregion




        #region Vampirism Wishes

        [WishCommand(Command = "crocs")]

        public static void crocs()
        {
            if (The.Player.LocalCells(out var cells))
            {
                foreach (var cell in cells)
                {
                    cell.AddObject(GameObject.Create("Croc"));
                }
            }
        }

        [WishCommand(Command = "vampire")]

        public static void GiveVampire()
        {
            var obj = Spawn(The.Player.CurrentCell, "WatervineFarmerJoppa");
            obj.RequireMutation<Vampirism>();
        }

        [WishCommand(Command = "coffindbg")]

        public static void CoffinDbg()
        {
            Switch<CoffinSpell>(nameof(CoffinSpell.ShowDebug), null);
        }

        [WishCommand(Command = "automote")]

        public static void AutoMote()
        {
            Switch<MoteOfHumanity>(nameof(MoteOfHumanity.MoteAutoMemory), null);
        }

        [WishCommand(Command = "mote")]

        public static void FreeMote()
        {
            Spawn(The.Player.CurrentCell, "MoteOfHumanity");
        }

        [WishCommand("freemote")]
        public static void Freemote() => Switch<DeathHandler>(nameof(DeathHandler.FreeMote), null);

        [WishCommand("findspotter")]

        public static void FindSpotter()
        {
            The.Player.CurrentZone.CombatObjects(x => x.HasEffect<Spotter>()).ForEach(x => msg($"{x}"));
            msg(nameof(FindSpotter));
        }

        [WishCommand("staticstealth")]

        public static void Staticstealth()
        {
            AddPlayerMessage($"{StealthCore.Player.DisplayName}");

        }

        [WishCommand("comparelevels")]

        public static void CompareLevels() //for the diablerie update
        {
            var v = The.Player.GetPart<Vampirism>();
            if (v != null)
            {
                msg($"Level {v.Level}");
                msg($"BaseLevel {v.BaseLevel}");
            }
            else
                msg("Not a vampire");
        }

        [WishCommand("splatterme")] //for testing humanity by bloodletting

        public static void splatterme() => The.Player.Bloodsplatter();

        [WishCommand("checkfrenzy")]

        public static void checkfrenzy() //for testing if the target registry is being cleaned properly
        {
            var frenzy = The.Player.GetPart<TheBeast>();
            if (frenzy != null)
            {
                msg("CHecking frenzy registry");
                Log("\nBREAK:::::");
                Log(frenzy.TargetRegistry);
            }
        }

        [WishCommand("vampirize")]

        public static void Vampirize()
        {
            GameObject GO = The.Player;
            if (GO.CmdTarget("vampirize", out var pick))
            {
                pick.RequireMutation<Vampirism>();
                //    v.Mutate(GO, 1);
                IComponent<GameObject>.AddPlayerMessage("Vampirized");
            }
        }

        [WishCommand("addspell")]

        public static void AddSpell(string text)
        {
            object obj = RequirePart(text);
            if (obj is VampiricSpell spell)
                spell.AddSpell();
            else
                msg($"{text} is not VampiricSpell or is null : {obj == null}");
        }

        [WishCommand("removespell")]

        public static void RemoveSpell(string text)
        {
            var obj = The.Player.GetPart(text);
            if (obj is VampiricSpell spell)
            {
                spell.RemoveSpell();
                msg($"{text} removed");
            }
            else
                msg($"{text} is not VampiricSpell or is null : {obj == null}");


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

        [WishCommand("badliquid")]
        public static void badliquid()
        {
            if (The.Player.CmdTarget("badliquid", out var pick))
            {
                var BadLiquids = new Nexus.Bite.Bite().BadLiquids;
                int range = WikiRng.Next(0, BadLiquids.Length - 1);
                string liquid = BadLiquids[range].Item1;
                pick.ApplyEffect(new LiquidCovered(liquid, 2, 50));
                cmd.msg($"badliquified {pick} {liquid} {range}");
            }
        }

        [WishCommand("autowin")]

        public static void autowin() //for diablerie
        {
            Switch<Nexus.Attack.FeedCommand>(nameof(Nexus.Attack.FeedCommand.AutoWin), null);
        }


        [WishCommand("lust")]

        public static void lust()
        {
            Switch<Vitae>(nameof(Vitae.AntiPuke), null);

        }


        [WishCommand("autolevel")]

        public static void AutoLevel()
        {
            Switch<IFeeding>(nameof(IFeeding.AutoLevel), null);
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

        [WishCommand("r")]

        public static void r() => refreshme();

        [WishCommand(Command = "blueprint")]

        public static void bp2() => bp();

        [WishCommand(Command = "bp")]

        public static void bp()
        {
            var obj = The.Player.GetBlueprint();
            msg($"{obj.Name}");
        }

        [WishCommand(Command = "heal")]

        public static void heal()
        {
            GameObject g = The.Player;
            int basehp = g.baseHitpoints;
            int hp = g.hitpoints;
            AddPlayerMessage($"{basehp} {hp}");
            int heal = basehp - hp;
            The.Player.Heal(heal, true, true, false);
        }

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
            RemovePart(value);
        }

        [WishCommand("addpart", null)]

        public static void addpart(string value)
        {
            RequirePart(value);
        }


        [WishCommand(Command = "hurt")]
        public static void Hurt()
        {
            Cell cell = The.Player.PickDirection("RemoveBleed");
            GameObject Victim = cell.GetCombatTarget(The.Player);
            Victim.ApplyEffect(new Bleeding("1", 20));
        }

        static string[] stats = new string[]
        {
            "Ego", "Intelligence", "Agility", "SP"
        };

        static string[] mutations = new string[]
        {
            nameof(Domination), nameof(Beguiling), nameof(Phasing)
        };

        [WishCommand(Command = "mod")]
        public static void Developer()
        {
            Popup.Suppress = true;
            Mutations m = The.Player.RequirePart<Mutations>();
            for (int i = 0; i < mutations.Length; i++)
            {
                m.AddMutation(mutations[i]);
            }
            for (int i = 0; i < stats.Length; i++)
            {
                The.Player.AddBaseStat(stats[i], 100);
            }
            The.Player.AddSkill<ShortBlades_Bloodletter>();
            The.Player.AddSkill<Physic_AmputateLimb>();
            The.Player.Inventory.AddObject("Battle Axe2");
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

        public static GameObject Spawn(Cell CurrentCell, string param) => CurrentCell.getClosestEmptyCell().AddObject(GameObject.Create(param));

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

        static void cmdSwitch(string nameOf) //nameof(Boolean)
        {
            var cmd = Get();
            Switch(nameOf, cmd);
        }
        static void Switch<T>(string nameOf, T obj) // T obj = null for static 
        {
            BindingFlags flag = obj == null ? BindingFlags.Static : BindingFlags.Instance;
            var field = typeof(T).GetField(nameOf, flag | BindingFlags.Public);
            _Switch(field, nameOf, obj);
        }

        static void _Switch<T>(FieldInfo field, string nameOf, T obj)
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
                if (obj.Value == true)
                    Names(obj.Key, 'W');
            }
        }

        public static object RequirePart(string value)
        {
            value = "XRL.World.Parts." + value;
            Type type = Type.GetType(value, false);
            if (type != null && Activator.CreateInstance(type) is IPart obj)
            {
                msg("requirepart " + value);
                return The.Player.RequirePart(obj);
            }
            else
                msg($"{value} is not IPart or is null : {value == null}");
            return null;
        }

        public static void RemovePart(string value)
        {
            if (The.Player.RemovePart(value))
                msg("removed " + value);
            else
                msg(value + " not on player");
        }

        #endregion


    }
}