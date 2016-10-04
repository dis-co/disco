with import <nixpkgs> {};

let
  zeromq = callPackage /home/k/.config/nixos/derivations/zeromq.nix {};

in {
  fsiEnv =  stdenv.mkDerivation {
    name = "testEnv";
    buildInputs = [ stdenv curl openssl ];
    libpath="${curl}/lib:${openssl}/lib:${zeromq}/lib";
    shellHook = ''
      export LD_LIBRARY_PATH="$libpath":$LD_LIBRARY_PATH
      mono Iris.Tests.exe
      exit $?
    '';
  };
}
