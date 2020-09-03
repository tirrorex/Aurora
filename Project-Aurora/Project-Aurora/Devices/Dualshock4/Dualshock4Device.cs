﻿using Aurora.Settings;
using Aurora.Utils;
using DS4Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace Aurora.Devices.Dualshock
{
    internal class DS4Container
    {
        public readonly DS4Device device;
        public readonly ConnectionType connectionType;
        public readonly Color RestoreColor;

        public int Battery { get; private set; }
        public double Latency { get; private set; }
        public bool Charging { get; private set; }

        public Color sendColor;
        public DS4HapticState state;

        public DS4Container(DS4Device _device)
        {
            device = _device;
            connectionType = device.getConnectionType();
            device.Report += OnDeviceReport;
            device.StartUpdate();
        }

        private void OnDeviceReport(object sender, EventArgs e)
        {
            Battery = device.Battery;
            Latency = device.Latency;
            Charging = device.Charging;

            if (ColorsEqual(sendColor, state.LightBarColor))
                return;

            state.LightBarExplicitlyOff = sendColor.R == 0 && sendColor.G == 0 && sendColor.B == 0;
            state.LightBarColor = new DS4Color(sendColor);
            device.pushHapticState(state);
        }

        public void Disconnect(bool stop)
        {
            device.Report -= OnDeviceReport;
            state.LightBarExplicitlyOff = RestoreColor.R == 0 && RestoreColor.G == 0 && RestoreColor.B == 0;
            state.LightBarColor = new DS4Color(RestoreColor);
            device.pushHapticState(state);
            if (stop)
            {
                device.DisconnectBT();
                device.DisconnectDongle();
            }
            device.StopUpdate();
        }

        public string GetDeviceDetails()
        {
            var connectionString = connectionType switch
            {
                ConnectionType.BT => "Bluetooth",
                ConnectionType.SONYWA => "DS4 Wireless adapter",
                ConnectionType.USB => "USB",
                _ => "",
            };

            return $"over {connectionString} {(Charging ? "⚡" : "")} 🔋{Battery}% Latency: {Latency:0.00}ms";
        }

        private bool ColorsEqual(Color clr, DS4Color ds4clr)
        {
            return clr.R == ds4clr.red &&
                    clr.G == ds4clr.green &&
                    clr.B == ds4clr.blue;
        }
    }

    public class DualshockDevice : DefaultDevice
    {
        public override string DeviceName => "Sony DualShock 4(PS4)";

        protected override string DeviceInfo =>
            string.Join(", ", devices.Select((dev, i) => $"#{i + 1} {dev.GetDeviceDetails()}"));

        public int Battery => devices.FirstOrDefault()?.Battery ?? -1;
        public double Latency => devices.FirstOrDefault()?.Latency ?? -1;
        public bool Charging => devices.FirstOrDefault()?.Charging ?? false;

        private readonly List<DS4Container> devices = new List<DS4Container>();
        private DeviceKeys key;

        public DualshockDevice()
        {
            HidSharp.DeviceList.Local.Changed += DeviceListChanged;
        }

        private void DeviceListChanged(object sender, HidSharp.DeviceListChangedEventArgs e)
        {
            DS4Devices.findControllers();
            if (DS4Devices.getDS4Controllers().Count() != devices.Count)
                Reset();
        }

        public override bool Initialize()
        {
            if (IsInitialized)
                return true;

            key = Global.Configuration.VarRegistry.GetVariable<DeviceKeys>($"{DeviceName}_devicekey");
            DS4Devices.findControllers();

            foreach (var controller in DS4Devices.getDS4Controllers())
                devices.Add(new DS4Container(controller));

            return IsInitialized = devices.Count > 0;
        }

        public override void Shutdown()
        {
            if (!IsInitialized)
                return;

            foreach (var dev in devices)
                dev.Disconnect(Global.Configuration.VarRegistry.GetVariable<bool>($"{DeviceName}_disconnect_when_stop"));

            DS4Devices.stopControllers();
            devices.Clear();
            IsInitialized = false;
        }

        public override bool UpdateDevice(Dictionary<DeviceKeys, Color> keyColors, DoWorkEventArgs e, bool forced = false)
        {
            if (keyColors.TryGetValue(key, out var clr))
            {
                foreach (var dev in devices)
                {
                    dev.sendColor = ColorUtils.CorrectWithAlpha(clr);
                    if (dev.device.isDisconnectingStatus())
                    {
                        Reset();
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        protected override void RegisterVariables(VariableRegistry variableRegistry)
        {
            variableRegistry.Register($"{DeviceName}_devicekey", DeviceKeys.Peripheral, "Key to Use", DeviceKeys.MOUSEPADLIGHT15, DeviceKeys.Peripheral_Logo);
            variableRegistry.Register($"{DeviceName}_disconnect_when_stop", false, "Disconnect when Stopping");
        }
    }
}
