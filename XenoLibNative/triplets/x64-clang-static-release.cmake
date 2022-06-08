set(VCPKG_TARGET_ARCHITECTURE x64)
set(VCPKG_CRT_LINKAGE dynamic)
set(VCPKG_LIBRARY_LINKAGE static)
set(VCPKG_BUILD_TYPE release)

if(CMAKE_HOST_SYSTEM_NAME STREQUAL "Windows")
   set(VCPKG_CMAKE_SYSTEM_NAME MinGW)
   set(VCPKG_ENV_PASSTHROUGH PATH)
   set(VCPKG_CHAINLOAD_TOOLCHAIN_FILE "${CMAKE_CURRENT_SOURCE_DIR}/../cmake/windows.cmake")
elseif(CMAKE_HOST_SYSTEM_NAME STREQUAL "Linux")
   set(VCPKG_CMAKE_SYSTEM_NAME Linux)
   set(VCPKG_CHAINLOAD_TOOLCHAIN_FILE "${CMAKE_CURRENT_SOURCE_DIR}/../cmake/linux.cmake")
elseif(CMAKE_HOST_SYSTEM_NAME STREQUAL "Darwin")
   set(VCPKG_CMAKE_SYSTEM_NAME Darwin)
   set(VCPKG_OSX_ARCHITECTURES x86_64)
   set(VCPKG_CHAINLOAD_TOOLCHAIN_FILE "${CMAKE_CURRENT_SOURCE_DIR}/../cmake/osx.cmake")
endif()
