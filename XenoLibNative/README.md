# XenoLibNative
***This is the native c library used by Xiro. Without this library, mining speed will be slower.***

#### Table of contents
  - [Compile Instructions](#compile-instructions)
    - [For Windows](#for-windows)
      - [Via Command Line](#via-command-line)
    - [For Linux](#for-linux)
      - [Ubuntu 20.04 (LTS)](#ubuntu-2004-lts)
  - [Donations](#donations)
  - [Developers](#developers)

## Compile Instructions
Xeno Native uses [CMake](https://cmake.org/download) and [vcpkg](https://vcpkg.io/en/index.html) to build.

### For Windows
#### Via Command Line:
Minimum Requirements:
- [Git](https://git-scm.com/downloads)
- [MSYS2](https://www.msys2.org/)
  - Run MSYS2 MSYS and enter the following command in order:
    ```bash
    pacman -Syu
    pacman -Syu
    pacman -S --needed mingw-w64-x86_64-toolchain mingw-w64-x86_64-cmake mingw-w64-x86_64-ninja mingw-w64-i686-toolchain mingw-w64-i686-cmake mingw-w64-i686-ninja
    ```

```cmd
git clone --recursive https://github.com/TheDialgaTeam/Xenorig.git Xenorig

cd Xenorig/XenoLibNative

###################################################
# If you want to use GCC toolchain (only x64/x86) #
###################################################
SET CHERE_INVOKING=yes

# For x64
SET MSYSTEM=MINGW64

C:\msys64\usr\bin\bash -c "cmake --preset windows-x64 && cmake --build --preset windows-x64 && cmake --install build"

# For x86
SET MSYSTEM=MINGW32

C:\msys64\usr\bin\bash -c "cmake --preset windows-x86 && cmake --build --preset windows-x86 && cmake --install build"
```

### For Linux
#### Ubuntu 20.04 (LTS)
Run the following command in sequence:
```bash
sudo apt-get update -y
sudo apt-get install git curl zip unzip tar cmake ninja-build build-essential pkg-config gcc-10-arm-linux-gnueabihf gcc-10-aarch64-linux-gnu g++-10-arm-linux-gnueabihf g++-10-aarch64-linux-gnu -y

# Clone this repository
git clone --recursive https://github.com/TheDialgaTeam/Xenorig.git Xenorig
cd Xenorig/XenoLibNative

# Build for x64-linux
cmake --preset linux-x64
cmake --build --preset linux-x64
cmake --install build

# Build for ARM32-linux
cmake --preset linux-arm
cmake --build --preset linux-arm
cmake --install build

# Build for ARM64-linux
cmake --preset linux-arm64
cmake --build --preset linux-arm64
cmake --install build
```

## Donations
- BTC: `3Dc5jpiyuts136YhamcRbAeue7mi44gW8d`
- LTC: `LUU9Avuanafmq1vMp53AWS1mr3GCCc2X42`
- XMR: `42oj7eV68BK8Z8wcGzLMFEJgAQG22Z3ajGdtpmJx5p7iDqEgG91wNybWbwaVe4vUMveKAzAiA4j8xgUi29TpKXpm3zwfwWN`

## Developers
- The Dialga Team (Yong Jian Ming)
