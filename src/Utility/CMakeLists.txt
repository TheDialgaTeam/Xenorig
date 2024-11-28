add_library(Utility STATIC)

target_sources(Utility
        PUBLIC
        "Console.h"
        "CpuInformation.h"
        PRIVATE
        "Console.cpp"
        "CpuInformation.cpp"
)

target_link_libraries(Utility PRIVATE cpuinfo::cpuinfo)
