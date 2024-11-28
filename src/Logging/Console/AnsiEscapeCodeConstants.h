#ifndef XENORIG_LOGGING_CONSOLE_ANSIESCAPECODECONSTANTS_H
#define XENORIG_LOGGING_CONSOLE_ANSIESCAPECODECONSTANTS_H

#include <string>

namespace Xenorig::Logging::Console
{
    class AnsiEscapeCodeConstants
    {
    public:
        static const std::string BlackForegroundColor;
        static const std::string BlackBackgroundColor;

        static const std::string DarkRedForegroundColor;
        static const std::string DarkRedBackgroundColor;

        static const std::string DarkGreenForegroundColor;
        static const std::string DarkGreenBackgroundColor;

        static const std::string DarkYellowForegroundColor;
        static const std::string DarkYellowBackgroundColor;

        static const std::string DarkBlueForegroundColor;
        static const std::string DarkBlueBackgroundColor;

        static const std::string DarkMagentaForegroundColor;
        static const std::string DarkMagentaBackgroundColor;

        static const std::string DarkCyanForegroundColor;
        static const std::string DarkCyanBackgroundColor;

        static const std::string DarkGrayForegroundColor;
        static const std::string DarkGrayBackgroundColor;

        static const std::string GrayForegroundColor;
        static const std::string GrayBackgroundColor;

        static const std::string RedForegroundColor;
        static const std::string RedBackgroundColor;

        static const std::string GreenForegroundColor;
        static const std::string GreenBackgroundColor;

        static const std::string YellowForegroundColor;
        static const std::string YellowBackgroundColor;

        static const std::string BlueForegroundColor;
        static const std::string BlueBackgroundColor;

        static const std::string MagentaForegroundColor;
        static const std::string MagentaBackgroundColor;

        static const std::string CyanForegroundColor;
        static const std::string CyanBackgroundColor;

        static const std::string WhiteForegroundColor;
        static const std::string WhiteBackgroundColor;

        static const std::string Reset;

        static const std::string Bold;
        static const std::string Underline;
        static const std::string Reverse;
        static const std::string NoUnderline;
    };
} // namespace Xenorig::Logging::Console

#endif // XENORIG_LOGGING_CONSOLE_ANSIESCAPECODECONSTANTS_H
