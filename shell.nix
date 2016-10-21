with import <nixpkgs> {};

{
  irisEnv =  stdenv.mkDerivation {
    name = "irisEnv";
    buildInputs = [ stdenv curl openssl phantomjs2 ];
    libpath="${curl.out}/lib:${openssl.out}/lib:";
    shellHook = ''
      export LD_LIBRARY_PATH="$libpath":$LD_LIBRARY_PATH
      export IRIS_NODE_ID=`uuidgen`
      export PHANTOMJS_PATH=${phantomjs2}/bin/phantomjs
    '';
  };
}
