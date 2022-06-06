# Xiro.Native
***This is the native c library used by Xiro. Without this library, mining speed will be slower.***

#### Table of contents
  - [Compile Instructions](#compile-instructions)
    - [For Windows](#for-windows)
      - [Via Command Line](#via-command-line)
      - [Via Visual Studio](#via-visual-studio)
    - [For Linux](#for-linux)
      - [Ubuntu 20.04 (LTS)](#ubuntu-2004-lts)
  - [Donations](#donations)
  - [Developers](#developers)

## Compile Instructions
Xiro Native uses [CMake](https://cmake.org/download) and [vcpkg](https://vcpkg.io/en/index.html) to build.

### For Windows
#### Via Command Line:
Minimum Requirements:
- [Git](https://git-scm.com/downloads)
- [Visual Studio 2019](https://visualstudio.microsoft.com/downloads)
  - Install Desktop Development with C++ workload
    - C++ CMake tools for Windows
    - MSVC v14x - VS 2019 C++ x68/x86 build tools
    - MSVC v14x - VS 2019 C++ ARM build tools
    - MSVC v14x - VS 2019 C++ ARM64 build tools
    - Windows 10 SDK (10.0.1xxxx.0)
- [MSYS2](https://www.msys2.org/)
  - Run MSYS2 MSYS and enter the following command in order:
    ```bash
    pacman -Syu
    pacman -Syu
    pacman -S --needed mingw-w64-x86_64-toolchain mingw-w64-x86_64-cmake mingw-w64-x86_64-ninja mingw-w64-i686-toolchain mingw-w64-i686-cmake mingw-w64-i686-ninja
    ```

```cmd
git clone --recursive https://github.com/TheDialgaTeam/Xirorig.git Xirorig

cd Xirorig

###################################################
# If you want to use GCC toolchain (only x64/x86) #
###################################################
SET CHERE_INVOKING=yes

# For x64
SET MSYSTEM=MINGW64

C:\msys64\usr\bin\bash -c "cmake -G Ninja -DCMAKE_BUILD_TYPE=Release -DVCPKG_TARGET_TRIPLET=x64-mingw-static -DCMAKE_INSTALL_PREFIX=""$PWD/Xirorig.Native/xirorig_native/out/install/windows-x64"" -S ""$PWD/Xirorig.Native/xirorig_native"" -B ""$PWD/Xirorig.Native/xirorig_native/out/build/windows-x64"" && ninja -C ""$PWD/Xirorig.Native/xirorig_native/out/build/windows-x64"" install"

# For x86
SET MSYSTEM=MINGW32

C:\msys64\usr\bin\bash -c "cmake -G Ninja -DCMAKE_BUILD_TYPE=Release -DVCPKG_TARGET_TRIPLET=x86-mingw-static -DCMAKE_INSTALL_PREFIX=""$PWD/Xirorig.Native/xirorig_native/out/install/windows-x86"" -S ""$PWD/Xirorig.Native/xirorig_native"" -B ""$PWD/Xirorig.Native/xirorig_native/out/build/windows-x86"" && ninja -C ""$PWD/Xirorig.Native/xirorig_native/out/build/windows-x86"" install"

# For GCC/Mingw toolchain, the output file is called libxirorig_native.dll. Rename it to xirorig_native.dll so that Xirorig can recongnize the native library.

#####################################
# If you want to use MSVC toolchain #
#####################################
# For x64
call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvars64.bat"

cmake -G Ninja -DCMAKE_BUILD_TYPE=Release -DVCPKG_TARGET_TRIPLET=x64-windows-static -DCMAKE_INSTALL_PREFIX="Xirorig.Native/xirorig_native/out/install/windows-x64" -S "Xirorig.Native/xirorig_native" -B "Xirorig.Native/xirorig_native/out/build/windows-x64"
ninja -C "Xirorig.Native/xirorig_native/out/build/windows-x64" install

# For x86
call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvars32.bat"

cmake -G Ninja -DCMAKE_BUILD_TYPE=Release -DVCPKG_TARGET_TRIPLET=x86-windows-static -DCMAKE_INSTALL_PREFIX="Xirorig.Native/xirorig_native/out/install/windows-x86" -S "Xirorig.Native/xirorig_native" -B "Xirorig.Native/xirorig_native/out/build/windows-x86"
ninja -C "Xirorig.Native/xirorig_native/out/build/windows-x86" install

# For ARM32
call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsamd64_arm.bat"

cmake -G Ninja -DCMAKE_BUILD_TYPE=Release -DVCPKG_TARGET_TRIPLET=arm-windows-static -DCMAKE_INSTALL_PREFIX="Xirorig.Native/xirorig_native/out/install/windows-ARM32" -S "Xirorig.Native/xirorig_native" -B "Xirorig.Native/xirorig_native/out/build/windows-ARM32"
ninja -C "Xirorig.Native/xirorig_native/out/build/windows-ARM32" install

# For ARM64
call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsamd64_arm64.bat"

cmake -G Ninja -DCMAKE_BUILD_TYPE=Release -DVCPKG_TARGET_TRIPLET=arm-windows-static -DCMAKE_INSTALL_PREFIX="Xirorig.Native/xirorig_native/out/install/windows-ARM64" -S "Xirorig.Native/xirorig_native" -B "Xirorig.Native/xirorig_native/out/build/windows-ARM64"
ninja -C "Xirorig.Native/xirorig_native/out/build/windows-ARM64" install
```

#### Via Visual Studio
Minimum Requirements:
- [Visual Studio 2019](https://visualstudio.microsoft.com/downloads)
  - Install Desktop Development with C++ workload
    - C++ CMake tools for Windows
    - MSVC v14x - VS 2019 C++ x68/x86 build tools
    - MSVC v14x - VS 2019 C++ ARM build tools
    - MSVC v14x - VS 2019 C++ ARM64 build tools
    - Windows 10 SDK (10.0.1xxxx.0)
- [MSYS2](https://www.msys2.org/)
  - Run MSYS2 MSYS and enter the following command in order:
    ```bash
    pacman -Syu
    pacman -Syu
    pacman -S --needed mingw-w64-x86_64-toolchain mingw-w64-x86_64-cmake mingw-w64-x86_64-ninja mingw-w64-i686-toolchain mingw-w64-i686-cmake mingw-w64-i686-ninja
    ```

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
7. Copy the output at `Xirorig.Native/xirorig_native/out/build/x64-windows/xirorig_native.dll` to your Xirorig installation and you are done.

For GCC/Mingw toolchain, the output file is called `libxirorig_native.dll`. Rename it to `xirorig_native.dll` so that Xirorig can recongnize the native library.

### For Linux
#### Ubuntu 20.04 (LTS)
Run the following command in sequence:
```bash
sudo apt-get update -y
sudo apt-get install git curl zip unzip tar cmake ninja-build build-essential pkg-config gcc-10 gcc-10-arm-linux-gnueabihf gcc-10-aarch64-linux-gnu g++-10 g++-10-arm-linux-gnueabihf g++-10-aarch64-linux-gnu -y

# Optional - if you want to select gcc 10
sudo update-alternatives --install /usr/bin/arm-linux-gnueabihf-gcc arm-linux-gnueabihf-gcc /usr/bin/arm-linux-gnueabihf-gcc-10 20
sudo update-alternatives --install /usr/bin/arm-linux-gnueabihf-g++ arm-linux-gnueabihf-g++ /usr/bin/arm-linux-gnueabihf-g++-10 20

sudo update-alternatives --install /usr/bin/aarch64-linux-gnu-gcc aarch64-linux-gnu-gcc /usr/bin/aarch64-linux-gnu-gcc-10 20
sudo update-alternatives --install /usr/bin/aarch64-linux-gnu-g++ aarch64-linux-gnu-g++ /usr/bin/aarch64-linux-gnu-g++-10 20

# Clone this repository
git clone --recursive https://github.com/TheDialgaTeam/Xirorig.git Xirorig
cd Xirorig

# Optional - Configure to use GCC 10 instead
export CC=gcc-10
export CXX=g++-10

# Build for x64-linux
cmake -G Ninja -DCMAKE_BUILD_TYPE=Release -DCMAKE_INSTALL_PREFIX="${PWD}/Xirorig.Native/xirorig_native/out/install/linux-x64" -DVCPKG_TARGET_TRIPLET=x64-linux -S "${PWD}/Xirorig.Native/xirorig_native" -B "${PWD}/Xirorig.Native/xirorig_native/out/build/linux-x64"
ninja -C "${PWD}/Xirorig.Native/xirorig_native/out/build/linux-x64" install

# Build for ARM32-linux
cmake -G Ninja -DCMAKE_BUILD_TYPE=Release -DCMAKE_INSTALL_PREFIX="${PWD}/Xirorig.Native/xirorig_native/out/install/linux-ARM32" -DVCPKG_TARGET_TRIPLET=arm-linux -DVCPKG_CHAINLOAD_TOOLCHAIN_FILE="${PWD}/Xirorig.Native/xirorig_native/cmake/raspberrypi-arm.cmake" -S "${PWD}/Xirorig.Native/xirorig_native" -B "${PWD}/Xirorig.Native/xirorig_native/out/build/linux-ARM32"
ninja -C "${PWD}/Xirorig.Native/xirorig_native/out/build/linux-ARM32" install

# Build for ARM64-linux
cmake -G Ninja -DCMAKE_BUILD_TYPE=Release -DCMAKE_INSTALL_PREFIX="${PWD}/Xirorig.Native/xirorig_native/out/install/linux-ARM64" -DVCPKG_TARGET_TRIPLET=arm64-linux -DVCPKG_CHAINLOAD_TOOLCHAIN_FILE="${PWD}/Xirorig.Native/xirorig_native/cmake/raspberrypi-arm64.cmake" -S "${PWD}/Xirorig.Native/xirorig_native" -B "${PWD}/Xirorig.Native/xirorig_native/out/build/linux-ARM64"
ninja -C "${PWD}/Xirorig.Native/xirorig_native/out/build/linux-ARM64" install

# All output is at Xirorig.Native/xirorig_native/out/build/linux-<Arch>
```

## Donations
- BTC: `3Dc5jpiyuts136YhamcRbAeue7mi44gW8d`
- LTC: `LUU9Avuanafmq1vMp53AWS1mr3GCCc2X42`
- XMR: `42oj7eV68BK8Z8wcGzLMFEJgAQG22Z3ajGdtpmJx5p7iDqEgG91wNybWbwaVe4vUMveKAzAiA4j8xgUi29TpKXpm3zwfwWN`

## Developers
- The Dialga Team (Yong Jian Ming)
