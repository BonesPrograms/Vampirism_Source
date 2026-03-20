using System;
using VampirismSys.Extensions;
using VampirismSys.Rules;
using XRL.World.Parts;

namespace XRL.World.Effects
{
    public abstract class IGhoulEffect : IBeastScribedEffect
    {
        internal string Name { get => DisplayName; set { DisplayName = value; } }
        internal bool Thrall => GetType() == typeof(EnthralledGhoul);
    }

    [Serializable]
    public class RelinquishedGhoul : IGhoulEffect
    {
        public override bool Apply(GameObject Object)
        {
            Object.RemoveEffect<EnthralledGhoul>();
            return base.Apply(Object);
        }
        public RelinquishedGhoul()
        {
            DisplayName = "{{r|masterless}}";
            Duration = 9999;
        }

        public override string GetDescription()
        {
            return "{{r|masterless}}";
        }


    }

    [Serializable]
    public class EnthralledGhoul : IGhoulEffect
    {

        public GameObject Master { get => _master; private init { _master = value; } }
        GameObject _master;
        public EnthralledGhoul()
        {
            DisplayName = "{{r|ghoul}}";
            Duration = 9999;
        }
        internal EnthralledGhoul(GameObject Master) : this()
        {
            this.Master = Master;
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register("ApplyProselytize");
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "ApplyProselytize")
            {
                UI.Popup.Show($"{Object.t()} is already enthralled.");
                return false;
            }
            return base.FireEvent(E);
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == EffectAppliedEvent.ID || ID == EffectRemovedEvent.ID || ID == ApplyEffectEvent.ID || ID == CanApplyEffectEvent.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }
        public override bool HandleEvent(ApplyEffectEvent E)
        {
            if (E.Name == "Beguile")
            {
                UI.Popup.Show($"{Object.t()} is already enthralled.");
                return false;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(CanApplyEffectEvent E)
        {
            if (E.Name == "Beguile")
            {
                UI.Popup.Show($"{Object.t()} is already enthralled.");
                return false;
            }
            return base.HandleEvent(E);
        }

        public override string GetDescription()
        {
            return "{{r|ghoul}}";
        }

        public override bool Apply(GameObject Object)
        {
            Object.BecomeCompanionOf(Master);
            Object.RequirePart<GhoulBloodMetabolism>();
            return true;
        }
        public override void Remove(GameObject Object)
        {
            Object.Brain.Goals.Clear();
            Object.PartyLeader = null;
            Object.Target = null;
            Object.ApplyEffect(new RelinquishedGhoul());
        }

        public bool IsGhoulOf(GameObject Target)
        {
            return Target == Master;
        }

    }


    [Serializable]
    public class BuffedEnthralledGhoul : IBeastScribedEffect
    {
        int Bonus;
        public BuffedEnthralledGhoul()
        {
            Duration = Ghoul.BUFFTIME;
            DisplayName = "{{r|blooddrunk}}";
        }

        public BuffedEnthralledGhoul(int bonus) : this()
        {
            Bonus = bonus;
        }

        public override string GetDescription()
        {
            return "{{r|blooddrunk}}";
        }

        public override bool Apply(GameObject Object)
        {
            GhoulBloodMetabolism.Stats.ForEach(x => StatShifter.SetStatShift(x, Bonus));
            Object.Heal(Bonus);
            AddPlayerMessage($"{Object.t()} goes drunk on " + "{{r|blood}}!");
            return base.Apply(Object);
        }
        public override void Remove(GameObject Object)
        {
            GhoulBloodMetabolism.Stats.ForEach(x => StatShifter.RemoveStatShift(Object, x));
        }
    }

}



