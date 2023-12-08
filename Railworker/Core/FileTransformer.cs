using RWLib;
using System;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;

namespace Railworker.Core
{
    public class FileTransformer
    {
        private Func<RWLibrary, string, Task<string>> transformFunction;

        public FileTransformer(Func<RWLibrary, string, Task<string>> transformFunction)
        {
            this.transformFunction = transformFunction;
        }

        public Task<string> Transform(string filename, RWLibrary library)
        {
            return transformFunction(library, filename);
        }
    }
}