#ifndef XENORIG_LOGGER_ANSIESCAPECODECONSTANTS_H
#define XENORIG_LOGGER_ANSIESCAPECODECONSTANTS_H

#include <string>

namespace Xenorig::Logger::AnsiEscapeCodeConstants
{
    const std::string BlackForegroundColor = "\u001b[30m";
    const std::string BlackBackgroundColor = "\u001b[40m";

    const std::string DarkRedForegroundColor = "\u001b[31m";
    const std::string DarkRedBackgroundColor = "\u001b[41m";

    const std::string DarkGreenForegroundColor = "\u001b[32m";
    const std::string DarkGreenBackgroundColor = "\u001b[42m";

    const std::string DarkYellowForegroundColor = "\u001b[33m";
    const std::string DarkYellowBackgroundColor = "\u001b[43m";

    const std::string DarkBlueForegroundColor = "\u001b[34m";
    const std::string DarkBlueBackgroundColor = "\u001b[44m";

    const std::string DarkMagentaForegroundColor = "\u001b[35m";
    const std::string DarkMagentaBackgroundColor = "\u001b[45m";

    const std::string DarkCyanForegroundColor = "\u001b[36m";
    const std::string DarkCyanBackgroundColor = "\u001b[46m";

    const std::string DarkGrayForegroundColor = "\u001b[37m";
    const std::string DarkGrayBackgroundColor = "\u001b[47m";

    const std::string GrayForegroundColor = "\u001b[90m";
    const std::string GrayBackgroundColor = "\u001b[100m";

    const std::string RedForegroundColor = "\u001b[91m";
    const std::string RedBackgroundColor = "\u001b[101m";

    const std::string GreenForegroundColor = "\u001b[92m";
    const std::string GreenBackgroundColor = "\u001b[102m";

    const std::string YellowForegroundColor = "\u001b[93m";
    const std::string YellowBackgroundColor = "\u001b[103m";

    const std::string BlueForegroundColor = "\u001b[94m";
    const std::string BlueBackgroundColor = "\u001b[104m";

    const std::string MagentaForegroundColor = "\u001b[95m";
    const std::string MagentaBackgroundColor = "\u001b[105m";

    const std::string CyanForegroundColor = "\u001b[96m";
    const std::string CyanBackgroundColor = "\u001b[106m";

    const std::string WhiteForegroundColor = "\u001b[97m";
    const std::string WhiteBackgroundColor = "\u001b[107m";

    const std::string Reset = "\u001b[0m";

    const std::string Bold = "\u001b[1m";
    const std::string Underline = "\u001b[4m";
    const std::string Reverse = "\u001b[7m";
    const std::string NoUnderline = "\u001b[24m";
} // namespace Xenorig::Logger::AnsiEscapeCodeConstants

#endif // XENORIG_LOGGER_ANSIESCAPECODECONSTANTS_H
