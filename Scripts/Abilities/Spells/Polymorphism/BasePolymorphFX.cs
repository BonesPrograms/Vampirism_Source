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
    public abstract class BasePolymorphFX : IScribedEffect
    {
        [NonSerialized]
        public GameObjectBlueprint Blueprint; //does not need to be serialized, just needs to be there on application for Transform to access
        public string FormName;
        public string TargetFaction;
        public int FactionFeeling;
        //4 fields above may be assigned in constructor
        //fields below will be overwritten, do not assign
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

        public BasePolymorphFX()
        {
            Duration = 9999;
            DisplayName = "";
        }

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

        void VerifyInitialization()
        {
            if (Blueprint == null)
                throw new Exception($"Blueprint not assigned in {GetType().Name}'s constructor!");
            if (!Blueprint.DescendsFrom("Creature"))
                throw new Exception($"Blueprint assigned in {GetType().Name} does not descend from Creature!");
        }
        public override sealed bool Apply(GameObject Object)
        {
            VerifyInitialization();
            Transform();
            return true;
        }

        public override sealed void Remove(GameObject Object)
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
            VerifyFormName();
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

        void VerifyFormName()
        {
            if (FormName.IsNullOrEmpty())
            {
                FormName = Object.DisplayName;
                MetricsManager.LogModInfo(ModManager.GetMod("vampirism"), $"{GetType().Name}: FormName not assigned, defaulting to displayname.");
            }
        }
        void ChangeLook()
        {
            var part = Blueprint.GetPart(nameof(Parts.Render));
            base.Object.DisplayName = part.GetParameterString("DisplayName");
            base.Object.Render.Tile = part.GetParameterString("Tile");
            base.Object.Render.ColorString = part.GetParameterString("TileColor");
            base.Object.Render.RenderString = part.GetParameterString("DetailColor");
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
            base.Object.Body.Anatomy = Blueprint.GetPartParameter<string>(nameof(Body), "Anatomy");
        }

        void ChangeBlueprint()
        {
            OriginalBlueprint = Object.Blueprint;
            Object.SetBlueprint(Blueprint); //final piece of the puzzle, this allows you to get bat sounds which are stored as tags and only accessible through their blueprint
        }

        void ChangeDescription()
        {
            if (VerifyObject())
            {
                var description = base.Object.GetPart<Description>();
                LastDescriptionShort = description.Short;
                description.Short = Blueprint.GetPartParameter<string>(nameof(Description), "Short");
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