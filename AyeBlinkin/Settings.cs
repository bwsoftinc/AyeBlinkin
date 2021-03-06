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

namespace AyeBlinkin 
{
    internal static class Settings
    {
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
                get => (int)this[nameof(Left)];
                set => this[nameof(Left)] = value;
            }

            [ConfigurationProperty("Top", DefaultValue="-1")]
            public int Top {
                get => (int)this[nameof(Top)];
                set => this[nameof(Top)] = value;
            }

            [ConfigurationProperty("Mirror", DefaultValue="false")]
            public bool Mirror { get => (bool)this[nameof(Mirror)]; set {
                this[nameof(Mirror)] = value;
                NotifyPropertyChanged();
                AyeBlinkin.StartStopScreenThread();
            } }

            [ConfigurationProperty("Red", DefaultValue="0")]
            public int Red { get => (int)this[nameof(Red)]; set { 
                this[nameof(Red)] = value;
                NotifyPropertyChanged();
            } }

            [ConfigurationProperty("Green", DefaultValue="0")]
            public int Green { get => (int)this[nameof(Green)]; set { 
                this[nameof(Green)] = value;
                NotifyPropertyChanged();
            } }

            [ConfigurationProperty("Blue", DefaultValue="0")]
            public int Blue { get => (int)this[nameof(Blue)]; set {
                this[nameof(Blue)] = value;
                NotifyPropertyChanged();
            } }

            [ConfigurationProperty("Brightness", DefaultValue="0")]
            public int Brightness { get => (int)this[nameof(Brightness)]; set { 
                this[nameof(Brightness)] = value;
                NotifyPropertyChanged();
            } }

            [ConfigurationProperty("HorizontalLEDs", DefaultValue="25")]
            public int HorizontalLEDs { get => (int)this[nameof(HorizontalLEDs)]; set { 
                this[nameof(HorizontalLEDs)] = value;
                TotalLeds = value + (VerticalLEDs * 2);
                NotifyPropertyChanged();
            } }

            [ConfigurationProperty("VerticalLEDs", DefaultValue="10")]
            public int VerticalLEDs { get => (int)this[nameof(VerticalLEDs)]; set {
                this[nameof(VerticalLEDs)] = value;
                TotalLeds = (value * 2) + HorizontalLEDs;
                NotifyPropertyChanged();
            } }

            [ConfigurationProperty("AdapterId", DefaultValue="")]
            public string AdapterId { get => (string)this[nameof(AdapterId)]; set {
                int val;
                if(int.TryParse(value, out val)) {
                    this[nameof(AdapterId)] = value;
                    NotifyPropertyChanged();

                    var list = new BindingList<KeyValuePair<string, string>>();
                    foreach(var x in DeviceEnumerator.GetDisplays(val))
                        list.Add(new KeyValuePair<string, string>(
                            x.Substring(0, x.Length - x.LastIndexOf("(") - 1),x));

                    Displays = list;
                }
                else {
                    this[nameof(AdapterId)] = String.Empty;
                    Displays = new BindingList<KeyValuePair<string, string>>();
                }
            } }

            [ConfigurationProperty("DisplayId", DefaultValue="")]
            public string DisplayId {
                get => (string)this[nameof(DisplayId)];
                set {
                    this[nameof(DisplayId)] = value;
                    NotifyPropertyChanged();

                    var val = Displays.FirstOrDefault(d => d.Key == DisplayId).Value;
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

                        Settings.Model.Scale = scale;
                        SettingsWindowSize = new Size(x, y);
                    }
                }
            }

            [ConfigurationProperty("SerialComId", DefaultValue="")]
            public string SerialComId { get => (string)this[nameof(SerialComId)]; set {
                this[nameof(SerialComId)] = value;
                NotifyPropertyChanged();
                
                if(string.IsNullOrWhiteSpace(value))
                    Mirror = false;

                AyeBlinkin.StartStopComThread();
            } }

            [ConfigurationProperty("Audio", DefaultValue="false")]
            public bool Audio { get => (bool)this[nameof(Audio)]; set {
                this[nameof(Audio)] = value;
                NotifyPropertyChanged();
            } }

            //internal bindings (not saved to config)
            public int Scale { get; set; } = 1;            
            public int PreviewLED { get; set; }
            public int TotalLeds { get; set; } = 45;
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

            private BindingList<KeyValuePair<string, string>> adapters = new BindingList<KeyValuePair<string, string>>();
            public BindingList<KeyValuePair<string, string>> Adapters { get => adapters; set {
                adapters.Clear();

                foreach(var kvp in value)
                    adapters.Add(kvp);

                NotifyPropertyChanged();

                AdapterId = adapters.Any(kvp => kvp.Key == AdapterId)? AdapterId 
                    : adapters.FirstOrDefault().Key ?? string.Empty;
            } }

            private BindingList<KeyValuePair<string, string>> displays = new BindingList<KeyValuePair<string, string>>();
            public BindingList<KeyValuePair<string, string>> Displays { get => displays; set {
                displays.Clear();

                foreach(var kvp in value)
                    displays.Add(kvp);

                NotifyPropertyChanged();

                DisplayId = displays.Any(kvp => kvp.Key == DisplayId)? DisplayId 
                    : displays.FirstOrDefault().Key ?? string.Empty;
            } }

            private BindingList<KeyValuePair<string, string>> serialComs = new BindingList<KeyValuePair<string, string>>();
            public BindingList<KeyValuePair<string, string>> SerialComs { get => serialComs; set {
                serialComs.Clear();
                
                foreach(var kvp in value)
                    serialComs.Add(kvp);

                NotifyPropertyChanged();

                SerialComId = serialComs.Any(kvp => kvp.Key == SerialComId)? SerialComId 
                    : serialComs.FirstOrDefault().Key ?? string.Empty;
            }}
        }

        private static Configuration manager = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        internal static void SaveToDisk() => manager.Save(ConfigurationSaveMode.Modified);
        private static readonly BindingContext context = new BindingContext();
        internal static BindingModel Model { get; private set; }
        private static readonly BindingSource bindingSource;

        static Settings() 
        {
            //init configuration
            Model = manager.Sections.Get(nameof(BindingModel)) as BindingModel;
            if(Model == null)
            {
                manager.Sections.Add(nameof(BindingModel), new BindingModel());
                SaveToDisk();
                Model = manager.Sections.Get(nameof(BindingModel)) as BindingModel;
            }

            //init volatile copies
            Model.TotalLeds = (Model.VerticalLEDs * 2) + Model.HorizontalLEDs;
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
            control.VisibleChanged += (s, e) =>
            {
                var binding = control.DataBindings.Cast<Binding>().FirstOrDefault(x => x.PropertyName == controlField);
                if(binding != null)
                    control.DataBindings.Remove(binding);

                if(control is ComboBox && !controlField.Equals("Width"))
                {
                    var cb = control as ComboBox;
                    cb.DataSource = null;
                    cb.Items.Clear();
                    cb.DataSource = new BindingSource(bindingSource.DataSource, modelField);
                    cb.DisplayMember = "Value";
                    cb.ValueMember = "Key";
                    modelField = modelField.Substring(0, modelField.Length-1) + "Id";
                }

                control.DataBindings.Add(new Binding(controlField, bindingSource, modelField, true, DataSourceUpdateMode.OnPropertyChanged));
            };
        }
    }
}