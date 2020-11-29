using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;

using SharpDX.DXGI;

namespace AyeBlinkin.DirectX
{
    internal static class DeviceEnumerator
    {
        private static Dictionary<string, string> adapters = null;
        private static Dictionary<string, List<string>> displays = new Dictionary<string, List<string>>();

        internal static BindingList<KeyValuePair<string, string>> GetAdapters(bool refresh = false)
        {
            if(adapters == null || refresh)
                QueryAdapters();

            var list = new BindingList<KeyValuePair<string, string>>();

            foreach(var kvp in adapters)
                list.Add(new KeyValuePair<string, string>(kvp.Key, kvp.Value));

            return list;
        }

        internal static List<string> GetDisplays(int adapterId)
        {
            if(adapters == null)
                QueryAdapters();

            if(!displays.ContainsKey(adapterId.ToString()))
                return new List<string>();

            return displays[adapterId.ToString()].Select(x => new String(x)).ToList();
        }

        private static void QueryAdapters()
        {
            var adapt = new Dictionary<string, string>();
            var displ = new Dictionary<string, List<string>>();
            using(var factory = new Factory1())
            {
                var acount = factory.GetAdapterCount1();
                for(var i = 0; i < acount; i++)
                {
                    using(var adapter = factory.GetAdapter1(i))
                    {
                        var adesc = adapter.Description1;
                        if(adesc.Flags.HasFlag(AdapterFlags.Software))
                            continue;

                        var disp = new List<string>();
                        displ.Add(adesc.DeviceId.ToString(), disp);
                        adapt.Add(adesc.DeviceId.ToString(), adesc.Description);

                        var ocount = adapter.GetOutputCount();
                        for(var j = 0; j < ocount; j++)
                        {
                            using(var output = adapter.GetOutput(i))
                            {
                                var odesc = output.Description;
                                var width = odesc.DesktopBounds.Right - odesc.DesktopBounds.Left;
                                var height = odesc.DesktopBounds.Bottom - odesc.DesktopBounds.Top;
                                disp.Add($"{odesc.DeviceName} ({width} x {height})");
                            }
                        }
                    }
                }
            }
            adapters = adapt;
            displays = displ;
        }

        internal static Output1 GetOutput1(this Adapter1 adapter, string display)
        {
            var count = adapter.GetOutputCount();
            for(var i = 0; i < count; i++)
            {
                using(var output = adapter.GetOutput(i))
                {
                    if(output.Description.DeviceName == display)
                        return output.QueryInterface<Output1>();
                }
            }
            throw new ArgumentException($"Invalid display: {display}");
        }

        internal static Adapter1 GetAdapter(int deviceId)
        {
            using (var factory = new Factory1())
            {
                var count = factory.GetAdapterCount1();
                Adapter1 adapter = null;
                for(var i = 0; i < count; i++)
                {
                    try {
                        adapter = factory.GetAdapter1(i);
                        if(adapter.Description1.DeviceId == deviceId)
                            return adapter;
                        adapter.Dispose();
                    }
                    catch {
                        adapter?.Dispose();
                    }
                }
            }
            throw new ArgumentException($"Invalid adapter {deviceId}");
       }
    }
}