# XIRORIG_NATIVE_ROOT=Xirorig.Native/xirorig_native

if (${env:APPVEYOR_JOB_NAME} -eq "Build Xirorig_Native Windows") {
    # Build Xirorig Native x64
    $env:CHERE_INVOKING = 'yes'
    $env:MSYSTEM = 'MINGW64'

    C:\msys64\usr\bin\bash -lc "cmake -G Ninja -DCMAKE_BUILD_TYPE=Release -DCMAKE_INSTALL_PREFIX=${env:APPVEYOR_BUILD_FOLDER}.Replace('C:\', '/C/').Replace('\', '/')/${env:XIRORIG_NATIVE_ROOT}/out/install/x64-windows -DVCPKG_TARGET_TRIPLET=x64-mingw-static -S ${env:APPVEYOR_BUILD_FOLDER}.Replace('C:\', '/C/').Replace('\', '/')/${env:XIRORIG_NATIVE_ROOT} -B ${env:APPVEYOR_BUILD_FOLDER}.Replace('C:\', '/C/').Replace('\', '/')/${env:XIRORIG_NATIVE_ROOT}/out/build/x64-windows && ninja -C ${env:APPVEYOR_BUILD_FOLDER}.Replace('C:\', '/C/').Replace('\', '/')/${env:XIRORIG_NATIVE_ROOT}/out/build/x64-windows install"

    # Build Xirorig Native x86
    $env:MSYSTEM = 'MINGW32'

    C:\msys64\usr\bin\bash -lc "cmake -G Ninja -DCMAKE_BUILD_TYPE=Release -DCMAKE_INSTALL_PREFIX=${env:APPVEYOR_BUILD_FOLDER}.Replace('C:\', '/C/').Replace('\', '/')/${env:XIRORIG_NATIVE_ROOT}/out/install/x86-windows -DVCPKG_TARGET_TRIPLET=x86-mingw-static -S ${env:APPVEYOR_BUILD_FOLDER}.Replace('C:\', '/C/').Replace('\', '/')/${env:XIRORIG_NATIVE_ROOT} -B ${env:APPVEYOR_BUILD_FOLDER}.Replace('C:\', '/C/').Replace('\', '/')/${env:XIRORIG_NATIVE_ROOT}/out/build/x86-windows && ninja -C ${env:APPVEYOR_BUILD_FOLDER}.Replace('C:\', '/C/').Replace('\', '/')/${env:XIRORIG_NATIVE_ROOT}/out/build/x86-windows install"

    # Build Xirorig Native ARM32
    cmd /c "call 'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsamd64_arm.bat' && cmake -G 'Visual Studio 16 2019' -A ARM -DCMAKE_BUILD_TYPE=Release -DCMAKE_INSTALL_PREFIX=${env:APPVEYOR_BUILD_FOLDER}.Replace('\', '/')/${env:XIRORIG_NATIVE_ROOT}/out/install/ARM32-windows -DVCPKG_TARGET_TRIPLET=arm-windows-static -DEXCLUDE_CPU_INFO_DEPENDENCY=true -S ${env:APPVEYOR_BUILD_FOLDER}.Replace('\', '/')/${env:XIRORIG_NATIVE_ROOT} -B ${env:APPVEYOR_BUILD_FOLDER}.Replace('\', '/')/${env:XIRORIG_NATIVE_ROOT}/out/build/ARM32-windows && msbuild.exe ${env:APPVEYOR_BUILD_FOLDER}.Replace('\', '/')/${env:XIRORIG_NATIVE_ROOT}/out/build/ARM32-windows/INSTALL.vcxproj -p:Configuration=Release"

    # Build Xirorig Native ARM64
    cmd /c "call 'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsamd64_arm64.bat' && cmake -G 'Visual Studio 16 2019' -A ARM64 -DCMAKE_BUILD_TYPE=Release -DCMAKE_INSTALL_PREFIX=${env:APPVEYOR_BUILD_FOLDER}.Replace('\', '/')/${env:XIRORIG_NATIVE_ROOT}/out/install/ARM64-windows -DVCPKG_TARGET_TRIPLET=arm64-windows-static -DEXCLUDE_CPU_INFO_DEPENDENCY=true -S ${env:APPVEYOR_BUILD_FOLDER}.Replace('\', '/')/${env:XIRORIG_NATIVE_ROOT} -B ${env:APPVEYOR_BUILD_FOLDER}.Replace('\', '/')/${env:XIRORIG_NATIVE_ROOT}/out/build/ARM64-windows && msbuild.exe ${env:APPVEYOR_BUILD_FOLDER}.Replace('\', '/')/${env:XIRORIG_NATIVE_ROOT}/out/build/ARM64-windows/INSTALL.vcxproj -p:Configuration=Release"

    Set-Location "${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}/out/install"

    7z a ${env:XIRORIG_NATIVE_WINDOWS_ARTIFACT_NAME} * 
} elseif (${env:APPVEYOR_JOB_NAME} -eq "Build Xirorig_Native Linux") {
    # Build Xirorig Native x64
    cmake -G Ninja -DCMAKE_BUILD_TYPE=Release -DCMAKE_INSTALL_PREFIX="${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}/out/install/x64-linux" -DVCPKG_TARGET_TRIPLET=x64-linux -S "${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}" -B "${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}/out/build/x64-linux"
    ninja -C "${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}/out/build/x64-linux" install

    # Build Xirorig Native ARM32
    cmake -G Ninja -DCMAKE_BUILD_TYPE=Release -DCMAKE_INSTALL_PREFIX="${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}/out/install/ARM32-linux" -DVCPKG_TARGET_TRIPLET=arm-linux -DVCPKG_CHAINLOAD_TOOLCHAIN_FILE="${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}/cmake/raspberrypi-arm.cmake" -S "${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}" -B "${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}/out/build/ARM32-linux"
    ninja -C "${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}/out/build/ARM32-linux" install

    # Build Xirorig Native ARM64
    cmake -G Ninja -DCMAKE_BUILD_TYPE=Release -DCMAKE_INSTALL_PREFIX="${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}/out/install/ARM64-linux" -DVCPKG_TARGET_TRIPLET=arm64-linux -DVCPKG_CHAINLOAD_TOOLCHAIN_FILE="${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}/cmake/raspberrypi-arm64.cmake" -S "${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}" -B "${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}/out/build/ARM64-linux"
    ninja -C "${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}/out/build/ARM64-linux" install

    Set-Location "${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}/out/install"

    7z a ${env:XIRORIG_NATIVE_LINUX_ARTIFACT_NAME} * 
} elseif (${env:APPVEYOR_JOB_NAME} -eq "Build Xirorig_Native MacOS") {
    # Build Xirorig Native x64
    cmake -G Ninja -DCMAKE_BUILD_TYPE=Release -DCMAKE_INSTALL_PREFIX="${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}/out/install/x64-osx" -DVCPKG_TARGET_TRIPLET=x64-osx -S "${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}" -B "${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}/out/build/x64-osx"
    ninja -C "${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}/out/build/x64-osx" install

    Set-Location "${env:APPVEYOR_BUILD_FOLDER}/${env:XIRORIG_NATIVE_ROOT}/out/install"

    7z a ${env:XIRORIG_NATIVE_MACOS_ARTIFACT_NAME} * 
} elseif (${env:APPVEYOR_JOB_NAME} -eq "Build Xirorig Windows") {
    $dotnet = "${env:APPVEYOR_BUILD_FOLDER}\..\dotnet"

    "$dotnet publish -p:PublishProfile=x64-windows.pubxml -p:Platform=x64 -c Release_windows"
    "$dotnet publish -p:PublishProfile=x86-windows.pubxml -p:Platform=x86 -c Release_windows"
    "$dotnet publish -p:PublishProfile=arm-windows.pubxml -p:Platform=ARM32 -c Release_windows"
    "$dotnet publish -p:PublishProfile=arm64-windows.pubxml -p:Platform=ARM64 -c Release_windows"

    Set-Location Xirorig/publish

    7z a x64-windows.zip x64-windows -mx=9
    7z a x86-windows.zip x86-windows -mx=9
    7z a arm-windows.zip arm-windows -mx=9
    7z a arm64-windows.zip arm64-windows -mx=9
} elseif (${env:APPVEYOR_JOB_NAME} -eq "Build Xirorig Linux") {
    $dotnet = "${env:APPVEYOR_BUILD_FOLDER}/../dotnet"

    "$dotnet publish -p:PublishProfile=x64-linux.pubxml -p:Platform=x64 -c Release_linux"
    "$dotnet publish -p:PublishProfile=arm-linux.pubxml -p:Platform=ARM32 -c Release_linux"
    "$dotnet publish -p:PublishProfile=arm64-linux.pubxml -p:Platform=ARM64 -c Release_linux"

    Set-Location Xirorig/publish

    7z a x64-linux.tar x64-linux
    7z a x64-linux.tar.gz x64-linux.tar -mx=9

    7z a arm-linux.tar arm-linux
    7z a arm-linux.tar.gz arm-linux.tar -mx=9

    7z a arm64-linux.tar arm64-linux
    7z a arm64-linux.tar.gz arm64-linux.tar -mx=9
} elseif (${env:APPVEYOR_JOB_NAME} -eq "Build Xirorig MacOS") {
    $dotnet = "${env:APPVEYOR_BUILD_FOLDER}/../dotnet"

    "$dotnet publish -p:PublishProfile=x64-osx.pubxml -p:Platform=x64 -c Release_osx"

    Set-Location Xirorig/publish

    7z a x64-osx.tar x64-osx
    7z a x64-osx.tar.gz x64-osx.tar -mx=9
}
