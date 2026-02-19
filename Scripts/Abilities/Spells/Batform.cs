using XRL.World.Effects;
using System;
using Nexus.Rules;
using Nexus.Core;
using XRL.World.Parts.Mutation;
using XRL.Core;
using XRL.World.Parts;
using System.Collections.Generic;
using XRL.World.Anatomy;

namespace XRL.World.Effects
{

    [Serializable]
    public class BatformFX : VampireFX
    {
        public const string COMMAND_NAME = "cmdTrueformBat";
        public override Type SpellType => typeof(BatformFX);
        public bool AlreadyHadWings;
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
        public List<GameObject> OriginallyEquippedObjects;
        public BatformFX()
        {
            DisplayName = "";
            Duration = 9999;
        }

        public override bool WantEvent(int ID, int cascade)
        {
            if (ID == BeforeRenderEvent.ID && !UI.Options.GetOptionBool(OPTIONS.NIGHTBEAST)) //because nightbeast already does this for you
                return true;
            if (ID == CommandEvent.ID)
                return true;
            return base.WantEvent(ID, cascade);
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
            Revert();
            RemoveMyActivatedAbility(ref ID);
        }


        void Transform()
        {
            OriginallyEquippedObjects = UnequipAndGet();
            SaveLook();
            ChangeLook();
            ChangeWings();
            ChangeBody();
            ChangeDescription();
            ChangeBlueprint();
            AutoEquip();
            CommandEvent.Send(base.Object, Wings.COMMAND_NAME);
            Suppress(false);
            AddPlayerMessage("You assume the form of a bat.");
            base.Object.ParticleBlip("&K-", 10, 0L);
            base.Object.Brain.AddFactionFeeling(BATFORM.FACTION, 100);
        }
        public void Revert()
        {
            Unequip();
            RevertLook();
            RevertWings();
            RevertBody();
            RevertDescription();
            RevertBlueprint();
            TryReEquip();
            Suppress(false);
            AddPlayerMessage("You revert to your true form.");
            base.Object.ParticleBlip("&K-", 10, 0L);
            base.Object.Brain.SubtractFactionFeeling(BATFORM.FACTION, 100);
        }

        #region Reversion

        void RevertWings()
        {
            if (!AlreadyHadWings)
                base.Object.RemoveMutation<Wings>();
            else
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
            base.Object.Body.Anatomy = OldAnatomy;
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
            if (base.Object.TryGetPart<Wings>(out var Wings))
            {
                HadWings(Wings);
            }
            else
            {
                AlreadyHadWings = false;
                var wings = base.Object.AddMutation<Wings>(10);
                wings.CapOverride = 10;
            }
        }

        void HadWings(Wings Wings)
        {
            AlreadyHadWings = true;
            if (Wings.Level < 10)
            {
                CurrentWingLevel = Wings.BaseLevel;
                Wings.BaseLevel = 10;
                Wings.CapOverride = 10;
            }
        }
        void ChangeLook()
        {

            base.Object.DisplayName = "vampiric bat";
            base.Object.Render.Tile = "Assets_Content_Textures_Creatures_sw_bat.bmp";
            base.Object.Render.ColorString = "K";
            base.Object.Render.RenderString = "b";
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
            OldAnatomy = base.Object.Body?.Anatomy;
            base.Object.Body.Anatomy = "Quadruped";
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
                var Description = base.Object.GetPart<Description>();
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
                    Object.AutoEquip(obj);
                }
            }
        }
        List<GameObject> UnequipAndGet()
        {
            List<GameObject> equipped = new(12);
            Object.ForeachEquippedObject(UnequipAndAdd);

            void UnequipAndAdd(GameObject x)
            {
                equipped.Add(x);
                x.ForceUnequip(true);
            }

            return equipped;
        }
        bool VerifyObject()
        {
            DeathHandler.Security();
            return base.Object != DeathHandler.Player; //so that we do not reset your description, only npc body descriptions
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
        public override Type SpellType => typeof(BatformSpell);
        public override int Cooldown => BATFORM.COOLDOWN;
        public bool Transformed = false;
        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == EffectRemovedEvent.ID && Transformed)
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

        public override bool HandleEvent(EffectRemovedEvent E)
        {
            if (E.Effect.GetType() == typeof(BatformFX))
                Transformed = false;
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == BATFORM.COMMAND_NAME && Checks.Prerequisites(ParentObject, BATFORM.ABILITY_NAME, "transform"))
            {
                if (!ParentObject.IsRealityDistortionUsable())
                    RealityStabilized.ShowGenericInterdictMessage(ParentObject);
                else
                    Cast();

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
                    if (!Transformed)
                    {
                        ParentObject.ApplyEffect(new BatformFX());
                        Transformed = true;
                    }
                    else
                        UI.Popup.Show("You are already in batform!");
                }
            }
        }

    }
}
