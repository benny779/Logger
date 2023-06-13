using System;
using System.IO;

namespace Logging
{
    internal static class General
    {
        internal static readonly string AppName = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);
        internal static readonly string MachineName = Environment.MachineName;
        internal static readonly string UserName = Environment.UserName;
    }
}
