add_subdirectory("Utility")

add_executable(Xenorig
        main.cpp
        Logger/Logger.cpp
        Config/XenorigConfig.cpp
        Config/XenorigConfig.h)

target_link_libraries(Xenorig PRIVATE Utility cxxopts::cxxopts lz4::lz4 OpenSSL::SSL OpenSSL::Crypto rapidjson spdlog::spdlog)

# CPack
set(CPACK_PACKAGE_NAME "Xenorig")
set(CPACK_PACKAGE_VENDOR "The Dialga Team")
set(CPACK_PACKAGE_DIRECTORY "${CMAKE_BINARY_DIR}/Packaging")
set(CPACK_PACKAGE_FILE_NAME "${CPACK_PACKAGE_NAME}-${SET_PACKAGE_OUTPUT_SUFFIX}")

if(CMAKE_HOST_SYSTEM_NAME STREQUAL "Windows")
    set(CPACK_GENERATOR "ZIP")
elseif(CMAKE_HOST_SYSTEM_NAME STREQUAL "Linux")
    set(CPACK_GENERATOR "TGZ;DEB")
elseif(CMAKE_HOST_SYSTEM_NAME STREQUAL "Darwin")
    set(CPACK_GENERATOR "TGZ")
endif()

include(CPack)