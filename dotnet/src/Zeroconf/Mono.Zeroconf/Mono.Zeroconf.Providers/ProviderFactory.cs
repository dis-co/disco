//
// ProviderFactory.cs
//
// Authors:
//    Aaron Bockover  <abockover@novell.com>
//
// Copyright (C) 2006-2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace Mono.Zeroconf.Providers
{
    internal static class ProviderFactory
    {
        private static IZeroconfProvider [] providers;
        private static IZeroconfProvider selected_provider;

        private static IZeroconfProvider DefaultProvider {
            get {
                if(providers == null) {
                    GetProviders();
                }

                return providers[0];
            }
        }

        public static IZeroconfProvider SelectedProvider {
            get { return selected_provider == null ? DefaultProvider : selected_provider; }
            set { selected_provider = value; }
        }

        public static void LoadProvider(List<IZeroconfProvider> list, string path)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string basePath = asm.Location;
            string absPath = Path.Combine(Path.GetDirectoryName(basePath), path);
            Assembly provider_asm = Assembly.LoadFile(absPath);
            Console.Write("Loading {0} zeroconf provider: ", path);
            foreach(Attribute attr in provider_asm.GetCustomAttributes(false)) {
                if(attr is ZeroconfProviderAttribute) {
                    Type type = (attr as ZeroconfProviderAttribute).ProviderType;
                    IZeroconfProvider provider = (IZeroconfProvider)Activator.CreateInstance(type);
                    try {
                        provider.Initialize();
                        list.Add(provider);
                        Console.WriteLine ("OK");
                    } catch (Exception e) {
                        Console.WriteLine ("FAILED ({0}: {1})", e.GetType().FullName, e.Message);
                    }
                }
            }
        }

        private static IZeroconfProvider [] GetProviders()
        {
            if(providers != null) {
                return providers;
            }

            List<IZeroconfProvider> providers_list = new List<IZeroconfProvider>();

            LoadProvider(providers_list, "Mono.Zeroconf.Providers.Bonjour.dll");
            LoadProvider(providers_list, "Mono.Zeroconf.Providers.AvahiDBus.dll");

            providers = providers_list.ToArray();

            return providers;
        }
    }
}
