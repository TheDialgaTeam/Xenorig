cmake_minimum_required(VERSION 3.12)

set(CMAKE_TOOLCHAIN_FILE "${CMAKE_CURRENT_SOURCE_DIR}/vcpkg/scripts/buildsystems/vcpkg.cmake" CACHE FILEPATH "Vcpkg toolchain file")

project(xeno_native VERSION 1.0.0 DESCRIPTION "Xeno Native Library")

set(XENO_NATIVE_SOURCE_FILES
	"src/Algorithms/Xenophyte/Centralized/XenophyteCentralizedAlgorithm.c"
	"src/Utilities/Base58Utility.c"
	"src/Utilities/Base64Utility.c"
	"src/Utilities/BufferUtility.c"
	"src/Utilities/CpuInformationUtility.c"
	"src/Utilities/KeyDerivationFunctionUtility.c"
	"src/Utilities/MessageDigestUtility.c"
	"src/Utilities/RandomNumberGeneratorUtility.c"
	"src/Utilities/SymmetricAlgorithmUtility.c")

set(XENO_NATIVE_PUBLIC_HEADER
	"${PROJECT_BINARY_DIR}/xeno_native_export.h"
	"src/global.h"
	"src/Algorithms/Xenophyte/Centralized/XenophyteCentralizedAlgorithm.h"
	"src/Utilities/Base58Utility.h"
	"src/Utilities/Base64Utility.h"
	"src/Utilities/BufferUtility.h"
	"src/Utilities/CpuInformationUtility.h"
	"src/Utilities/KeyDerivationFunctionUtility.h"
	"src/Utilities/MessageDigestUtility.h"
	"src/Utilities/RandomNumberGeneratorUtility.h"
	"src/Utilities/SymmetricAlgorithmUtility.h")

find_package(OpenSSL REQUIRED)
find_package(lz4 CONFIG REQUIRED)
find_package(cpuinfo CONFIG REQUIRED)

set(XENO_NATIVE_TARGET_LINK_LIBRARIES OpenSSL::SSL OpenSSL::Crypto lz4::lz4 cpuinfo::clog cpuinfo::cpuinfo)

add_library("${PROJECT_NAME}_SHARED" SHARED)
add_library("${PROJECT_NAME}_STATIC" STATIC)

set(CMAKE_C_VISIBILITY_PRESET hidden)
set(CMAKE_VISIBILITY_INLINES_HIDDEN 1)

include(GenerateExportHeader)
generate_export_header("${PROJECT_NAME}_SHARED" BASE_NAME ${PROJECT_NAME})

#[[
==================================================
STATIC BUILD CONFIGURATION
==================================================
]]

set_target_properties("${PROJECT_NAME}_STATIC"
	PROPERTIES 
		VERSION ${PROJECT_VERSION} 
		PUBLIC_HEADER "${XENO_NATIVE_PUBLIC_HEADER}"
		C_STANDARD 99
		C_STANDARD_REQUIRED ON
		COMPILE_FLAGS -D${PROJECT_NAME}_STATIC_DEFINE
		OUTPUT_NAME ${PROJECT_NAME}
		ARCHIVE_OUTPUT_DIRECTORY static)

target_sources("${PROJECT_NAME}_STATIC" PRIVATE ${XENO_NATIVE_SOURCE_FILES})
target_include_directories("${PROJECT_NAME}_STATIC" PRIVATE "src" ${PROJECT_BINARY_DIR})
target_link_libraries("${PROJECT_NAME}_STATIC" PRIVATE ${XENO_NATIVE_TARGET_LINK_LIBRARIES})

install(TARGETS "${PROJECT_NAME}_STATIC"
	LIBRARY
		DESTINATION ${CMAKE_INSTALL_LIBDIR}
		PUBLIC_HEADER DESTINATION ${CMAKE_INSTALL_INCLUDEDIR})

#[[
==================================================
SHARED BUILD CONFIGURATION
==================================================
]]

set_target_properties("${PROJECT_NAME}_SHARED"
	PROPERTIES
		VERSION ${PROJECT_VERSION}
		PUBLIC_HEADER "${XENO_NATIVE_PUBLIC_HEADER}"
		C_STANDARD 99
		C_STANDARD_REQUIRED ON
		COMPILE_FLAGS -D${PROJECT_NAME}_SHARED_EXPORTS
		OUTPUT_NAME ${PROJECT_NAME}
		ARCHIVE_OUTPUT_DIRECTORY shared)

target_sources("${PROJECT_NAME}_SHARED" PRIVATE ${XENO_NATIVE_SOURCE_FILES})
target_include_directories("${PROJECT_NAME}_SHARED" PRIVATE "src" ${PROJECT_BINARY_DIR})
target_link_libraries("${PROJECT_NAME}_SHARED" PRIVATE ${XENO_NATIVE_TARGET_LINK_LIBRARIES})

install(TARGETS "${PROJECT_NAME}_SHARED"
	LIBRARY
		DESTINATION ${CMAKE_INSTALL_LIBDIR}
		PUBLIC_HEADER DESTINATION ${CMAKE_INSTALL_INCLUDEDIR})
