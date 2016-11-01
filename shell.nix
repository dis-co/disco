{ pkgs ? import <nixpkgs> {} }:

with pkgs;
let
  mono = mono46;

  inputs = [ stdenv curl openssl phantomjs2 ];

  libpath="${curl.out}/lib:${openssl.out}/lib:";

  hook = ''
    export LD_LIBRARY_PATH="$libpath":$LD_LIBRARY_PATH
    export IRIS_NODE_ID=`uuidgen`
    export PHANTOMJS_PATH=${phantomjs2}/bin/phantomjs
  '';

  service = stdenv.mkDerivation {
    name = "Iris";
    version = "1.0";
    phases = [ "unpackPhase" "buildPhase" "installPhase" ];

    buildInputs = [ fsharp ];

    src = ./dotnet;

    unpackPhase = ''
      cp -r $src .
      chmod -R +rwx .
      cd $src
      substituteInPlace ./build.sh \
	--replace /usr/bin/env bash ${stdenv.shell}
    '';

    buildPhase = ''
      ./build.sh DebugDocker
    '';

    installPhase = ''
      mkdir -p $out/bin
      cp src/Iris/bin/Debug/Iris/* $out/bin
    '';
  };

  image = mono: dockerTools.buildImage {
    name = "iris-image";
    tag = "0.1";

    runAsRoot = ''
      #!${stdenv.shell}
      export PATH=/bin:/usr/bin:/sbin:/usr/sbin:$PATH
      ${dockerTools.shadowSetup}
      mkdir /data
    '';

    contents = [ mono myApp ];

    config = {
      Cmd = [ "${mono}/bin/mono" "${myApp}/bin/Main.exe"];
      ExposedPorts = {
	"6379/tcp" = {};
      };
      WorkingDir = "/data";
      Volumes = {
	"/data" = {};
      };
    };
  };

  env = stdenv.mkDerivation {
    name = "irisEnv";
    buildInputs = inputs;
    libpath = libpath;
    shellHook = hook;
  };

in rec {
  irisService = service;
  dockerImage = image;
  irisEnv = env;
}
