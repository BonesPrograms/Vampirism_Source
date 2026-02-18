using XRL.World.Effects;
using System;
using Nexus.Rules;
using Nexus.Core;
using XRL.World.Parts.Mutation;
using XRL.Core;
using XRL.World.Parts;
using System.Collections.Generic;


namespace XRL.World.Parts
{
    [Serializable]
    public class BatformSpell : VampiricSpell //the original version used metamorphosis to turn you into a literal bat, but your party would not sync and i didnt feel like trying to fix that
    {                                           //because the alternative is easier: fake transformation as you see in this type. there are also tons of other issues like mutations and stats not easily being synced so this is optimal
        public override Type SpellType => typeof(BatformSpell);
        public override int Cooldown => BATFORM.COOLDOWN;
        public List<GameObject> EquippedObjects;
        public bool Transformed = false;
        public bool AlreadyHadWings = false;
        public int CurrentWingLevel = default;
        public string OldTile = null;
        public string OldDisplayName = null;
        public string OldAnatomy = null;
        public string LastDescriptionShort = null;
        public string OldColorString = null;
        public string OldRenderString = null;
        public int OriginalFactionFeeling = default;
        public int OriginalCapOverride = default; //may need this for mod compat later
        public (string, string)[] OriginalProps =
        {
          ("DeathSounds", null),
          ("TakeDamageSound", null),
          ("AmbientIdleSound", null),
          ("PunchSound", null),
          ("PrimaryLimbType", "Hand") //i dont want this to be removed if null (thats what we do here), it is usually going to be the hand for humanoids
        };
        static readonly (string, string)[] BatProps =
        {
          ("DeathSounds", "Sounds/Creatures/VO/sfx_creature_animal_bat_vo_die"),
          ("TakeDamageSound", "Sounds/Creatures/VO/sfx_creature_animal_bat_vo_hurt"),
          ("AmbientIdleSound", "Sounds/Creatures/VO/sfx_creature_animal_bat_vo_idle"),
          ("PunchSound", "Sounds/Creatures/VO/sfx_creature_animal_bat_vo_attack"),
          ("PrimaryLimbType", "Face")
        };
        public override bool WantEvent(int ID, int cascade)
        {
            if (ID == BeforeRenderEvent.ID && Transformed && !UI.Options.GetOptionBool(OPTIONS.NIGHTBEAST)) //because nightbeast already does this for you
                return true;
            return base.WantEvent(ID, cascade);
        }

        public override bool HandleEvent(BeforeRenderEvent E)
        {
            AddLight(21, LightLevel.Dimvision);
            return base.HandleEvent(E);
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
                        Transform();
                    else
                        Revert();
                }
            }
        }

        public void Transform()
        {
            EquippedObjects = new(8);
            Suppress(true);
            UnequipAndList(); //unequip equipped items before changing body anatomy otherwise its gone
            ChangeLook();
            ChangeWings();
            ChangeBody();
            ChangeDescription();
            BackupProperties();
            SetProperties(BatProps);
            TryReequip();
            Suppress(false);
            AddPlayerMessage("You assume the form of a bat.");
            ParentObject.ParticleBlip("&K-", 10, 0L);
            ParentObject.Brain.AddFactionFeeling(BATFORM.FACTION, 100);
            CommandEvent.Send(ParentObject, Wings.COMMAND_NAME);
            Transformed = true;
        }
        public void Revert()
        {
            Suppress(true);
            RevertLook();
            RevertWings();
            RevertBody();
            RevertEquipment();
            RevertDescription();
            SetProperties(OriginalProps);
            Suppress(false);
            AddPlayerMessage("You revert to your true form.");
            ParentObject.ParticleBlip("&K-", 10, 0L);
            ParentObject.Brain.SubtractFactionFeeling(BATFORM.FACTION, 100);
            Transformed = false;
            EquippedObjects = null;
        }

        #region Reversion

        void RevertWings()
        {
            if (!AlreadyHadWings)
                ParentObject.RemoveMutation<Wings>();
            else
            {
                var Wings = ParentObject.GetPart<Wings>();
                Wings.BaseLevel = CurrentWingLevel;
                Wings.CapOverride = -1;
            }
        }

        void RevertLook()
        {
            ParentObject.Render.ColorString = OldColorString;
            ParentObject.Render.RenderString = OldRenderString;
            ParentObject.Render.Tile = OldTile;
            ParentObject.DisplayName = OldDisplayName;
            OldDisplayName = null;
            OldTile = null;
            OldColorString = null;
            OldRenderString = null;
        }

        void RevertBody()
        {
            ParentObject.Body.Anatomy = OldAnatomy;
        }

        void RevertEquipment()
        {
            for (int i = 0; i < EquippedObjects.Count; i++)
            {
                GameObject obj = EquippedObjects[i];
                obj.ForceUnequip(true);
                ParentObject.AutoEquip(obj);
            }
        }

        void RevertDescription()
        {
            if (VerifyObject())
            {
                var Description = ParentObject.GetPart<Description>();
                Description.Short = LastDescriptionShort;
                LastDescriptionShort = null;
            }
        }

        #endregion

        #region Transformation
        void ChangeWings()
        {
            if (ParentObject.TryGetPart<Wings>(out var Wings))
            {
                AlreadyHadWings = true;
                if (Wings.Level < 10)
                {
                    CurrentWingLevel = Wings.Level;
                    Wings.BaseLevel = 10;
                    Wings.CapOverride = 10;
                }
            }
            else
            {
                AlreadyHadWings = false;
                var wings = ParentObject.AddMutation<Wings>(10);
                wings.CapOverride = 10;
            }
        }
        void ChangeLook()
        {
            OldColorString = ParentObject.Render.ColorString;
            OldRenderString = ParentObject.Render.RenderString;
            OldTile = ParentObject.Render.Tile;
            OldDisplayName = ParentObject.DisplayName;
            ParentObject.DisplayName = "bat";
            ParentObject.Render.Tile = "Assets_Content_Textures_Creatures_sw_bat.bmp";
        }

        void ChangeBody()
        {
            OldAnatomy = ParentObject.Body?.Anatomy;
            ParentObject.Body.Anatomy = BATFORM.ANATOMY;
        }

        void ChangeDescription()
        {
            if (VerifyObject())
            {
                var Description = ParentObject.GetPart<Description>();
                LastDescriptionShort = Description.Short;
                Description.Short = "It sheaths itself in filmy wings.";
            }
        }

        void UnequipAndList()
        {
            ParentObject.ForeachEquippedObject(List);
            void List(GameObject x)
            {
                EquippedObjects.Add(x);
                x.ForceUnequip(true);
            }
        }

        void TryReequip()
        {
            for (int i = 0; i < EquippedObjects.Count; i++)
            {
                GameObject obj = EquippedObjects[i];
                ParentObject.AutoEquip(obj, false, false, true);
            }
        }

        #endregion

        void SetProperties((string, string)[] Properties)
        {
            for (int i = 0; i < 4; i++)
            {
                var item = Properties[i];
                ParentObject.SetStringProperty(item.Item1, item.Item2, true);
            }
        }

        void BackupProperties()
        {
            for (int i = 0; i < 4; i++)
            {
                if (ParentObject.TryGetStringProperty(OriginalProps[i].Item1, out var value))
                {
                    OriginalProps[i].Item2 = value;
                }
            }
        }

        bool VerifyObject()
        {
            DeathHandler.Security();
            return ParentObject != DeathHandler.Player; //so that we do not reset your description, only npc body descriptions
        }

        static void Suppress(bool value)
        {
            UI.Popup.Suppress = value;
            Messages.MessageQueue.Suppress = value;
        }


    }
}