using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Iris.Core.Types;
using Iris.Core.Logging;
using Iris.FrontEnd.Types;
using Iris.FrontEnd.Api.Interfaces;

using Newtonsoft.Json;
using RabbitMQ.Client.Events;

namespace Iris.FrontEnd.Api.Impl
{
    public class FileSystemService : IrisService, IFileSystemService
    {
        private const string TAG = "FileSystemService";

        public FileSystemService() : base(DataUri.FsBase)
        {
        }

        protected override void BeforeStart()
        {
        }

        protected override void OnMessage(BasicDeliverEventArgs ea)
        {
        }

        public List<Drive> GetDrives(string _sid)
        {
            var drives =  Environment.GetLogicalDrives()
                .ToList()
                .ConvertAll(new Converter<string, Drive>(letter => {
                            return new Drive(letter);
                        }));

            LogEntry.Fatal(TAG, "Drives: " + drives.Aggregate(String.Empty, (acc, drive) => { acc += (drive.Letter + ", "); return acc; }));
            return drives;
        }

        public FsEntity GetEntity(string _sid, string path)
        {
            try
            {
                FileAttributes attr = File.GetAttributes(path);

                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    return new FsEntity.Dir(path);
                }
                else
                {
                    return new FsEntity.File(path);
                }
            }
            catch(UnauthorizedAccessException ex)
            {
                Console.WriteLine("UnauthorizedException " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                return new FsEntity.UnauthorizedFile(path);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw IrisException.ObjectNotFound();
            }
        }
    }
}
