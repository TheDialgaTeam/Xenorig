add_subdirectory("Utility")

add_executable(Xenorig
        main.cpp
        Logger/Logger.cpp
        Config/XenorigConfig.cpp
        Config/XenorigConfig.h)

target_link_libraries(Xenorig PRIVATE Utility cxxopts::cxxopts lz4::lz4 OpenSSL::SSL OpenSSL::Crypto rapidjson spdlog::spdlog)
