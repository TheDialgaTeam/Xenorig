add_library(Logging STATIC)

target_sources(Logging
        PUBLIC
        "Console/AnsiEscapeCodeConstants.h"
        "Logger.h"
        PRIVATE
        "Console/AnsiEscapeCodeConstants.cpp"
        "Logger.cpp"
)

target_link_libraries(Logging PRIVATE spdlog::spdlog)