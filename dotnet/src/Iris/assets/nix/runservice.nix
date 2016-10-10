with import <nixpkgs> {};

{
  fsiEnv =  stdenv.mkDerivation {
    name = "fsiEnv";
    buildInputs = [ stdenv curl openssl ];
    libpath="${curl.out}/lib:${openssl.out}/lib:";
    shellHook = ''
      export LD_LIBRARY_PATH="$libpath":$LD_LIBRARY_PATH
      export IRIS_NODE_ID=`uuidgen`
    '';
  };
}
