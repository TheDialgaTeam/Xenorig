cmake_minimum_required (VERSION 3.8)

project (xirorig_native VERSION 1.0.0 DESCRIPTION "Xirorig Native Library")

add_library (${PROJECT_NAME} SHARED src/libxirorig.c)

set_target_properties(${PROJECT_NAME} PROPERTIES 
	VERSION ${PROJECT_VERSION}
	SOVERSION 1
	PUBLIC_HEADER include/libxirorig.h
)

if (WIN32)
	if ("${CMAKE_SIZEOF_VOID_P}" STREQUAL "8")
		set(OPENSSL_ROOT_DIR "external/openssl/x64")
	else()
		set(OPENSSL_ROOT_DIR "external/openssl/x86")
	endif()
endif()

find_package (OpenSSL REQUIRED)

target_include_directories(${PROJECT_NAME} PRIVATE .)
target_link_libraries (${PROJECT_NAME} OpenSSL::Crypto)

install (TARGETS ${PROJECT_NAME}
	LIBRARY DESTINATION ${CMAKE_INSTALL_LIBDIR}
    PUBLIC_HEADER DESTINATION ${CMAKE_INSTALL_INCLUDEDIR}
)
