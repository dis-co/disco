namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Pallet")>]
[<assembly: AssemblyProductAttribute("Pallet")>]
[<assembly: AssemblyDescriptionAttribute("An implementation of the Raft concensus algorithm")>]
[<assembly: AssemblyVersionAttribute("1.0")>]
[<assembly: AssemblyFileVersionAttribute("1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0"
