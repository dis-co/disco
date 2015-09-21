namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Iris.Web")>]
[<assembly: AssemblyProductAttribute("Iris")>]
[<assembly: AssemblyDescriptionAttribute("VVVV Automation Infrastructure")>]
[<assembly: AssemblyVersionAttribute("1.0")>]
[<assembly: AssemblyFileVersionAttribute("1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0"
