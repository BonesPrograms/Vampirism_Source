using XRL.World.Effects;
using System;
using Nexus.Rules;
using Nexus.Core;
using XRL.World.Parts.Mutation;
using XRL.Core;
using XRL.World.Parts;
using System.Collections.Generic;
using XRL.Collections;
using System.Linq;

namespace XRL.World.Effects
{

    [Serializable]
    public class BatformFX : VampireFX
    {
        public const string COMMAND_NAME = "cmdTrueformBat";
        public override Type SpellType => typeof(BatformFX);
        public bool AlreadyHadWings;
        public bool WasLessThanTen;
        public string OldTile;
        public string OldDisplayName;
        public string OldColorString;
        public string OldRenderString;
        public string OriginalBlueprint;
        public string OldAnatomy = default;
        public string LastDescriptionShort = default;
        public int CurrentWingLevel = default;
        public int OriginalFactionFeeling = default;
        public int OriginalCapOverride = default;
        public int XPTrack = default;

        [NonSerialized]
        public List<GameObject> OriginallyEquippedObjects = new();

        [NonSerialized]
        public GameObject OldBody;
        public BatformFX()
        {
            DisplayName = "";
            Duration = 9999;
        }

        public BatformFX(GameObject Object) : this()
        {
            OldBody = Object;
        }

        public override void Write(GameObject Basis, SerializationWriter Writer)
        {
            Writer.Write(OriginallyEquippedObjects.Count);
            for (int i = 0; i < OriginallyEquippedObjects.Count; i++)
                Writer.WriteGameObject(OriginallyEquippedObjects[i]);
            Writer.WriteGameObject(OldBody);
            base.Write(Basis, Writer);
        }

        public override void Read(GameObject Basis, SerializationReader Reader)
        {
            int length = Reader.ReadInt32();
            for (int i = 0; i < length; i++)
                OriginallyEquippedObjects.Add(Reader.ReadGameObject());
            OldBody = Reader.ReadGameObject();
            base.Read(Basis, Reader);
        }

        public override bool WantEvent(int ID, int cascade)
        {
            if (ID == BeforeRenderEvent.ID && !UI.Options.GetOptionBool(OPTIONS.NIGHTBEAST)) //because nightbeast already does this for you
                return true;
            if (ID == CommandEvent.ID || ID == AwardedXPEvent.ID)
                return true;
            return base.WantEvent(ID, cascade);
        }

        public override bool HandleEvent(AwardXPEvent E)
        {
            XPTrack += E.Amount;
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeRenderEvent E)
        {
            AddLight(21, LightLevel.Dimvision);
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == COMMAND_NAME)
            {
                Cast();
            }
            return base.HandleEvent(E);
        }
        void Cast()
        {
            if (Cast("transform"))
            {
                ExpendBlood();
                if (RealityCheck(Object.CurrentCell))
                {
                    Duration = 0;
                }
            }
        }

        public override bool Apply(GameObject Object)
        {
            Suppress(true);
            ID = AddMyActivatedAbility("True Form", COMMAND_NAME, $"{SpellType}", $"{CLASS}", null, "\u0002");
            Transform();
            return true;
        }

        public override void Remove(GameObject Object)
        {
            Suppress(true);
            RemoveMyActivatedAbility(ref ID);
            Revert();
        }


        void Transform()
        {
            //  OriginallyEquippedObjects = UnequipAndGet();
            //       SaveLook();
            //   ChangeBody();
            //    ChangeBlueprint();
            //    ChangeLook();
            //     ChangeWings();
            //     ChangeDescription();
            //      AutoEquip();
            //  CommandEvent.Send(base.Object, Wings.COMMAND_NAME);
            Suppress(false);
            AddPlayerMessage("You assume the form of a bat.");
            //  Object.ParticleBlip("&K-", 10, 0L);
            //    Object.Brain.AddFactionFeeling(BATFORM.FACTION, 100);
        }
        public void Revert()
        {
            //    Unequip();
            //   RevertBlueprint();
            RevertBodyAlt();
            //    RevertWings();
            //    RevertDescription();
            //   RevertLook();
            //    TryReEquip();
            Suppress(false);
            Metamorphosis.TransferInventory(Object, OldBody, false);
            AddPlayerMessage("You revert to your true form.");
            Object.ParticleBlip("&K-", 10, 0L);
            //    base.Object.Brain.SubtractFactionFeeling(BATFORM.FACTION, 100);
        }

        #region Reversion
        void RevertWings()
        {
            if (!AlreadyHadWings)
                base.Object.RemoveMutation<Wings>();
            else if (WasLessThanTen)
            {
                var Wings = base.Object.GetPart<Wings>();
                Wings.BaseLevel = CurrentWingLevel;
                Wings.CapOverride = -1;
            }
        }

        void RevertLook()
        {
            base.Object.Render.ColorString = OldColorString;
            base.Object.Render.RenderString = OldRenderString;
            base.Object.Render.Tile = OldTile;
            base.Object.DisplayName = OldDisplayName;
        }

        void RevertBody()
        {
            // base.Object.Body.Anatomy = OldAnatomy;
            Object = OldBody;
        }

        void RevertDescription()
        {
            if (VerifyObject())
            {
                var Description = base.Object.GetPart<Description>();
                Description.Short = LastDescriptionShort;
            }
        }

        void RevertBlueprint()
        {
            Object.SetBlueprint(GameObjectFactory.Factory.Blueprints[OriginalBlueprint]);
        }

        #endregion

        #region Transformation
        void ChangeWings()
        {
            //     if (base.Object.TryGetPart<Wings>(out var Wings))
            //     {
            //       HadWings(Wings);
            //    }
            //    else
            //     {
            AlreadyHadWings = false;
            var wings = Object.AddMutation<Wings>(10);
            wings.CapOverride = 10;
            //     }
        }

        void HadWings(Wings Wings)
        {
            AlreadyHadWings = true;
            if (Wings.Level < 10)
            {
                WasLessThanTen = true;
                CurrentWingLevel = Wings.BaseLevel;
                Wings.BaseLevel = 10;
                Wings.CapOverride = 10;
            }
        }
        void ChangeLook()
        {

            Object.DisplayName = "vampiric bat";
            Object.Render.Tile = "Assets_Content_Textures_Creatures_sw_bat.bmp";
            Object.Render.ColorString = "K";
            Object.Render.RenderString = "b";
        }

        void SaveLook()
        {
            OldColorString = base.Object.Render.ColorString;
            OldRenderString = base.Object.Render.RenderString;
            OldTile = base.Object.Render.Tile;
            OldDisplayName = base.Object.DisplayName;
        }

        void ChangeBody()
        {
            OldAnatomy = base.Object.Body.Anatomy;
            Object.Body.Anatomy = "Quadruped";
        }

        void RevertBodyAlt()
        {
            var cell = Object.CurrentCell;
            cell.RemoveObject(Object);
            cell.AddObject(OldBody);
            XRLCore.Core.Game.Player.Body = OldBody;
            Object.MakeInactive();
            OldBody.MakeActive();
            OldBody.AwardXP(XPTrack);
        }

        void ChangeBlueprint()
        {
            OriginalBlueprint = Object.Blueprint;
            Object.SetBlueprint(GameObjectFactory.Factory.Blueprints["Bat"]); //final piece of the puzzle, this allows you to get bat sounds which are stored as tags and only accessible through their blueprint
        }

        void ChangeDescription()
        {
            if (VerifyObject())
            {
                var Description = Object.GetPart<Description>();
                LastDescriptionShort = Description.Short;
                Description.Short = "It sheaths itself in filmy wings.";
            }
        }

        #endregion

        bool VerifyHasObject(GameObject obj)
        {
            return Object.Inventory.InventoryContains(obj);//|| currentlyEquipped.Contains(obj);
        }

        void AutoEquip()
        {
            for (int i = 0; i < OriginallyEquippedObjects.Count; i++)
                Object.AutoEquip(OriginallyEquippedObjects[i]);
        }

        void Unequip()
        {
            List<GameObject> currentlyEquipped = Object.GetEquippedObjects();
            for (int i = 0; i < currentlyEquipped.Count; i++)
                currentlyEquipped[i].ForceUnequip(true);
        }

        void TryReEquip()
        {
            for (int i = 0; i < OriginallyEquippedObjects.Count; i++)
            {
                GameObject obj = OriginallyEquippedObjects[i];
                if (VerifyHasObject(obj))
                {
                    if (UI.Popup.ShowYesNo($"{obj == null} obj == null, {Object == null} Object == null, {GameObject.Validate(ref obj)} validate obj ") == XRL.UI.DialogResult.Yes)
                        Object.AutoEquip(obj);
                }
            }
        }
        List<GameObject> UnequipAndGet()
        {
            List<GameObject> equipped = new();
            Object.SafeForeachEquippedObject(delegate (GameObject obj)
            {
                equipped.Add(obj);
                obj.ForceUnequip(true);
            });
            return equipped;
        }

        bool VerifyObject()
        {
            DeathHandler.Security();
            return Object != DeathHandler.Player; //so that we do not reset your description, only npc body descriptions
        }

        static void Suppress(bool value)
        {
            UI.Popup.Suppress = value;
            Messages.MessageQueue.Suppress = value;
        }
    }
}

namespace XRL.World.Parts
{
    [Serializable]
    public class BatformSpell : VampiricSpell //the original version used metamorphosis to turn you into a literal bat, but your party would not sync and i didnt feel like trying to fix that
    {                                           //because the alternative is easier: fake transformation as you see in this type. there are also tons of other issues like mutations and stats not easily being synced so this is optimal
        public override int Cooldown => BATFORM.COOLDOWN;
        public bool Transformed => ParentObject.Blueprint == "Bat";
        static readonly string[] Stats =
        {
            "Strength", "Ego", "Agility", "Toughness", "Intelligence", "Willpower", "Level"
        };
        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == EffectRemovedEvent.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }
        public override void CollectStats(Templates.StatCollector stats)
        {
            stats.CollectCooldownTurns(MyActivatedAbility(SpellID), Cooldown);
        }

        public override void AddSpell()
        {
            SpellID = AddMyActivatedAbility(BATFORM.ABILITY_NAME, BATFORM.COMMAND_NAME, $"{CLASS}", null, "\u009f");
        }
        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == BATFORM.COMMAND_NAME && Checks.Prerequisites(ParentObject, BATFORM.ABILITY_NAME, "transform"))
            {
                if (!ParentObject.IsRealityDistortionUsable())
                    RealityStabilized.ShowGenericInterdictMessage(ParentObject);
                else if (!Transformed)
                    Cast();
                else
                    UI.Popup.Show("You are already in batform!");
            }
            return base.HandleEvent(E);
        }


        void Cast()
        {
            if (Cast("transform"))
            {
                ExpendBlood();
                if (RealityCheck(ParentObject.CurrentCell))
                {
                    ChangeBody();
                }
            }
        }

        //            extract.ForEachIndexAssign(i => (mutations[i].Name, mutations[i].Level));
        static (string, int)[] GetMutations(List<BaseMutation> mutations)
        {
            var extract = new (string, int)[mutations.Count]; //originally i was going to restrict this to only mental mutations which is why this method exists
            extract.AssignEachIndexed(delegate (int i) { (string, int) tuple = new() { Item1 = mutations[i].Name, Item2 = mutations[i].Level }; return tuple; });
            return extract;
        }
        static Effect[] GetFX(Rack<Effect> fx, GameObject bat)
        {
            Effect[] effects = new Effect[fx.Count];
            effects.AssignEachIndexed(delegate (int i) { return fx[i].DeepCopy(bat); });
            return effects;
        }
        static void SyncStats(GameObject bat, Dictionary<string, Statistic> stats) => Stats.ForEach(delegate (string stat) { bat.Statistics[stat] = new Statistic(stats[stat]); });
        static void SyncFX(GameObject bat, Effect[] fx) => fx.ForEach(delegate (Effect e) { bat.ApplyEffect(e); });
        static void SyncMutations(Mutations part, (string, int)[] mutations) => mutations.ForEach(delegate ((string, int) m) { part.AddMutation(m.Item1, m.Item2); });
        static void MakeBat(GameObject bat, GameObject obj)
        {
            UI.Popup.Suppress = true;
            SyncStats(bat, obj.Statistics);
            // SyncFX(bat, GetFX(bat.Effects, obj));
            SyncMutations(bat.RequirePart<Mutations>(), GetMutations(obj.GetPart<Mutations>().MutationList));
            SyncBlood(obj.GetIntProperty(Nexus.Properties.FLAGS.BLOOD_VALUE), bat.GetPart<Vitae>());
            UI.Popup.Suppress = false;
        }

        static void SyncBlood(int bloodValue, Vitae v)
        {
            v.Blood = bloodValue;
        }

        static void SyncCooldowns(ActivatedAbilities abilities)
        {
            GameObject obj = new();
            // abilities.
        }
        static void ChangeBody()
        {
            GameObject obj = The.Player;
            GameObject bat = GameObject.Create("Bat");
            Metamorphosis.TransferInventory(obj, bat);
            bat.ApplyEffect(new BatformFX(obj));
            Cell cell = obj.CurrentCell;
            XRLCore.Core.Game.ActionManager.RemoveActiveObject(obj);
            XRLCore.Core.Game.ActionManager.AddActiveObject(bat);
            cell.RemoveObject(obj);
            cell.AddObject(bat);
            obj.MakeInactive();
            bat.MakeActive();
            XRLCore.Core.Game.Player.Body = bat;
            MakeBat(bat, obj);

        }

    }
}
