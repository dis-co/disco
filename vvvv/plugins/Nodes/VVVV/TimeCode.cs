using System;
using System.Threading;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.ComponentModel.Composition;
using System.Security.Principal;
using System.Security.AccessControl;

using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes
{
    #region PluginInfo
    [PluginInfo(Name = "ShareTimeCode", Category = "Iris", Help = "Share TimeCode via a shared memory segment", Tags = "", AutoEvaluate = true)]
    #endregion PluginInfo

    public class ShareTimeCode : IPluginEvaluate, IDisposable
    {
        [Import()]
        public IHDEHost V2Host;

        [Import()]
        public ILogger Logger;

        [Input("TimeCode", IsSingle = true, DefaultValue = 0)]
        ISpread<ulong> InTimeCode;

        private MemoryMappedFile File;
        private MemoryMappedViewAccessor Accessor;

        public ShareTimeCode()
        {
            try {
                File = MemoryMappedFile.CreateOrOpen(
                    @"IrisTC", sizeof(ulong), MemoryMappedFileAccess.ReadWrite);

                Accessor = File.CreateViewAccessor(
                    0, sizeof(ulong), MemoryMappedFileAccess.ReadWrite);
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Debug, ex.Message);
                Logger.Log(LogType.Debug, ex.StackTrace);
            }
        }

        public void Dispose()
        {
            Accessor.Dispose();
            File.Dispose();
        }
            
        public void Evaluate(int _)
        {
            Accessor.Write(0, InTimeCode[0]);
        }
    }
}