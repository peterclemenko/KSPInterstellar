﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    [KSPModule("IC Fusion Reactor")]
    class InterstellarInertialConfinementReactor : InterstellarFusionReactor, IChargedParticleSource
    {
        [KSPField(isPersistant = true)]
        public int fuel_mode = 0;

        [KSPField(isPersistant = false)]
        public float powerRequirements;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Laser Consumption")]
        public string laserPower;

        protected double power_consumed;
        protected bool fusion_alert = false;
        protected int shutdown_c = 0;

        public override string TypeName { get { return (isupgraded ? upgradedName != "" ? upgradedName : originalName : originalName) + " Reactor"; } }

        public override bool IsNeutronRich { get { return !current_fuel_mode.Aneutronic; } }

        public override float MaximumPower
        {
            get
            {
                float thermal_fuel_factor = current_fuel_mode == null ? 1.0f : (float)Math.Sqrt(current_fuel_mode.NormalisedReactionRate);
                return isupgraded ? upgradedPowerOutput != 0 ? upgradedPowerOutput * (1.0f - ChargedParticleRatio) * thermal_fuel_factor : PowerOutput * (1.0f - ChargedParticleRatio) * thermal_fuel_factor : PowerOutput * (1.0f - ChargedParticleRatio) * thermal_fuel_factor;
            }
        }

        public override float MinimumPower { get { return MaximumPower * minimumThrottle; } }

        public float LaserPowerRequirements { get { return current_fuel_mode == null ? powerRequirements : (float)(powerRequirements * current_fuel_mode.NormalisedPowerRequirements); } }

        [KSPEvent(guiActive = true, guiName = "Switch Fuel Mode", active = false)]
        public void SwapFuelMode() {
            fuel_mode++;
            if (fuel_mode >= fuel_modes.Count) {
                fuel_mode = 0;
            }
            current_fuel_mode = fuel_modes[fuel_mode];
        }
        
        public override bool shouldScaleDownJetISP() {
            return true;
        }

        public override void OnUpdate() {
            if (getCurrentResourceDemand(FNResourceManager.FNRESOURCE_MEGAJOULES) > getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES) && getResourceBarRatio(FNResourceManager.FNRESOURCE_MEGAJOULES) < 0.1 && IsEnabled && !fusion_alert) {
                ScreenMessages.PostScreenMessage("Warning: Fusion Reactor plasma heating cannot be guaranteed, reducing power requirements is recommended.", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                fusion_alert = true;
            } else {
                fusion_alert = false;
            }
            Events["SwapFuelMode"].active = isupgraded;
            laserPower = PluginHelper.getFormattedPowerString(power_consumed);
            base.OnUpdate();
        }

        public override void OnFixedUpdate() {
            base.OnFixedUpdate();
            if (IsEnabled)
            {
                power_consumed = consumeFNResource(LaserPowerRequirements * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;
                if (power_consumed < LaserPowerRequirements) power_consumed += part.RequestResource("ElectricCharge", (LaserPowerRequirements - power_consumed) * 1000 * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime / 1000;
                if (power_consumed < LaserPowerRequirements) shutdown_c++;
                if (shutdown_c > 3) IsEnabled = false;
            } else
            {
                shutdown_c = 0;
            }
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
