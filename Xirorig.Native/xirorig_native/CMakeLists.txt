cmake_minimum_required(VERSION 3.8)

set(CMAKE_TOOLCHAIN_FILE ${CMAKE_CURRENT_SOURCE_DIR}/vcpkg/scripts/buildsystems/vcpkg.cmake CACHE STRING "Vcpkg toolchain file")

project(xirorig_native VERSION 1.0.0 DESCRIPTION "Xirorig Native Library")

add_library(${PROJECT_NAME} SHARED src/xirorig_native.c)

target_include_directories(${PROJECT_NAME} PRIVATE include)

set_target_properties(${PROJECT_NAME} PROPERTIES 
	VERSION ${PROJECT_VERSION}
	SOVERSION 1
	PUBLIC_HEADER include/xirorig_native.h
)

find_package(OpenSSL REQUIRED)
find_package(lz4 CONFIG REQUIRED)

target_link_libraries(${PROJECT_NAME} PRIVATE OpenSSL::Crypto lz4::lz4)

install (TARGETS ${PROJECT_NAME}
	LIBRARY DESTINATION ${CMAKE_INSTALL_LIBDIR}
    PUBLIC_HEADER DESTINATION ${CMAKE_INSTALL_INCLUDEDIR}
)
