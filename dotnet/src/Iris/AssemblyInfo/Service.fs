namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Iris.Service")>]
[<assembly: AssemblyProductAttribute("Iris")>]
[<assembly: AssemblyDescriptionAttribute("VVVV Automation Infrastructure")>]
[<assembly: AssemblyVersionAttribute("0.3.1")>]
[<assembly: AssemblyFileVersionAttribute("0.3.1")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.3.1"
