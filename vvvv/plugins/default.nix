with import <nixpkgs> {}; {
  IrisServiceEnv =  stdenv.mkDerivation {
    name = "IrisServiceEnv";
    buildInputs = [ stdenv fsharp curl openssl ];
    LD_LIBRARY_PATH="${curl}/lib:${openssl}/lib";
    run = "mono src/Iris.Service/bin/Debug/Iris.Service.exe";
    shellHook = ''
      if [ ! -L NativeBinaries ]; then
        ln -s src/Iris.Service/bin/Debug/NativeBinaries .
      fi
      $run
    '';
  };
}
