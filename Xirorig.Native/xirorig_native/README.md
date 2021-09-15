# Xirorig.Native
***This is the native c library used by Xirorig. Without this library, mining speed will be slower.***

#### Table of contents
- [Compile Instructions](#Compile-instructions)
- [Donations](#Donations)
- [Developers](#Developers)

## Compile Instructions
Xirorig Native uses [CMake](https://cmake.org/download) and [vcpkg](https://vcpkg.io/en/index.html) to build.

### For Windows
#### Via Visual Studio
Minimum Requirements:
- [Visual Studio 2019](https://visualstudio.microsoft.com/downloads)
Install Desktop Development with C++ workload. <br />
Select C++ CMake tools for Windows. <br />
Select MSVC v14x - VS 2019 C++ x68/x86 build tools. <br />
Select MSVC v14x - VS 2019 C++ ARM build tools. (Optional - if targeting ARM devices) <br />
Select MSVC v14x - VS 2019 C++ ARM64 build tools. (Optional - if targeting ARM64 devices) <br />
Select Windows 10 SDK (10.0.1xxxx.0)
- [MSYS2](https://www.msys2.org/) (Optional - if you prefer GCC/Mingw toolchain)

1. Clone the git repository.
2. Open CMake Project in Visual Studio (File > Open > CMake)
3. Select CMakeLists.txt
4. Open CMakeSettings.json

If you prefer GCC/Mingw toolchain, the x64-windows and x86-windows configuration is already configure for it. <br />
You need to change the environment "MINGW64_ROOT" in CMakeSettings.json if your msys2 installation is not at "C:/msys64/mingw64".

For MSVC toolchain, add a new configuration and select x64-Release or x86-Release. <br />
Click "Edit Json" to add new variables required for the build.
Add the new variable and ensure that "VCPKG_TARGET_TRIPLET" matches the archtecture you have selected. (x64-windows-static or x86-windows-static)

```json
"variables": [
  {
    "name": "VCPKG_TARGET_TRIPLET",
    "value": "x64-windows-static",
    "type": "STRING"
  }
],
```

5. Select the new configuration on the configuration drop down and let it generate. You can manually generate via (Project > Configure xirorig_native)
6. Build the project. (Build > Build All)
7. Copy the output at `Xirorig/Xirorig.Native/xirorig_native/out/build/x64-windows/xirorig_native.dll` to your Xirorig installation and you are done.

For GCC/Mingw toolchain, the output file is called `libxirorig_native.dll`. Rename it to `xirorig_native.dll` so that Xirorig can recongnize the native library.

#### Via CMake GUI
Minimum Requirements:
- [CMake](https://cmake.org/download/)
- [Visual Studio 2019](https://visualstudio.microsoft.com/downloads)
Install Desktop Development with C++ workload. <br />
Select MSVC v14x - VS 2019 C++ x68/x86 build tools. <br />
Select Windows 10 SDK (10.0.1xxxx.0) <br />

1. Clone the git repository.
2. Open CMake GUI.
3. Select Source at `Xirorig/Xirorig.Native/xirorig_native`
4. Select Build at `Xirorig/Xirorig.Native/xirorig_native/build` (Create a folder if required)
5. Add a new Entry: `VCPKG_TARGET_TRIPLET` as `STRING` with `x64-windows-static` or `x86-windows-static`.
6. Click Configure. (Generator: Visual Studio 16 2019)
7. Wait and Click Generate.
8. Click Open Project.
9. Build the project. (Build > Build All)
10. Copy the output at `Xirorig/Xirorig.Native/xirorig_native/build/xirorig_native.dll` to your Xirorig installation and you are done.

The final build location might differ from my instructions, just find the `xirorig_native.dll` inside the build directory and you should be good.

#### For advanced users
This project can be build using any toolchain you prefer. GCC/Mingw toolchain is preferred for performance reason. <br />
You can also set `CMAKE_TOOLCHAIN_FILE` to empty if you prefer not to use vcpkg for dependency management.

CMake Variable required:
You need to set `VCPKG_TARGET_TRIPLET` with `x64-windows-static` or `x86-windows-static` or `x64-mingw-static` or `x86-mingw-static`.

## Donations
- BTC: `3Dc5jpiyuts136YhamcRbAeue7mi44gW8d`
- LTC: `LUU9Avuanafmq1vMp53AWS1mr3GCCc2X42`
- XMR: `42oj7eV68BK8Z8wcGzLMFEJgAQG22Z3ajGdtpmJx5p7iDqEgG91wNybWbwaVe4vUMveKAzAiA4j8xgUi29TpKXpm3zwfwWN`

## Developers
- The Dialga Team (Yong Jian Ming)
