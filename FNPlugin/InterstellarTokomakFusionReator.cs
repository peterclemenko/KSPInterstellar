﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    [KSPModule("Tokamak Reactor")]
    class InterstellarTokamakFusionReator : InterstellarFusionReactor
    {
        [KSPField(isPersistant = true)]
        public int fuel_mode = 0;

        [KSPField(isPersistant = false)]
        public float powerRequirements;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Plasma Heating Consumption")]
        public string tokomakPower;

        // 
        protected bool fusion_alert = false;
        protected double power_consumed;
        protected float plasma_ratio = 1.0f;

        // properties

        public override float MaximumThermalPower { get { return base.MaximumThermalPower * Mathf.Pow(plasma_ratio, 4.0f); } }

        public override float MinimumPower { get { return MaximumPower * minimumThrottle; } }

        public override string TypeName { get { return (isupgraded ? upgradedName != "" ? upgradedName : originalName : originalName) + " Reactor"; } }

        public override bool IsNeutronRich {  get { return !current_fuel_mode.Aneutronic; }  }

        public float HeatingPowerRequirements { get { return current_fuel_mode == null ? powerRequirements : (float)(powerRequirements * current_fuel_mode.NormalisedPowerRequirements); } }

        [KSPEvent(guiActive = true, guiName = "Switch Fuel Mode", active = false)]
        public void SwapFuelMode() {
            fuel_mode++;
            if (fuel_mode >= fuel_modes.Count) {
                fuel_mode = 0;
            }
            current_fuel_mode = fuel_modes[fuel_mode];
        }

        public override void OnUpdate() {
            if (getCurrentResourceDemand(FNResourceManager.FNRESOURCE_MEGAJOULES) > getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES) && getResourceBarRatio(FNResourceManager.FNRESOURCE_MEGAJOULES) < 0.1 && IsEnabled && !fusion_alert) {
                ScreenMessages.PostScreenMessage("Warning: Fusion Reactor plasma heating cannot be guaranteed, reducing power requirements is recommended.", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                fusion_alert = true;
            } else {
                fusion_alert = false;
            }
            Events["SwapFuelMode"].active = isupgraded;
            tokomakPower = PluginHelper.getFormattedPowerString(power_consumed);
            base.OnUpdate();
        }

        public override void OnFixedUpdate() {
            base.OnFixedUpdate();
            if (IsEnabled) {
                power_consumed = consumeFNResource(HeatingPowerRequirements * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;
                if (power_consumed < HeatingPowerRequirements) power_consumed += part.RequestResource("ElectricCharge", (HeatingPowerRequirements - power_consumed) * 1000 * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime / 1000;
                plasma_ratio = (float)(power_consumed / HeatingPowerRequirements);
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (state != StartState.Editor) breedtritium = true;
            base.OnStart(state);
        }

        public override string getResourceManagerDisplayName() {
            return TypeName;
        }

        public override int getPowerPriority() {
            return 1;
        }

        protected override void setDefaultFuelMode()
        {
            current_fuel_mode = (fuel_mode < fuel_modes.Count) ? fuel_modes[fuel_mode] : fuel_modes.FirstOrDefault();
        }

    }
}
