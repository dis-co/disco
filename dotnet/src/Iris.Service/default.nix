with import <nixpkgs> {}; {
  fsiEnv =  stdenv.mkDerivation {
    name = "fsiEnv";
    buildInputs = [ stdenv fsharp curl openssl ];
    LD_LIBRARY_PATH="${curl}/lib:${openssl}/lib";
    shellHook = ''
      fsi run.fsx
    '';
  };
}
