amespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Worker")>]
[<assembly: AssemblyProductAttribute("Disco")>]
[<assembly: AssemblyDescriptionAttribute("VVVV Automation Infrastructure")>]
[<assembly: AssemblyVersionAttribute("0.3.1")>]
[<assembly: AssemblyFileVersionAttribute("0.3.1")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.3.1"
    let [<Literal>] InformationalVersion = "0.3.1"
