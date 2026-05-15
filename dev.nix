# To learn more about how to use Nix to configure your environment
# see: https://firebase.google.com/docs/studio/customize-workspace

{ pkgs, ... }: {

  channel = "stable-24.05";

  packages = [
    pkgs.dotnet-sdk_8
  ];

  env = {};

  idx = {

    extensions = [
      "ms-dotnettools.csdevkit"
    ];

    previews = {
      enable = true;

      previews = {
        web = {
          command = [
            "dotnet"
            "run"
            "--project"
            "./OctafxIndia/OctafxIndia.csproj"
            "--urls"
            "http://0.0.0.0:$PORT"
          ];

          manager = "web";
        };
      };
    };

    workspace = {

      onCreate = {
        restore = "dotnet restore ./OctafxIndia/OctafxIndia.csproj";
      };

      onStart = {
        build = "dotnet build ./OctafxIndia/OctafxIndia.csproj";
      };
    };
  };
}