cmake_minimum_required(VERSION 3.12)

set(CMAKE_TOOLCHAIN_FILE ${CMAKE_CURRENT_SOURCE_DIR}/vcpkg/scripts/buildsystems/vcpkg.cmake CACHE FILEPATH "Vcpkg toolchain file")
set(EXCLUDE_CPU_INFO_DEPENDENCY false CACHE BOOL "Exclude cpu info dependency")

project(xirorig_native VERSION 1.0.0 DESCRIPTION "Xirorig Native Library")

set(
	XIRORIG_NATIVE_SOURCE_FILES
		src/Algorithm/Xiropht/Decentralized/XirophtDecentralizedCpuMiner.c
		src/Utility/Sha3Utility.c
		src/Utility/Sha2Utility.c
)

set(
	XIRORIG_NATIVE_PUBLIC_HEADER
		${PROJECT_BINARY_DIR}/${PROJECT_NAME}_export.h
		include/Algorithm/Xiropht/Decentralized/XirophtDecentralizedCpuMiner.h
		include/Utility/Sha2Utility.h
		include/Utility/Sha3Utility.h
)

set(
	XIRORIG_NATIVE_TARGET_LINK_LIBRARIES
		OpenSSL::SSL OpenSSL::Crypto
		lz4::lz4
)

if (MSVC)
	set(XIRORIG_NATIVE_TARGET_LINK_LIBRARIES advapi32.lib crypt32.lib ${XIRORIG_NATIVE_TARGET_LINK_LIBRARIES})
endif()

find_package(OpenSSL REQUIRED)
find_package(lz4 CONFIG REQUIRED)

if (!${EXCLUDE_CPU_INFO_DEPENDENCY})
	set(XIRORIG_NATIVE_SOURCE_FILES src/Utility/CpuInformationUtility.c ${XIRORIG_NATIVE_SOURCE_FILES})
	set(XIRORIG_NATIVE_PUBLIC_HEADER include/Utility/CpuInformationUtility.h ${XIRORIG_NATIVE_PUBLIC_HEADER})
	set(XIRORIG_NATIVE_TARGET_LINK_LIBRARIES unofficial::cpuinfo::clog unofficial::cpuinfo::cpuinfo ${XIRORIG_NATIVE_TARGET_LINK_LIBRARIES})

	find_package(unofficial-cpuinfo CONFIG REQUIRED)
endif()

add_library(${PROJECT_NAME} SHARED)

include(GenerateExportHeader)
generate_export_header(${PROJECT_NAME})

set_target_properties(${PROJECT_NAME} PROPERTIES VERSION ${PROJECT_VERSION} PUBLIC_HEADER "${XIRORIG_NATIVE_PUBLIC_HEADER}")

target_sources(${PROJECT_NAME} PRIVATE ${XIRORIG_NATIVE_SOURCE_FILES})
target_include_directories(${PROJECT_NAME} PRIVATE include ${PROJECT_BINARY_DIR})
target_compile_definitions(${PROJECT_NAME} PRIVATE ${PROJECT_NAME}_EXPORTS)
target_link_libraries(${PROJECT_NAME} PRIVATE ${XIRORIG_NATIVE_TARGET_LINK_LIBRARIES})

if(MSVC)
	target_link_options(${PROJECT_NAME} PRIVATE /NODEFAULTLIB:libcmt.lib /NODEFAULTLIB:libcmtd.lib /NODEFAULTLIB:msvcrtd.lib)
endif()

install(TARGETS ${PROJECT_NAME} LIBRARY DESTINATION ${CMAKE_INSTALL_LIBDIR} PUBLIC_HEADER DESTINATION ${CMAKE_INSTALL_INCLUDEDIR})
