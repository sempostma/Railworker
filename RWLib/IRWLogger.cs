namespace RWLib
{
    public interface IRWLogger
    {
        void Log(RWLogType type, string message);
    }

    public enum RWLogType { Verbose, Info, Warning, Error, Debug }
}