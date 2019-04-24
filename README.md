![alt text](https://user-images.githubusercontent.com/1014562/40281319-9c1fa318-5c60-11e8-9208-92fbc126095d.png "SpeckleRhino")

# SpeckleRhino
This repository contains various projects for building Speckle Clients for Rhino and Grasshopper.
[SpeckleCore](https://github.com/speckleworks/SpeckleCore) (the .net speckle sdk) and [SpeckleView](https://github.com/speckleworks/SpeckleView) (the frontend ui) are included as submodules.

[![Build status](https://ci.appveyor.com/api/projects/status/mtfs3owdpy72yuh3/branch/master?svg=true)](https://ci.appveyor.com/project/SpeckleWorks/specklerhino/branch/master)

## Download

> The [latest build is here](https://github.com/speckleworks/SpeckleInstaller/releases/latest). Be sure to check out the [getting started guide](https://speckle.works/log/speckle-guide/) to get up to speed with some basics.


## Building Speckle for Rhino

Speckle for Rhino is developed with Visual Studio 2017.

In order to debug/build Speckle for Rhino:

1. Clone this repository
2. run `git submodule update --init` to bring in the SpeckleCore submodule
3. Navigate to the SpeckleView directory and run `npm install` from a console. This will install all dependencies for the SpeckleView project.
4. Run `npm run dev` to build the UI and start a local server. Test it: in a browser, navigate to `http://localhost:9090`. You should see the SpeckleView UI. 
5. Open SpeckleRhino.sln from the repository root.
6. Restore nuget packages.
7. Start debugging either SpeckleRhino or SpeckleGrasshopper. To build the solution, make sure you run first `npm run build` in the SpeckleView folder to generate the `dist.js` file.

Note: by default, Speckle does not come with an object model. To actually be able to convert to and from Rhino when building/debugging, please clone and build the [SpeckleCoreGeometry](https://github.com/speckleworks/SpeckleCoreGeometry) repository.

## License 
MIT 
