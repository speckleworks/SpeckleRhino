![alt text](https://user-images.githubusercontent.com/1014562/40281319-9c1fa318-5c60-11e8-9208-92fbc126095d.png "SpeckleRhino")

# SpeckleRhino
This repository contains various projects for building Speckle Clients for Rhino and Grasshopper.\
While [SpeckleCore](https://github.com/speckleworks/SpeckleCore) (the .net speckle sdk) is now coming with nuget, [SpeckleView](https://github.com/speckleworks/SpeckleView) (the frontend ui) needs to be included as submodules.


[![Build status](https://ci.appveyor.com/api/projects/status/mtfs3owdpy72yuh3/branch/master?svg=true)](https://ci.appveyor.com/project/SpeckleWorks/specklerhino/branch/master)

## Download

> The [latest build is here](https://github.com/speckleworks/SpeckleInstaller/releases/latest). Be sure to check out the [getting started guide](https://speckle.works/log/speckle-guide/) to get up to speed with some basics.


## Building Speckle for Rhino

Speckle for Rhino is developed with Visual Studio 2017.

In order to debug/build Speckle for Rhino:

1. Fork and/or clone this repository.
1. Navigate in the local folder and run `git submodule update --init` to bring in the SpeckleView submodule
1. Navigate to the SpeckleView directory and run `npm install` from a console. This will install all dependencies for the SpeckleView project.
1. Run `npm run build` in the SpeckleView folder to generate the `dist.js` file.
1. Run `npm run dev` to build the UI and start a local server. Test it: in a browser, navigate to `http://localhost:9090`. You should see the SpeckleView UI. 
1. Open SpeckleRhino.sln from the repository root.
1. Build the solution, it will:
    * get the right version of nuget references (RhinoCommon, [SpeckleCore](https://github.com/speckleworks/SpeckleCore)..)
    * create a `~/SpekleRhino/Debug` folder containing the plugin to install
1. Start debugging either SpeckleRhino or SpeckleGrasshopper

    *Rhino Plugin*
    1. Drag&Drop `~/SpekleRhino/Debug/SpeckleWinR6.rhp` into Rhino canvas.
    1. Test the installation typing command `SpeklePanel` > `Show`

    *Grasshopper plugin*
    1. Open Grasshopper
    1. Drag&Drop `~/SpekleRhino/Debug/SpeckleGrasshopper.gha` into Grasshopper canvas.

## Common Issues
1. If during the installation of the `rhp` file you get a "SpeckleKits" missing folder (see #119). Please manually create it in `%localappdata%/SpeckleKits`.
1. By default, Speckle does not come with an object model. To actually be able to convert to and from Rhino when building/debugging, please clone and build the [SpeckleCoreGeometry](https://github.com/speckleworks/SpeckleCoreGeometry) repository and copy all `.dll`s inside `%localappdata%/SpeckleKits`

## License 
MIT 
