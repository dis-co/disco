with import <nixpkgs> {}; {
  fsiEnv =  stdenv.mkDerivation {
    name = "testEnv";
    buildInputs = [ stdenv curl openssl ];
    LD_LIBRARY_PATH="${curl}/lib:${openssl}/lib";
    shellHook = ''
      mono Iris.Tests.exe
      exit
    '';
  };
}
