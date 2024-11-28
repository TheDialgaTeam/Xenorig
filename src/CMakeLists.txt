configure_file("${CMAKE_CURRENT_SOURCE_DIR}/About.h.in" "${CMAKE_CURRENT_SOURCE_DIR}/About.h")

add_subdirectory("Logging")
add_subdirectory("Utility")

add_executable(Xenorig
        About.h
        main.cpp)

target_link_libraries(Xenorig PRIVATE Utility Logging cxxopts::cxxopts lz4::lz4 OpenSSL::SSL OpenSSL::Crypto rapidjson)

# CPack
set(CPACK_PACKAGE_NAME "Xenorig")
set(CPACK_PACKAGE_VENDOR "The Dialga Team")
set(CPACK_PACKAGE_DIRECTORY "${CMAKE_BINARY_DIR}/Packaging")
set(CPACK_PACKAGE_FILE_NAME "${CPACK_PACKAGE_NAME}-${SET_PACKAGE_OUTPUT_SUFFIX}")

if (CMAKE_HOST_SYSTEM_NAME STREQUAL "Windows")
    set(CPACK_GENERATOR "ZIP")
elseif (CMAKE_HOST_SYSTEM_NAME STREQUAL "Linux")
    set(CPACK_GENERATOR "TGZ;DEB")
elseif (CMAKE_HOST_SYSTEM_NAME STREQUAL "Darwin")
    set(CPACK_GENERATOR "TGZ")
endif ()

include(CPack)