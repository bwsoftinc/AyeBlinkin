using System;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Configuration;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using AyeBlinkin.DirectX;
using AyeBlinkin.CoreAudio;

namespace AyeBlinkin 
{
    internal static class Settings  
    {
        public volatile static int PreviewLED;
        public volatile static int TotalLeds = 6;
        public volatile static int Scale = 1;
        public volatile static IntPtr SettingsHwnd = IntPtr.Zero;

        internal class BindingModel : ConfigurationSection, INotifyPropertyChanged 
        {
            public event PropertyChangedEventHandler PropertyChanged;
            internal SynchronizationContext uiContext { private get; set; }

            internal void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            {
                if(PropertyChanged == null) 
                    return;

                try {
                    if(uiContext == null)
                        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    else
                        uiContext.Post(_ => PropertyChanged(this, new PropertyChangedEventArgs(propertyName)), null);
                }
                catch { }
            }

            [ConfigurationProperty("Left", DefaultValue="-1")]
            public int Left {
                get => (int)this["Left"];
                set => this["Left"] = value;
            }

            [ConfigurationProperty("Top", DefaultValue="-1")]
            public int Top {
                get => (int)this["Top"];
                set => this["Top"] = value;
            }

            [ConfigurationProperty("Mirror", DefaultValue="false")]
            public bool Mirror { get => (bool)this["Mirror"]; set {
                this["Mirror"] = value;
                NotifyPropertyChanged();
                AyeBlinkin.StartStopScreenThread();
            } }

            [ConfigurationProperty("Red", DefaultValue="0")]
            public int Red { get => (int)this["Red"]; set { 
                this["Red"] = value;
                NotifyPropertyChanged();
            } }

            [ConfigurationProperty("Green", DefaultValue="0")]
            public int Green { get => (int)this["Green"]; set { 
                this["Green"] = value;
                NotifyPropertyChanged();
            } }

            [ConfigurationProperty("Blue", DefaultValue="0")]
            public int Blue { get => (int)this["Blue"]; set {
                this["Blue"] = value;
                NotifyPropertyChanged();
            } }

            [ConfigurationProperty("Brightness", DefaultValue="0")]
            public int Brightness { get => (int)this["Brightness"]; set { 
                this["Brightness"] = value;
                NotifyPropertyChanged();
            } }

            [ConfigurationProperty("HorizontalLEDs", DefaultValue="2")]
            public int HorizontalLEDs { get => (int)this["HorizontalLEDs"]; set { 
                this["HorizontalLEDs"] = value;
                Settings.TotalLeds = value + (VerticalLEDs * 2);
                NotifyPropertyChanged();
            } }

            [ConfigurationProperty("VerticalLEDs", DefaultValue="2")]
            public int VerticalLEDs { get => (int)this["VerticalLEDs"]; set {
                this["VerticalLEDs"] = value;
                Settings.TotalLeds = (value * 2) + HorizontalLEDs;
                NotifyPropertyChanged();
            } }

            [ConfigurationProperty("AdapterId", DefaultValue="")]
            public string AdapterId { get => (string)this["AdapterId"]; set {
                int val;
                if(int.TryParse(value, out val)) {
                    this["AdapterId"] = value;
                    NotifyPropertyChanged();

                    Displays = DeviceEnumerator.GetDisplays(val).ToDictionary(
                        x => x.Substring(0, x.Length - x.LastIndexOf("(") - 1), x => x);
                }
                else {
                    this["AdapterId"] = String.Empty;
                    Displays = new Dictionary<string, string>() { [String.Empty] = String.Empty };
                }
            } }

            [ConfigurationProperty("DisplayId", DefaultValue="")]
            public string DisplayId {
                get => (string)this["DisplayId"];
                set {
                    this["DisplayId"] = value;
                    NotifyPropertyChanged();

                    var val = Displays[DisplayId];
                    var i = val.LastIndexOf("(") + 1;
                    if(i==0) return;

                    var dim = val.Substring(i, val.Length - i - 1).Split('x');
                    if(int.TryParse(dim[0].Trim(), out int x) && int.TryParse(dim[1].Trim(), out int y)) {
                        var scale = 1;
                        while(x/2 > MinSettingsWindowWidth && y/2 > MinSettingsWindowHeight) {
                            x/=2;
                            y/=2;
                            scale *= 2;
                        }

                        Settings.Scale = scale;
                        SettingsWindowSize = new Size(x, y);
                    }
                }
            }

            [ConfigurationProperty("SerialComId", DefaultValue="")]
            public string SerialComId { get => (string)this["SerialComId"]; set {
                this["SerialComId"] = value;
                NotifyPropertyChanged();
                AyeBlinkin.StartStopComThread();
            } }

            [ConfigurationProperty("Audio", DefaultValue="false")]
            public bool Audio { get => (bool)this["Audio"]; set {
                this["Audio"] = value;

                if(value)
                    WasapiSoundCapture.Start();
                else
                    WasapiSoundCapture.Stop();

                NotifyPropertyChanged();
            } }

            //internal bindings (not saved to config)
            private const int MinSettingsWindowHeight = 240; //150
            private const int MinSettingsWindowWidth = 320; //300
            public bool BrightBarEnabled {get => !Audio; set { } }
            public bool RGBBarsEnabled { get => !Mirror && PatternId == -2; set { } }
            public bool MirrorOff { get => !Mirror; set { } }

            public Point SettingsWindowLocation { //the settings window screen location
                get => new Point(Left, Top); 
                set { Top = value.Y; Left = value.X; }
            }

            private Size settingsWindowSize = new Size(MinSettingsWindowWidth, MinSettingsWindowHeight);
            public Size SettingsWindowSize { get => settingsWindowSize; set { 
                settingsWindowSize = value;
                NotifyPropertyChanged();
            } }

            private Dictionary<int, string> patterns = new Dictionary<int, string>() { [-2] = "Manual Control" };
            public Dictionary<int, string> Patterns { get => patterns; set {
                patterns = value;
                patterns[-2] = "Manual Control";
                NotifyPropertyChanged();

                if(!value.ContainsKey(patternId))
                    PatternId = -2;
            } }
            
            private volatile int patternId = -2;
            public int PatternId { get => patternId; set {
                if(patterns.ContainsKey(value) && value != patternId) {
                    patternId = value;
                    NotifyPropertyChanged();
                }
            } }

            private Dictionary<string, string> adapters = new Dictionary<string, string>() { [String.Empty] = String.Empty };
            public Dictionary<string, string> Adapters { get => adapters; set {
                if(value.Count > 0) {
                    adapters = value;
                    NotifyPropertyChanged();
                    AdapterId = adapters.ContainsKey(AdapterId)? AdapterId : Adapters.First().Key;
                }
                else {
                    adapters = new Dictionary<string, string>() { [String.Empty] = String.Empty };
                    AdapterId = String.Empty;
                }
            } }

            private Dictionary<string, string> displays = new Dictionary<string, string>() { [String.Empty] = String.Empty };
            public Dictionary<string, string> Displays { get => displays; set {
                if(value.Count > 0) {
                    displays = value;
                    NotifyPropertyChanged();
                    DisplayId = displays.ContainsKey(DisplayId)? DisplayId : displays.First().Key;
                }
                else {
                    displays = new Dictionary<string, string>() { [String.Empty] = String.Empty };
                    DisplayId = String.Empty;
                }
            } }

            private Dictionary<string, string> serialComs = new Dictionary<string, string>() { [String.Empty] = String.Empty };
            public Dictionary<string, string> SerialComs { get => serialComs; set {
                if(value.Count > 0) {
                    serialComs = value;
                    NotifyPropertyChanged();
                    SerialComId = serialComs.ContainsKey(SerialComId)? SerialComId : serialComs.First().Key;
                }
                else {
                    serialComs = new Dictionary<string, string>() { [String.Empty] = String.Empty };
                    SerialComId = String.Empty;
                }
            } }
        }

        private static Configuration manager = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        internal static void SaveToDisk() => manager.Save(ConfigurationSaveMode.Modified);
        private static readonly BindingContext context = new BindingContext();
        internal static BindingModel Model { get; private set; }
        private static readonly BindingSource bindingSource;

        static Settings() {
            //init configuration
            Model = manager.Sections.Get(nameof(BindingModel)) as BindingModel;
            if(Model == null)
            {
                manager.Sections.Add(nameof(BindingModel), new BindingModel());
                SaveToDisk();
                Model = manager.Sections.Get(nameof(BindingModel)) as BindingModel;
            }

            //init volatile copies
            TotalLeds = (Model.VerticalLEDs * 2) + Model.HorizontalLEDs;
            Model.Mirror = false;
            bindingSource = new BindingSource(Model, null);
        }

        internal static void AddBinding(this IBindableComponent component, string controlField, string modelField) 
        {
            var binding = component.DataBindings.Cast<Binding>().FirstOrDefault(x => x.PropertyName == controlField);
            if(binding != null)
                component.DataBindings.Remove(binding);
                
            component.BindingContext = context; 
            component.DataBindings.Add(new Binding(controlField, bindingSource, modelField, false, DataSourceUpdateMode.OnPropertyChanged));
        }

        internal static void AddBinding(this Control control, string controlField, string modelField) 
        {
            control.BindingContext = context;
            control.VisibleChanged += delegate(object sender, EventArgs e) 
            {
                if(control is ComboBox && !controlField.Equals("Width")) 
                {
                    (control as ComboBox).DataSource = new BindingSource(bindingSource.DataSource, modelField);
                    modelField = modelField.Substring(0, modelField.Length-1) + "Id";
                }
                
                var binding = control.DataBindings.Cast<Binding>().FirstOrDefault(x => x.PropertyName == controlField);
                if(binding != null)
                    control.DataBindings.Remove(binding);

                control.DataBindings.Add(new Binding(controlField, bindingSource, modelField, false, DataSourceUpdateMode.OnPropertyChanged));
            };
        }
    }
}