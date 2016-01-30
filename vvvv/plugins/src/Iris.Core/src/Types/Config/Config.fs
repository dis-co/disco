namespace Iris.Core.Types.Config

[<AutoOpen>]
module Config =

  type Config =
    {  Audio     : AudioConfig
    ;  Vvvv      : VvvvConfig
    ;  Engine    : VsyncConfig
    ;  Timing    : TimingConfig
    ;  Port      : PortConfig
    ;  ViewPorts : ViewPort list
    ;  Displays  : Display  list
    ;  Tasks     : Task     list
    ;  Cluster   : Cluster
    }
