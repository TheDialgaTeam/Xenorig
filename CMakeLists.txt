cmake_minimum_required(VERSION 3.15)
cmake_policy(SET CMP0091 NEW)

message(CHECK_START "Finding CMAKE_SOURCE_DIR")
file(TO_CMAKE_PATH "${CMAKE_SOURCE_DIR}" CMAKE_SOURCE_DIR_NORMAL)
message(CHECK_PASS "${CMAKE_SOURCE_DIR_NORMAL}")

message(CHECK_START "Finding CMAKE_BINARY_DIR")
file(TO_CMAKE_PATH "${CMAKE_BINARY_DIR}" CMAKE_BINARY_DIR_NORMAL)

if(CMAKE_SOURCE_DIR_NORMAL STREQUAL CMAKE_BINARY_DIR_NORMAL)
    message(CHECK_FAIL "${CMAKE_BINARY_DIR_NORMAL}")
    message(FATAL_ERROR "CMAKE_BINARY_DIR cannot be the same as CMAKE_SOURCE_DIR.")
else()
    message(CHECK_PASS "${CMAKE_BINARY_DIR_NORMAL}")
endif()

message(CHECK_START "Finding vcpkg installation")

if(EXISTS "${CMAKE_SOURCE_DIR}/vcpkg/scripts/buildsystems/vcpkg.cmake")
    set(VCPKG_ROOT "${CMAKE_SOURCE_DIR}/vcpkg")
elseif(DEFINED ENV{VCPKG_ROOT} AND EXISTS "$ENV{VCPKG_ROOT}/scripts/buildsystems/vcpkg.cmake")
    set(VCPKG_ROOT "$ENV{VCPKG_ROOT}")
else()
    message(WARNING "vcpkg installation is missing from your source. Did you forget to clone with --recursive flag?")

    find_package(Git QUIET)

    if(Git_FOUND)
        if(EXISTS "${CMAKE_SOURCE_DIR}/.git")
            execute_process(COMMAND "${GIT_EXECUTABLE}" submodule update --init --recursive WORKING_DIRECTORY "${CMAKE_SOURCE_DIR}" RESULT_VARIABLE GIT_SUBMODULE_INIT_RESULT)

            if(GIT_SUBMODULE_INIT_RESULT)
                message(CHECK_FAIL "not found")
                message(FATAL_ERROR "Unable to initialize vcpkg submodule.")
            endif()
        else()
            execute_process(COMMAND "${GIT_EXECUTABLE}" clone https://github.com/microsoft/vcpkg WORKING_DIRECTORY "${CMAKE_SOURCE_DIR}" RESULT_VARIABLE GIT_CLONE_RESULT)

            if(GIT_CLONE_RESULT)
                message(CHECK_FAIL "not found")
                message(FATAL_ERROR "Unable to initialize vcpkg submodule.")
            endif()
        endif()
    else()
        message(CHECK_FAIL "not found")
        message(FATAL_ERROR "Git not found. Unable to initialize vcpkg submodule.")
    endif()

    set(VCPKG_ROOT "${CMAKE_SOURCE_DIR}/vcpkg")
endif()

message(CHECK_PASS "${VCPKG_ROOT}")

if(DEFINED CMAKE_TOOLCHAIN_FILE)
    file(REAL_PATH ${CMAKE_TOOLCHAIN_FILE} CMAKE_TOOLCHAIN_FILE_ABSOLUTE)
    file(TO_CMAKE_PATH ${CMAKE_TOOLCHAIN_FILE_ABSOLUTE} CMAKE_TOOLCHAIN_FILE_NORMAL)

    if(NOT CMAKE_TOOLCHAIN_FILE_NORMAL STREQUAL "${VCPKG_ROOT}/scripts/buildsystems/vcpkg.cmake")
        message(WARNING "You are trying to override CMAKE_TOOLCHAIN_FILE which is required for vcpkg. Set VCPKG_CHAINLOAD_TOOLCHAIN_FILE instead.")
        set(VCPKG_CHAINLOAD_TOOLCHAIN_FILE "${CMAKE_TOOLCHAIN_FILE_ABSOLUTE}")
        include("${VCPKG_CHAINLOAD_TOOLCHAIN_FILE}")
    endif()
endif()

set(CMAKE_TOOLCHAIN_FILE "${VCPKG_ROOT}/scripts/buildsystems/vcpkg.cmake")

set(SUPPORTED_VCPKG_TARGET_TRIPLET
    "x64-windows-static"
    "x64-windows-static-release"
    "x64-mingw-static"
    "x64-mingw-static-release"
    "x64-linux"
    "x64-linux-clang"
    "x64-linux-release"
    "x64-linux-release-clang"
    "arm64-linux"
    "arm64-linux-clang"
    "arm64-linux-release"
    "arm64-linux-release-clang"
    "x64-osx"
    "x64-osx-release")

message(CHECK_START "Finding VCPKG_TARGET_TRIPLET")

if(NOT DEFINED VCPKG_TARGET_TRIPLET)
    if((DEFINED CMAKE_SYSTEM_NAME AND CMAKE_SYSTEM_NAME STREQUAL "Windows") OR (NOT DEFINED CMAKE_SYSTEM_NAME AND CMAKE_HOST_SYSTEM_NAME STREQUAL "Windows"))
        if(DEFINED ENV{VCToolsVersion})
            if(DEFINED CMAKE_BUILD_TYPE AND CMAKE_BUILD_TYPE STREQUAL "Debug")
                set(VCPKG_TARGET_TRIPLET x64-windows-static)
            else()
                set(VCPKG_TARGET_TRIPLET x64-windows-static-release)
            endif()
        elseif(DEFINED ENV{MSYSTEM} AND ENV{MSYSTEM} STREQUAL "MINGW64")
            if(DEFINED CMAKE_BUILD_TYPE AND CMAKE_BUILD_TYPE STREQUAL "Debug")
                set(VCPKG_TARGET_TRIPLET x64-mingw-static)
            else()
                set(VCPKG_TARGET_TRIPLET x64-mingw-static-release)
            endif()
        else()
            message(CHECK_FAIL "not found")
            message(FATAL_ERROR "Target build system is not supported.")
        endif()
    elseif((DEFINED CMAKE_SYSTEM_NAME AND CMAKE_SYSTEM_NAME STREQUAL "Linux") OR (NOT DEFINED CMAKE_SYSTEM_NAME AND CMAKE_HOST_SYSTEM_NAME STREQUAL "Linux"))
        if((DEFINED CMAKE_SYSTEM_PROCESSOR AND CMAKE_SYSTEM_PROCESSOR STREQUAL "x86_64") OR (NOT DEFINED CMAKE_SYSTEM_PROCESSOR AND CMAKE_HOST_SYSTEM_PROCESSOR STREQUAL "x86_64"))
            if(DEFINED CMAKE_BUILD_TYPE AND CMAKE_BUILD_TYPE STREQUAL "Debug")
                set(VCPKG_TARGET_TRIPLET x64-linux)
            else()
                set(VCPKG_TARGET_TRIPLET x64-linux-release)
            endif()
        elseif((DEFINED CMAKE_SYSTEM_PROCESSOR AND CMAKE_SYSTEM_PROCESSOR STREQUAL "aarch64") OR (NOT DEFINED CMAKE_SYSTEM_PROCESSOR AND CMAKE_HOST_SYSTEM_PROCESSOR STREQUAL "aarch64"))
            if(DEFINED CMAKE_BUILD_TYPE AND CMAKE_BUILD_TYPE STREQUAL "Debug")
                set(VCPKG_TARGET_TRIPLET arm64-linux)
            else()
                set(VCPKG_TARGET_TRIPLET arm64-linux-release)
            endif()
        endif()
    endif()

    message(CHECK_PASS "${VCPKG_TARGET_TRIPLET}")
elseif(DEFINED VCPKG_TARGET_TRIPLET AND NOT VCPKG_TARGET_TRIPLET IN_LIST SUPPORTED_VCPKG_TARGET_TRIPLET)
    message(CHECK_FAIL "${VCPKG_TARGET_TRIPLET}")
    list(JOIN SUPPORTED_VCPKG_TARGET_TRIPLET "\n" SUPPORTED_VCPKG_TARGET_TRIPLET_STRING)
    message(FATAL_ERROR "Invalid VCPKG_TARGET_TRIPLET selected.\nSupported Triplet(s):\n${SUPPORTED_VCPKG_TARGET_TRIPLET_STRING}")
else()
    message(CHECK_PASS "${VCPKG_TARGET_TRIPLET}")
endif()

# Set vcpkg toolchain options
set(VCPKG_OVERLAY_PORTS "${CMAKE_SOURCE_DIR}/ports")
set(VCPKG_OVERLAY_TRIPLETS "${CMAKE_SOURCE_DIR}/triplets")
set(VCPKG_BOOTSTRAP_OPTIONS "--disableMetrics")
set(VCPKG_INSTALL_OPTIONS "--clean-after-build")

include("${CMAKE_TOOLCHAIN_FILE}")

project(Xenorig VERSION 2.0.0.0 HOMEPAGE_URL https://github.com/TheDialgaTeam/Xenorig LANGUAGES C CXX)

# Enable c17
set(CMAKE_C_STANDARD 17)
set(CMAKE_C_STANDARD_REQUIRED ON)

# Enable c++17
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

include(CheckCCompilerFlag)
include(CheckCXXCompilerFlag)

# Set default build type.
set(CMAKE_BUILD_TYPE Release CACHE STRING "Choose the type of build, options are: Debug, Release, RelWithDebInfo")

# Set target architecture for optimizations
set(ARCH native CACHE STRING "Set target architecture, options are: native, default")

if(CMAKE_C_COMPILER_ID STREQUAL "GNU" OR CMAKE_C_COMPILER_ID STREQUAL "Clang")
    if(NOT ARCH STREQUAL "default")
        add_compile_options(-march=${ARCH})
    endif()
endif()

set(USE_CCACHE ON CACHE BOOL "Use ccache to speed up incremental compilation.")

if(USE_CCACHE)
    find_program(CCACHE_PROGRAM ccache)

    if(CCACHE_PROGRAM)
        message(STATUS "Found ccache: ${CCACHE_PROGRAM}")

        if(MSVC)
            file(COPY_FILE "${CCACHE_PROGRAM}" "${CMAKE_BINARY_DIR}/cl.exe" ONLY_IF_DIFFERENT)

            # By default Visual Studio generators will use /Zi which is not compatible
            # with ccache, so tell Visual Studio to use /Z7 instead.
            if(CMAKE_VERSION VERSION_GREATER_EQUAL "3.25")
                message(STATUS "Setting MSVC debug information format to 'Embedded'")
                set(CMAKE_MSVC_DEBUG_INFORMATION_FORMAT "$<$<CONFIG:Debug,RelWithDebInfo>:Embedded>")
            endif()

            set(CMAKE_VS_GLOBALS
                "CLToolExe=cl.exe"
                "CLToolPath=${CMAKE_BINARY_DIR}"
                "UseMultiToolTask=true"
                "DebugInformationFormat=OldStyle")
        else()
            set(CMAKE_C_COMPILER_LAUNCHER "${CCACHE_PROGRAM}")
            set(CMAKE_CXX_COMPILER_LAUNCHER "${CCACHE_PROGRAM}")
        endif()
    endif()
endif()

# This section helps us tag our builds with the git commit information
set(SET_COMMIT_ID_IN_VERSION ON CACHE BOOL "Include commit ID in version")

file(MAKE_DIRECTORY "${CMAKE_BINARY_DIR}/version")

if(NOT SET_COMMIT_ID_IN_VERSION)
    unset(BUILD_COMMIT_ID)
elseif(NOT DEFINED BUILD_COMMIT_ID)
    if(EXISTS "${CMAKE_SOURCE_DIR}/.git")
        find_package(Git QUIET)

        if(Git_FOUND)
            execute_process(
                COMMAND "${GIT_EXECUTABLE}" rev-parse --short HEAD
                WORKING_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}"
                RESULT_VARIABLE BUILD_COMMIT_ID_RESULT
                OUTPUT_VARIABLE BUILD_COMMIT_ID
                OUTPUT_STRIP_TRAILING_WHITESPACE)
        endif()
    endif()
endif()

configure_file("${CMAKE_CURRENT_SOURCE_DIR}/include/version.h.in" "${CMAKE_CURRENT_BINARY_DIR}/version/version.h")

include_directories("${CMAKE_CURRENT_BINARY_DIR}/version" "include" "src")

# Platform specific code base information is applied here
if(MSVC)
    set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
    add_compile_definitions(WIN32_LEAN_AND_MEAN _WIN32_WINNT=0x0601)
elseif(MINGW)
    add_link_options(-static)
    add_compile_definitions(WIN32_LEAN_AND_MEAN _WIN32_WINNT=0x0601)
elseif(NOT APPLE)
    add_link_options(-static-libgcc -static-libstdc++)
endif()

find_package(httplib CONFIG REQUIRED)
find_package(cpuinfo CONFIG REQUIRED)
find_package(cxxopts CONFIG REQUIRED)
find_package(lz4 CONFIG REQUIRED)
find_package(OpenSSL REQUIRED)
find_package(RapidJSON CONFIG REQUIRED)
find_package(spdlog CONFIG REQUIRED)

add_subdirectory("src")
