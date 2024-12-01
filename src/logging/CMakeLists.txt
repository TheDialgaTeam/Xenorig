add_library(logging STATIC)

target_sources(logging
        PUBLIC
        "ansi_escape_code_constants.h"
        "logger.h"
        PRIVATE
        "ansi_escape_code_constants.cpp"
        "logger.cpp"
)

target_link_libraries(logging PRIVATE utility spdlog::spdlog)