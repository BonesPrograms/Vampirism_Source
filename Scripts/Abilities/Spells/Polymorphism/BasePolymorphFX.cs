using XRL.World.Effects;
using System;
using Nexus.Rules;
using Nexus.Core;
using XRL.World.Parts.Mutation;
using System.Linq;
using XRL.World.Anatomy;
using XRL.World.Parts;
using System.Collections.Generic;
using AiUnity.NLog.Core.Targets;
using Qud.API;

namespace XRL.World.Effects
{

    [Serializable]
    public abstract class BasePolymorphFX : Effect
    {
        public abstract string NewDescription {get;}
        public abstract string NewDisplayName { get; }
        public abstract string NewRenderTile { get; }
        public abstract string NewColorString { get; }
        public abstract string NewRenderString { get; }
        public abstract string FormName { get; }
        public abstract string BlueprintName { get; }
        public abstract string AnatomyName { get; }
        public virtual string TargetFaction { get; }
        public virtual int FactionFeeling { get; }
        public string OldTile;
        public string OldDisplayName;
        public string OldColorString; //changing color isnt actually working right now but it will one day i assure ye
        public string OldRenderString;
        public string OriginalBlueprint;
        public string LastDescriptionShort;

        [NonSerialized]
        public List<GameObject> OriginallyEquippedObjects = new();

        [NonSerialized]
        public GameObject OldObject;
        public override void Write(GameObject Basis, SerializationWriter Writer)
        {
            Writer.Write(OriginallyEquippedObjects.Count);
            for (int i = 0; i < OriginallyEquippedObjects.Count; i++)
                Writer.WriteGameObject(OriginallyEquippedObjects[i]);
            Writer.WriteGameObject(OldObject);
            base.Write(Basis, Writer);
        }

        public override void Read(GameObject Basis, SerializationReader Reader)
        {
            int length = Reader.ReadInt32();
            for (int i = 0; i < length; i++)
                OriginallyEquippedObjects.Add(Reader.ReadGameObject());
            OldObject = Reader.ReadGameObject();
            base.Read(Basis, Reader);
        }


        public override bool Apply(GameObject Object)
        {
            Transform();
            return true;
        }

        public override void Remove(GameObject Object)
        {
            Revert();
        }

        //Always call base.Revert and base.Transform FIRST, before any of your additional modifications
        //such as adding new parts, mutations, firing events, or whatever it is you want to do
        public virtual void Transform()
        {
            Suppress(true);
            OriginallyEquippedObjects = UnequipAndGet();
            SaveLook();
            ChangeLook();
            ChangeBody();
            ChangeDescription();
            ChangeBlueprint();
            Object.Body.UpdateBodyParts();
            AutoEquip();
            Suppress(false);
            AddPlayerMessage($"You assume the form of a {FormName}.");
            base.Object.ParticleBlip("&K-", 10, 0L);
            if (TargetFaction != null)
                base.Object.Brain.AddFactionFeeling(TargetFaction, FactionFeeling);
        }
        public virtual void Revert()
        {
            Suppress(true);
            Unequip();
            RevertLook();
            RevertDescription();
            RevertBlueprint();
            RevertBody();
            TryReEquip();
            Suppress(false);
            AddPlayerMessage("You revert to your true form.");
            base.Object.ParticleBlip("&K-", 10, 0L);
            if (TargetFaction != null)
                base.Object.Brain.SubtractFactionFeeling(TargetFaction, FactionFeeling);
        }

        #region Transformation

        void ChangeLook()
        {

            base.Object.DisplayName = NewDisplayName;
            base.Object.Render.Tile = NewRenderTile;
            base.Object.Render.ColorString = NewColorString;
            base.Object.Render.RenderString = NewRenderString;
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
            OldObject = Object.DeepCopy(CopyID: true);
            base.Object.Body.Anatomy = AnatomyName;
        }

        void ChangeBlueprint()
        {
            OriginalBlueprint = Object.Blueprint;
            Object.SetBlueprint(GameObjectFactory.Factory.Blueprints[BlueprintName]); //final piece of the puzzle, this allows you to get bat sounds which are stored as tags and only accessible through their blueprint
        }

        void ChangeDescription()
        {
            if (VerifyObject())
            {
                var Description = base.Object.GetPart<Description>();
                LastDescriptionShort = Description.Short;
                Description.Short = NewDescription;
            }
        }

        void AutoEquip()
        {
            for (int i = 0; i < OriginallyEquippedObjects.Count; i++)
                Object.AutoEquip(OriginallyEquippedObjects[i]);
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
            List<GameObject> equipped = new();
            Object.SafeForeachEquippedObject(delegate (GameObject obj)
            {
                if (obj != null)
                {
                    equipped.Add(obj);
                    obj.ForceUnequip(true);
                }
            });
            return equipped;
        }

        #endregion

        #region Reversion

        void RevertLook()
        {
            base.Object.Render.ColorString = OldColorString;
            base.Object.Render.RenderString = OldRenderString;
            base.Object.Render.Tile = OldTile;
            base.Object.DisplayName = OldDisplayName;
        }

        void RevertBody()
        {
            Object.Body = null;
            Object.RemovePart<Body>();
            Object.Body = Object.AddPart(OldObject.Body);
            OldObject.Body.ParentObject = Object;
            OldObject.Body = null;
            OldObject = null;
            Object.Body.UpdateBodyParts();
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

        bool VerifyHasObject(GameObject obj)
        {
            return Object.Inventory.InventoryContains(obj);//|| currentlyEquipped.Contains(obj);
        }

        void Unequip()
        {
            List<GameObject> currentlyEquipped = Object.GetEquippedObjects();
            for (int i = 0; i < currentlyEquipped.Count; i++)
                currentlyEquipped[i].ForceUnequip(true);
        }

        #endregion

        bool VerifyObject()
        {
            DeathHandler.Security();
            return base.Object != DeathHandler.Player; //so that we do not reset your description, only npc body descriptions
        }

        public static void Suppress(bool value)
        {
            UI.Popup.Suppress = value;
            Messages.MessageQueue.Suppress = value;
        }
    }
}