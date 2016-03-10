with import <nixpkgs> {}; {
  fsiEnv =  stdenv.mkDerivation {
    name = "testEnv";
    buildInputs = [ stdenv curl openssl ];
    LD_LIBRARY_PATH="${curl}/lib:${openssl}/lib";
    shellHook = ''
      if [ ! -L $PWD/bin/Debug/NUnit.Framework.dll ]; then
        ln -s $PWD/bin/Debug/nunit.framework.dll $PWD/bin/Debug/NUnit.Framework.dll
      fi
      if [ ! -L $PWD/bin/Debug/Xunit.dll ]; then
        ln -s $PWD/bin/Debug/xunit.core.dll $PWD/bin/Debug/Xunit.dll
      fi
      if [ ! -L NativeBinaries ]; then
        ln -s $PWD/bin/Debug/NativeBinaries .
      fi
      fsi run.fsx
      exit
    '';
  };
}
