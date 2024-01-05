using System.Collections.Concurrent;

namespace RWLib
{
    public class RWRailDriverMonitor
    {
        public class MonitorState
        {
            internal HashSet<string> controlValuesToPrint = new HashSet<string>();
            internal string[] controllerList = new string[0];
            internal bool printAllControlValues = true;
            internal List<float> controlValues = new List<float>();
            internal List<KeyValuePair<int, float>> changedControlIdx = new List<KeyValuePair<int, float>>();

            public IEnumerable<KeyValuePair<string, float>> ControlValues
            {
                get
                {
                    for (int i = 0; i < controllerList.Length; i++)
                    {
                        var name = controllerList[i];
                        var value = controlValues[i];

                        yield return KeyValuePair.Create(name, value);
                    }
                }
            }

            public IEnumerable<KeyValuePair<string, float>> ChangedControls
            {
                get
                {
                    for (int i = 0; i < changedControlIdx.Count; i++)
                    {
                        var idx = changedControlIdx[i].Key;

                        var name = controllerList[idx];
                        var value = controlValues[idx];

                        yield return KeyValuePair.Create(name, value);
                    }
                }
            }
        }

        private RWRailDriver railDriver;
        private int Period;
        private Thread? thread;
        private MonitorState state = new MonitorState();

        private PropertiesChangeEventArgs _propertiesChangeEventArgsCache;

        public class PropertiesChangeEventArgs
        {
            public required MonitorState MonitorState;
        };
        public delegate void OnPropertiesChangeEvent(PropertiesChangeEventArgs args);

        public event OnPropertiesChangeEvent? OnPropertiesChange;

        public RWRailDriverMonitor(RWRailDriver railDriver, int updateMilliseconds)
        {
            this.railDriver = railDriver;
            Period = updateMilliseconds;
            _propertiesChangeEventArgsCache = new PropertiesChangeEventArgs { MonitorState = state };
        }

        public bool IsStarted { get; set; }

        public void Start()
        {
            IsStarted = true;
            thread = new Thread(Update);
            thread.Start();
        }

        public void Stop()
        {
            IsStarted = false;
        }

        void Update()
        {
            var monitor = this;
            while (monitor.IsStarted)
            {
                if (monitor.OnPropertiesChange != null)
                {
                    var railDriver = monitor.railDriver;
                    railDriver.EnsureDllIsLoaded();
                    var dll = railDriver.RailDriverDLL!;
                    var isConnected = dll.GetRailSimConnected();
                    dll.SetRailDriverConnected(true);
                    
                    if (isConnected)
                    {
                        var monitorState = monitor.state;
                        var list = dll.GetControllerList();
                        var tmp = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(list)!;
                        monitorState.controllerList = tmp.Split("::");
                        var oldControlValuesCount = monitorState.controlValues.Count;
                        monitorState.changedControlIdx.Clear();

                        for (int i = 0; i < monitorState.controllerList.Length; i++)
                        {
                            var name = monitorState.controllerList[i];
                            if (monitorState.printAllControlValues == false && monitorState.controlValuesToPrint.Contains(name) == false)
                            {
                                if (i < monitorState.controlValues.Count)
                                {
                                    monitorState.controlValues[i] = 0;
                                }
                                else
                                {
                                    monitorState.controlValues.Add(0);
                                }
                                continue;
                            }

                            var newValue = dll.GetControllerValue(i, 0);
                            var containsOld = i < monitorState.controlValues.Count;
                            if (containsOld)
                            {
                                var oldValue = monitorState.controlValues[i];
                                if (oldValue != newValue)
                                {
                                    monitorState.changedControlIdx.Add(KeyValuePair.Create(i, oldValue));
                                }
                                monitorState.controlValues[i] = newValue;
                            } else
                            {
                                monitorState.controlValues.Add(newValue);
                            }
                        }
                        if (monitorState.controlValues.Count >= monitorState.controllerList.Length)
                        {
                            var count = monitorState.controllerList.Length - monitorState.controlValues.Count;
                            monitorState.controlValues.RemoveRange(monitorState.controlValues.Count, count);
                        } 
                        monitorState.controlValues.TrimExcess();
                        monitor.OnPropertiesChange?.Invoke(monitor._propertiesChangeEventArgsCache);
                    }
                };

                Thread.Sleep(monitor.Period);
            }
        }
    }
}
