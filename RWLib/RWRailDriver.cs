using System.Xml.Linq;

namespace RWLib
{
    public class RWRailDriver
    {
        internal RWLibrary rWLibrary;
        public RWRailDriverDLL? RailDriverDLL;
        private bool dllLoadAttemptStarted = false;
        private object dllLoadAttemptStartedLock = new object();

        public bool DLLIsLoading
        {
            get
            {
                lock (dllLoadAttemptStartedLock)
                {
                    return dllLoadAttemptStarted && RailDriverDLL == null;
                }
            }
        }

        public RWRailDriver(RWLibrary rWLibrary)
        {
            this.rWLibrary = rWLibrary;
        }

        public Task ImportDLL()
        {
            lock(dllLoadAttemptStartedLock)
            {
                if (dllLoadAttemptStarted == true) return Task.CompletedTask;
                dllLoadAttemptStarted = true;
            }
            return new Task(() => RailDriverDLL = new RWRailDriverDLL(this));
        }

        public void EnsureDllIsLoaded()
        {
            if (RailDriverDLL == null)
            {
                bool _loadDll = false;
                lock (dllLoadAttemptStartedLock)
                {
                    if (dllLoadAttemptStarted == false)
                    {
                        dllLoadAttemptStarted = true;
                        _loadDll = true;
                    }
                }
                if (_loadDll)
                {
                    RailDriverDLL = new RWRailDriverDLL(this);
                }
                else if (dllLoadAttemptStarted)
                {
                    // give the dll 1 second to load if it was already
                    for (int i = 0; i < 1000 && RailDriverDLL == null; i++)
                    {
                        Thread.Sleep(1);
                    }
                }
                // check again
                if (RailDriverDLL == null)
                {
                    throw new Exceptions.RailDriverDLLHasNotBeenLoadedYetException();
                }
            }
        }

        public Task SetRailDriverConnected(bool isConnected)
        {
            return new Task(() => {
                EnsureDllIsLoaded();
                RailDriverDLL!.SetRailDriverConnected(isConnected);
            });
        }

        public Task<bool> GetRailSimConnected()
        {
            return new Task<bool>(() => {
                EnsureDllIsLoaded();
                return RailDriverDLL!.GetRailSimConnected();
            });
        }

        public Task SetRailSimValue(int controlID, float value)
        {
            return new Task(() =>
            {
                EnsureDllIsLoaded();
                RailDriverDLL!.SetRailSimValue(controlID, value);
            });
        }

        public Task<float> GetRailSimValue(int controlID, int type)
        {
            return new Task<float>(() => {
                EnsureDllIsLoaded();
                return RailDriverDLL!.GetRailSimValue(controlID, type);
            });
        }

        public Task<bool> GetRailSimLocoChanged()
        {
            return new Task<bool>(() => {
                EnsureDllIsLoaded();
                return RailDriverDLL!.GetRailSimLocoChanged();
            });
        }

        public Task<bool> GetRailSimCombinedThrottleBrake()
        {
            return new Task<bool>(() => {
                EnsureDllIsLoaded();
                return RailDriverDLL!.GetRailSimCombinedThrottleBrake();
            });
        } 

        public Task<string> GetLocoName()
        {
            return new Task<string>(() => {
                EnsureDllIsLoaded();
                var name = RailDriverDLL!.GetLocoName();
                return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(name)!;
            });
        }

        public Task<string> GetControllerList()
        {
            return new Task<string>(() => {
                EnsureDllIsLoaded();
                var list = RailDriverDLL!.GetLocoName();
                return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(list)!;
            });
        }

        public Task<float> GetControllerValue(int controlID, int type)
        {
            return new Task<float>(() => {
                EnsureDllIsLoaded();
                return RailDriverDLL!.GetControllerValue(controlID, type);
            });
        }

        public Task SetControllerValue(int controlID, float value)
        {
            return new Task(() => {
                EnsureDllIsLoaded();
                RailDriverDLL!.SetControllerValue(controlID, value);
            });
        }
    }
}