add_library(utility STATIC)

target_sources(utility
        PUBLIC
        "console.h"
        "cpu_information.h"
        "sha3.h"
        PRIVATE
        "console.cpp"
        "cpu_information.cpp"
)

target_link_libraries(utility PRIVATE cpuinfo::cpuinfo)
