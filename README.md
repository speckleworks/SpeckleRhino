# SpeckleRhino
This repository contains various projects for building Speckle Clients for Rhino and Grasshopper.
[SpeckleCore](https://github.com/speckleworks/SpeckleCore) (the .net speckle sdk) and [SpeckleView](https://github.com/speckleworks/SpeckleView) (the frontend ui) are included as submodules.

[![Build status](https://ci.appveyor.com/api/projects/status/mtfs3owdpy72yuh3/branch/master?svg=true)](https://ci.appveyor.com/project/SpeckleWorks/specklerhino/branch/master)

## Usage

#### Download the [latest build](https://ci.appveyor.com/api/projects/SpeckleWorks/SpeckleRhino/artifacts/specklerhino.rhi?branch=master&job=Configuration%3DRelease) from the `master` branch.

⚠️ Please make  sure you read the [notes on migration](https://speckle.works/log/versionone/#migration-testing) and the (older) [release notes](https://speckle.works/log/specklerhinoplugin/). At the moment, to register a new account, use `https://hestia.speckle.works/api/v1` as your server url, unless you've deployed your own speckle server. 

## Building Speckle for Rhino

Speckle for Rhino is developed with Visual Studio 2017.

In order to build Speckle for Rhino:

1. Clone this repository
2. run `git submodule update --init` to bring in the SpeckleCore submodule
3. Navigate to the SpeckleView directory and run `npm install` from a console. This will install all dependencies for the SpeckleView project.
4. Run `npm run dev` to build the UI and start a local server.
In a browser, navigate to `http://localhost:9090`. You should see the SpeckleView UI
5. Open SpeckleRhino.sln from the repository root.
6. Build the solution

The complete build output will be found in the Solution Directory under the `Debug` or `Release` directory, depending on the configuration you are building.
