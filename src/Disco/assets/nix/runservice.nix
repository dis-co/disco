with import <nixpkgs> {};

{
  fsiEnv =  stdenv.mkDerivation {
    name = "fsiEnv";
    buildInputs = [ stdenv curl zlib openssl ];
    libpath="${curl.out}/lib:${openssl.out}/lib:${zlib.out}/lib";
    shellHook = ''
      export LD_LIBRARY_PATH="$libpath":$LD_LIBRARY_PATH
      export DISCO_NODE_ID=`uuidgen`
    '';
  };
}
