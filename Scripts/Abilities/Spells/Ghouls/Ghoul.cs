using XRL.World.Parts;
using XRL.World.AI;
using System;
using Nexus.Rules;
using Nexus.Spells;
using XRL.World.Effects;
using Nexus.Blood;
using Nexus.Core;

namespace XRL.World.Effects
{

    [Serializable]
    public class RelinquishedGhoul : IScribedEffect
    {
        public string MasterID;
        public long TimeOfDeath;
        public long TurnsUntilDeath;
        public int LastRate;
        int DebuffRate => PercentTimeRemaining switch
        {
            > 75 => 0,
            > 50 => 2,
            > 25 => 4,
            _ => 6
        };
        long PercentTimeRemaining => TimeOfDeath - The.Game.Turns / TurnsUntilDeath * 100;
        public RelinquishedGhoul()
        {
            DisplayName = "{{r|bloodstarved}}";
            Duration = 9999;
        }
        public RelinquishedGhoul(GameObject Master) : this()
        {
            MasterID = Master.ID;
            TurnsUntilDeath = WikiRng.Next(1000, 3000); //they will die pretty quickly
            TimeOfDeath = The.Game.Turns + TurnsUntilDeath;
        }
        public override string GetDescription()
        {
            return "{{r|bloodstarved}}";
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == EndTurnEvent.ID || ID == ApplyEffectEvent.ID)
                return Duration > 0;
            return base.WantEvent(ID, Cascade);
        }

        public override bool HandleEvent(ApplyEffectEvent E)
        {
            if (E.Effect is EnthralledGhoul ghoul)
            {
                if (ghoul.Master.ID == MasterID)
                {
                    Duration = 0;
                    return true;
                }
                else
                {
                    UI.Popup.Show($"You are not {Object.t()}'s master and {Object.it} does not desire your blood.");
                    return false;
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EndTurnEvent E)
        {
            if (The.Game.Turns == TimeOfDeath)
                Object.Die();
            else if (CheckDebuffRate())
                Weaken();
            return base.HandleEvent(E);
        }

        public override void Remove(GameObject Object)
        {
            foreach (var obj in EnthralledGhoul.BuffedStats)
                StatShifter.RemoveStatShift(Object, obj);
        }

        bool CheckDebuffRate()
        {
            int debuff = DebuffRate;
            if (LastRate == debuff) //so the ghoul only experiences debuffs in the same moment that the debuff rate increases
                return false;
            LastRate = debuff;
            if (The.Player.HasLOSTo(Object))
                AddPlayerMessage($"{Object.t()} is starving for " + "{{r|blood}}!");
            return true;
        }

        void Weaken()
        {
            int debuff = DebuffRate;
            foreach (var obj in EnthralledGhoul.BuffedStats)
            {
                StatShifter.SetStatShift(obj, debuff);
            }
        }
    }
    [Serializable]

    public class BuffedEnthralledGhoul : IScribedEffect
    {
        public int BuffTime = Ghoul.BUFFTIME;

        public BuffedEnthralledGhoul()
        {
            DisplayName = "{{r|blooddrunk}}";
        }

        public override string GetDescription()
        {
            return "{{r|blooddrunk}}";
        }

        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == EndTurnEvent.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }

        public override bool HandleEvent(EndTurnEvent E)
        {
            if (BuffTime > 0)
                BuffTime--;
            else
                Debuff();
            return base.HandleEvent(E);
        }

        public override void Remove(GameObject Object)
        {
            Object.GetEffect<EnthralledGhoul>().Buffed = false;
        }

        void Debuff()
        {
            foreach (var obj in EnthralledGhoul.BuffedStats)
                StatShifter.RemoveStatShift(Object, obj);
            Duration = 0;
        }

    }


    [Serializable]
    public class EnthralledGhoul : IScribedEffect, IBloodMetabolism
    {
        public static string[] BuffedStats = { "Strength", "Agility", "Toughness", "Willpower", "Ego", "Hitpoints" };
        public GameObject Master;
        public bool Buffed;
        public int _Blood = Nexus.Rules.Vitae.BLOOD_GLUTTONOUS;
        public int Blood
        {
            get => _Blood;
            set
            {
                _Blood = value;
            }
        }

        public bool Bloodstarved;
        public string LastStatus; //used by Metab but i store it here for easy serialization : otherwise you will get notifications about ghoul bloodlevel every time you join if theyre thirsty
        GhoulBloodMetabolism _Metab;
        GhoulBloodMetabolism Metab => _Metab ??= new(this);
        public EnthralledGhoul()
        {
            DisplayName = "{{r|ghoul}}";
        }
        public EnthralledGhoul(GameObject Master) : this()
        {
            this.Master = Master;
            base.Duration = 9999;
        }
        public override string GetDescription()
        {
            return "{{r|ghoul}}";
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register("ApplyProselytize");
            Registrar.Register("AfterDrank");
            Registrar.Register("AddWater");
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "ApplyProselytize")
            {
                UI.Popup.Show($"{Object.t()} is already enthralled.");
                return false;
            }
            Metab.WaterEvents(E);
            return base.FireEvent(E);
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            if (ID == InduceVomitingEvent.ID || ID == EffectAppliedEvent.ID || ID == EffectRemovedEvent.ID || ID == SingletonEvent<EndTurnEvent>.ID || ID == SingletonEvent<BeforeBeginTakeActionEvent>.ID || ID == ApplyEffectEvent.ID || ID == CanApplyEffectEvent.ID || ID == SingletonEvent<BeginTakeActionEvent>.ID)
                return true;
            return base.WantEvent(ID, Cascade);
        }

        public override bool HandleEvent(InduceVomitingEvent E)
        {
            Metab.VomitEventHandler(E);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeTakeActionEvent E)
        {
            Metab.Cycle();
            return base.HandleEvent(E);
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

        public void Buff(int Roll)
        {
            foreach (var obj in EnthralledGhoul.BuffedStats)
                StatShifter.SetStatShift(obj, Roll);
            Object.Heal(Roll);
            Object.ApplyEffect(new BuffedEnthralledGhoul());
            Buffed = true;
        }

        public bool IsGhoulOf(GameObject Target)
        {
            return Target == Master;
        }

        public override bool Apply(GameObject Object)
        {
            if (!GameObject.Validate(Master))
                return false;
            if (Object.Brain == null)
                return false;
            Object.BecomeCompanionOf(Master);
            return true;
        }
        public override void Remove(GameObject Object)
        {
            Object.PartyLeader = null;
            Object.Target = null;
            Object.ApplyEffect(new RelinquishedGhoul(Master));
            base.Remove(Object);
        }

    }
}
