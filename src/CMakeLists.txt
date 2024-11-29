file(MAKE_DIRECTORY "${CMAKE_BINARY_DIR}/include")
configure_file("${CMAKE_CURRENT_SOURCE_DIR}/About.h.in" "${CMAKE_BINARY_DIR}/include/About.h")
include_directories("${CMAKE_BINARY_DIR}/include")

add_subdirectory("Configuration")
add_subdirectory("Logging")
add_subdirectory("Utility")

add_executable(Xenorig main.cpp)

target_link_libraries(Xenorig
        PRIVATE
        Configuration Utility Logging
        cxxopts::cxxopts lz4::lz4 OpenSSL::SSL OpenSSL::Crypto rapidjson
)

install(TARGETS Xenorig PERMISSIONS OWNER_READ OWNER_WRITE OWNER_EXECUTE GROUP_READ GROUP_EXECUTE WORLD_READ WORLD_EXECUTE)

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