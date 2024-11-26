#include "Logger/AnsiEscapeCodeConstants.h"
#include "Logger/Logger.h"
#include "Utility/Console.h"
#include "Utility/CpuInformation.h"

#include <version.h>

int main(int argv, char *argc[])
{
    const Xenorig::Logger::Logger logger;
    const Xenorig::Utility::CpuInformation cpuInformation;

    Xenorig::Utility::Console::SetConsoleTitle("Test");

    logger.GetConsole(true)->info(" {}* {}{:<12} {}{}/{} {}{}/{}{}",
                                   Xenorig::Logger::AnsiEscapeCodeConstants::GreenForegroundColor,
                                   Xenorig::Logger::AnsiEscapeCodeConstants::WhiteForegroundColor,
                                   "About",
                                   Xenorig::Logger::AnsiEscapeCodeConstants::CyanForegroundColor,
                                   PROJECT_NAME,
                                   PROJECT_VERSION,
                                   Xenorig::Logger::AnsiEscapeCodeConstants::DarkGrayForegroundColor,
                                   COMPILER_ID,
                                   COMPILER_VERSION,
                                   Xenorig::Logger::AnsiEscapeCodeConstants::Reset);

    logger.GetConsole(true)->info(" {}* {}{:<12} {}{}{}",
        Xenorig::Logger::AnsiEscapeCodeConstants::GreenForegroundColor,
        Xenorig::Logger::AnsiEscapeCodeConstants::WhiteForegroundColor,
        "CPU",
        Xenorig::Logger::AnsiEscapeCodeConstants::DarkGrayForegroundColor,
        cpuInformation.GetProcessorName(),
        Xenorig::Logger::AnsiEscapeCodeConstants::Reset);

    std::cin.get();

    return 0;
}