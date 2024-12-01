#include "logging/logger.h"

#include <fmt/format.h>
#include <spdlog/sinks/stdout_color_sinks.h>
#include <spdlog/spdlog.h>

#include "about.h"
#include "ansi_escape_code_constants.h"
#include "utility/cpu_information.h"

#ifdef _WIN32
    #include <windows.h>
#endif

namespace xenorig::logging
{
    void configure_logging()
    {
#if _WIN32
        const auto stdoutHandle = GetStdHandle(STD_OUTPUT_HANDLE);
        DWORD consoleMode;

        if (stdoutHandle != nullptr && GetConsoleMode(stdoutHandle, &consoleMode))
        {
            SetConsoleMode(stdoutHandle, consoleMode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
        }
#endif

        const auto console = spdlog::stdout_color_mt("Console");
        console->set_pattern(fmt::format("{}%Y-%m-%d %H:%M:%S{} %v",
                                         ansi_escape_code_constants::dark_gray_foreground_color,
                                         ansi_escape_code_constants::reset));

        const auto consoleNoTimestamp = spdlog::stdout_color_mt("ConsoleNoTimestamp");
        consoleNoTimestamp->set_pattern("%v");

#ifdef NDEBUG
        spdlog::set_level(spdlog::level::info);
#else
        spdlog::set_level(spdlog::level::trace);
#endif
    }

    void print_about()
    {
        const auto console = spdlog::get("ConsoleNoTimestamp");
        console->info(" {0}* {1}{5:<12} {2}{6}/{7} {3}{8}/{9}{4}", ansi_escape_code_constants::green_foreground_color,
                      ansi_escape_code_constants::white_foreground_color,
                      ansi_escape_code_constants::cyan_foreground_color,
                      ansi_escape_code_constants::dark_gray_foreground_color, ansi_escape_code_constants::reset,
                      "About", PROJECT_NAME, BUILD_COMMIT_ID == "" ? PROJECT_VERSION : PROJECT_VERSION_LONG,
                      COMPILER_ID, COMPILER_VERSION);
    }

    void print_cpu()
    {
        const auto cpuInfo = utility::cpu_information::get_cpu_information();
        const auto console = spdlog::get("ConsoleNoTimestamp");
        console->info(
            " {0}* {1}{5:<12} {2}{6} L2: {3}{7:.1f}{4}MB L3: {3}{8:.1f}{4}MB {3}{9:d}{4}C/{3}{10:d}{4}T",
            ansi_escape_code_constants::green_foreground_color, ansi_escape_code_constants::white_foreground_color,
            ansi_escape_code_constants::dark_gray_foreground_color, ansi_escape_code_constants::cyan_foreground_color,
            ansi_escape_code_constants::reset, "CPU", cpuInfo.processor_name, cpuInfo.l2_cache_size / 1024.0 / 1024.0,
            cpuInfo.l3_cache_size / 1024.0 / 1024.0, cpuInfo.processor_core_count, cpuInfo.processor_count);
    }
} // namespace xenorig::logging
