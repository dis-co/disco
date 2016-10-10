with import <nixpkgs> {};

{
  fsiEnv =  stdenv.mkDerivation {
    name = "testEnv";
    buildInputs = [ stdenv curl openssl ];
    libpath="${curl.out}/lib:${openssl.out}/lib";
    shellHook = ''
      export IRIS_NODE_ID=`uuidgen`
      export LD_LIBRARY_PATH="$libpath":$LD_LIBRARY_PATH
      mono Iris.Tests.exe
      exit $?
    '';
  };
}
