cmake_minimum_required(VERSION 3.12)

set(CMAKE_TOOLCHAIN_FILE "${CMAKE_CURRENT_SOURCE_DIR}/vcpkg/scripts/buildsystems/vcpkg.cmake" CACHE FILEPATH "Vcpkg toolchain file")

project(xeno_native VERSION 1.0.0 DESCRIPTION "Xeno Native Library")

add_library(${PROJECT_NAME} SHARED)

include(GenerateExportHeader)
GENERATE_EXPORT_HEADER(${PROJECT_NAME})

set(XENO_NATIVE_SOURCE_FILES
	"src/Algorithms/Xenophyte/Centralized/XenophyteCentralizedAlgorithm.c"
	"src/Utilities/Base64Utility.c"
	"src/Utilities/CpuInformationUtility.c"
	"src/Utilities/KeyDerivationFunctionUtility.c"
	"src/Utilities/MessageDigestUtility.c"
	"src/Utilities/RandomNumberGeneratorUtility.c"
	"src/Utilities/SymmetricAlgorithmUtility.c")

set(XENO_NATIVE_PUBLIC_HEADER
	"${PROJECT_BINARY_DIR}/${PROJECT_NAME}_export.h"
	"src/global.h"
	"src/Algorithms/Xenophyte/Centralized/XenophyteCentralizedAlgorithm.h"
	"src/Utilities/Base64Utility.h"
	"src/Utilities/CpuInformationUtility.h"
	"src/Utilities/KeyDerivationFunctionUtility.h"
	"src/Utilities/MessageDigestUtility.h"
	"src/Utilities/RandomNumberGeneratorUtility.h"
	"src/Utilities/SymmetricAlgorithmUtility.h")

set(XENO_NATIVE_COMPILE_DEFINITIONS "${PROJECT_NAME}_EXPORTS")

find_package(OpenSSL REQUIRED)
find_package(lz4 CONFIG REQUIRED)

set(XENO_NATIVE_TARGET_LINK_LIBRARIES OpenSSL::SSL OpenSSL::Crypto lz4::lz4)

set(EXCLUDE_CPU_INFO_WHEN_VCPKG_TARGET_TRIPLET_IN "arm-windows" "arm-windows-static" "arm64-windows" "arm64-windows-static")

if ((DEFINED VCPKG_TARGET_TRIPLET AND NOT (VCPKG_TARGET_TRIPLET IN_LIST EXCLUDE_CPU_INFO_WHEN_VCPKG_TARGET_TRIPLET_IN)))
	find_package(cpuinfo CONFIG REQUIRED)

	set(XENO_NATIVE_TARGET_LINK_LIBRARIES ${XENO_NATIVE_TARGET_LINK_LIBRARIES} cpuinfo::clog cpuinfo::cpuinfo)
else()
	set(XENO_NATIVE_COMPILE_DEFINITIONS ${XENO_NATIVE_COMPILE_DEFINITIONS} "NO_CPU_INFO")
endif()

set_target_properties(${PROJECT_NAME} PROPERTIES VERSION ${PROJECT_VERSION} PUBLIC_HEADER "${XENO_NATIVE_PUBLIC_HEADER}")

target_sources(${PROJECT_NAME} PRIVATE ${XENO_NATIVE_SOURCE_FILES})
target_include_directories(${PROJECT_NAME} PRIVATE "src" ${PROJECT_BINARY_DIR})
target_compile_definitions(${PROJECT_NAME} PRIVATE "${XENO_NATIVE_COMPILE_DEFINITIONS}")
target_compile_features(${PROJECT_NAME} PRIVATE c_std_90)
target_link_libraries(${PROJECT_NAME} PRIVATE ${XENO_NATIVE_TARGET_LINK_LIBRARIES})

install(TARGETS ${PROJECT_NAME} LIBRARY DESTINATION ${CMAKE_INSTALL_LIBDIR} PUBLIC_HEADER DESTINATION ${CMAKE_INSTALL_INCLUDEDIR})
