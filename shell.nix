with import <nixpkgs> {};

let
  inputs = [ stdenv curl openssl phantomjs2 ];

  libpath="${curl.out}/lib:${openssl.out}/lib:";

  hook = ''
    export LD_LIBRARY_PATH="$libpath":$LD_LIBRARY_PATH
    export IRIS_NODE_ID=`uuidgen`
    export PHANTOMJS_PATH=${phantomjs2}/bin/phantomjs
  '';

in rec {
  irisEnv =  stdenv.mkDerivation {
    name = "irisEnv";
    buildInputs = inputs;
    libpath = libpath;
    shellHook = hook;
  };
}
