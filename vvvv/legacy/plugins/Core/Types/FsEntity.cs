using System;
using System.IO;
using System.Collections.Generic;

using Iris.Core.Logging;

namespace Iris.Core.Types
{
    public abstract class FsEntity
    {
        private FsEntity()
        {
        }

        public sealed class File : FsEntity
        {
            public String Path { get; private set; }

            public String Parent { get; private set; }

            public String Name { get; private set; }

            public String Extension { get; private set; }

            public String Owner { get; private set; }

            public long Size    { get; private set; }

            public Boolean ReadOnly { get; private set; }

            public DateTime CreatedAt { get; private set; }

            public DateTime LastAccessAt { get; private set; }

            public DateTime LastWriteAt { get; private set; }

            public File(string path)
            {
                try
                {
                    FileInfo f = new FileInfo(path);
                    if (f.Exists)
                    {
                        Path = f.FullName;
                        Parent = f.DirectoryName;
                        Name = f.Name;
                        Extension = f.Extension;
                        Size = f.Length;
                        ReadOnly = f.IsReadOnly;

                        if (Environment.OSVersion.Platform.ToString().Equals("Unix"))
                        {
                            Owner = "Bob";
                        }
                        else
                        {
                            Owner = f.GetAccessControl().GetOwner(typeof(System.Security.Principal.NTAccount)).ToString();
                        }

                        CreatedAt = f.CreationTimeUtc; 
                        LastAccessAt = f.LastAccessTimeUtc;
                        LastWriteAt = f.LastWriteTimeUtc;
                    }
                }
                catch (Exception ex)
                {
                    LogEntry.Fatal("File", "Exception: " + ex.Message);
                }
            }
        }

        public sealed class UnauthorizedFile : FsEntity
        {
            public String Path { get; private set; }

            public UnauthorizedFile(string path)
            {
                Path = path;
            }
        }

        public sealed class Dir : FsEntity
        {
            public String Parent { get; private set; }

            public String Root { get; private set; }

            public String Path { get; private set; }

            public String Name { get; private set; }

            public String Owner { get; private set; }

            public DateTime CreatedAt { get; private set; }

            public DateTime LastAccessAt { get; private set; }

            public DateTime LastWriteAt { get; private set; }

            public List<String> Children { get; private set; }

            public Dir(string path)
            {
                try
                {
                    DirectoryInfo d = new DirectoryInfo(path);
                    if (d != null && d.Exists)
                    {
                        if (d.Parent != null)
                            Parent = d.Parent.FullName;

                        if (d.Root != null)
                            Root = d.Root.FullName;

                        Path = d.FullName;
                        Name = d.Name;

                        if (Environment.OSVersion.Platform.ToString().Equals("Unix"))
                        {
                            Owner = "Alice";
                        }
                        else
                        {
                            Owner = d.GetAccessControl().GetOwner(typeof(System.Security.Principal.NTAccount)).ToString();
                        }

                        CreatedAt = d.CreationTimeUtc;
                        LastAccessAt = d.LastAccessTimeUtc;
                        LastWriteAt = d.LastWriteTimeUtc;
                        Children = new List<String>();

                        var entries = d.EnumerateFileSystemInfos();
                        foreach (var entry in  entries)
                        {
                            Children.Add(entry.FullName);
                        }
                    }
                }
                catch(Exception ex)
                {
                    LogEntry.Fatal("Directory", "Exception: " + ex.Message);
                }
            }
        }
    }
}

