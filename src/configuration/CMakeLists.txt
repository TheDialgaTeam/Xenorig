add_library(Configuration STATIC)

target_sources(Configuration
        PUBLIC
        XenorigOptions.h
        PRIVATE
        XenorigOptions.cpp
)
