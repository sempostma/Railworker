using System.Reflection;
using System.Runtime.InteropServices;

namespace RWLib
{
    public class RWRailDriverDLL
    {
        class FunctionLoader
        {
            [DllImport("Kernel32.dll")]
            private static extern IntPtr LoadLibrary(string path);

            [DllImport("Kernel32.dll")]
            private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

            public static Delegate LoadFunction<T>(string dllPath, string functionName)
            {
                var hModule = LoadLibrary(dllPath);
                var functionAddress = GetProcAddress(hModule, functionName);
                return Marshal.GetDelegateForFunctionPointer(functionAddress, typeof(T));
            }
        }

        private string dllLocation;

        public RWRailDriverDLL(RWRailDriver parent)
        {
            dllLocation = Path.Join(parent.rWLibrary.TSPath, "plugins", "RailDriver64.dll");

            SetRailDriverConnected = (SetRailDriverConnectedDelegate)FunctionLoader
                .LoadFunction<SetRailDriverConnectedDelegate>(dllLocation, "SetRailDriverConnected");

            GetRailSimConnected = (GetRailSimConnectedDelegate)FunctionLoader
                .LoadFunction<GetRailSimConnectedDelegate>(dllLocation, "GetRailSimConnected");

            SetRailSimValue = (SetRailSimValueDelegate)FunctionLoader
                .LoadFunction<SetRailSimValueDelegate>(dllLocation, "SetRailSimValue");

            GetRailSimValue = (GetRailSimValueDelegate)FunctionLoader
                .LoadFunction<GetRailSimValueDelegate>(dllLocation, "GetRailSimValue");

            GetRailSimLocoChanged = (GetRailSimLocoChangedDelegate)FunctionLoader
                .LoadFunction<GetRailSimLocoChangedDelegate>(dllLocation, "GetRailSimLocoChanged");

            GetRailSimCombinedThrottleBrake = (GetRailSimCombinedThrottleBrakeDelegate)FunctionLoader
                .LoadFunction<GetRailSimCombinedThrottleBrakeDelegate>(dllLocation, "GetRailSimCombinedThrottleBrake");

            GetLocoName = (GetLocoNameDelegate)FunctionLoader
                .LoadFunction<GetLocoNameDelegate>(dllLocation, "GetLocoName");

            GetControllerList = (GetControllerListDelegate)FunctionLoader
                .LoadFunction<GetControllerListDelegate>(dllLocation, "GetControllerList");

            GetControllerValue = (GetControllerValueDelegate)FunctionLoader
                .LoadFunction<GetControllerValueDelegate>(dllLocation, "GetControllerValue");

            SetControllerValue = (SetControllerValueDelegate)FunctionLoader
                .LoadFunction<SetControllerValueDelegate>(dllLocation, "SetControllerValue");
        }

        public delegate int SetRailDriverConnectedDelegate(bool isConnected);
        public SetRailDriverConnectedDelegate SetRailDriverConnected;

        public delegate Boolean GetRailSimConnectedDelegate();
        public GetRailSimConnectedDelegate GetRailSimConnected;

        public delegate void SetRailSimValueDelegate(int controlID, float value);
        public SetRailSimValueDelegate SetRailSimValue;

        public delegate float GetRailSimValueDelegate(int controlID, int type);
        public GetRailSimValueDelegate GetRailSimValue;

        public delegate Boolean GetRailSimLocoChangedDelegate();
        public GetRailSimLocoChangedDelegate GetRailSimLocoChanged;

        public delegate Boolean GetRailSimCombinedThrottleBrakeDelegate();
        public GetRailSimCombinedThrottleBrakeDelegate GetRailSimCombinedThrottleBrake;

        public delegate IntPtr GetLocoNameDelegate();
        public GetLocoNameDelegate GetLocoName;

        public delegate IntPtr GetControllerListDelegate();
        public GetControllerListDelegate GetControllerList;

        public delegate float GetControllerValueDelegate(int controlID, int type);
        public GetControllerValueDelegate GetControllerValue;

        public delegate float SetControllerValueDelegate(int controlID, float value);
        public SetControllerValueDelegate SetControllerValue;
    }
}