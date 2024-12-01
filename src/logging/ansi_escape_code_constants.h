#ifndef XENORIG_LOGGING_ANSIESCAPECODECONSTANTS_H
#define XENORIG_LOGGING_ANSIESCAPECODECONSTANTS_H

#include <string>

namespace xenorig::logging
{
    class ansi_escape_code_constants
    {
    public:
        static const std::string black_foreground_color;
        static const std::string black_background_color;

        static const std::string dark_red_foreground_color;
        static const std::string dark_red_background_color;

        static const std::string dark_green_foreground_color;
        static const std::string dark_green_background_color;

        static const std::string dark_yellow_foreground_color;
        static const std::string dark_yellow_background_color;

        static const std::string dark_blue_foreground_color;
        static const std::string dark_blue_background_color;

        static const std::string dark_magenta_foreground_color;
        static const std::string dark_magenta_background_color;

        static const std::string dark_cyan_foreground_color;
        static const std::string dark_cyan_background_color;

        static const std::string dark_gray_foreground_color;
        static const std::string dark_gray_background_color;

        static const std::string gray_foreground_color;
        static const std::string gray_background_color;

        static const std::string red_foreground_color;
        static const std::string red_background_color;

        static const std::string green_foreground_color;
        static const std::string green_background_color;

        static const std::string yellow_foreground_color;
        static const std::string yellow_background_color;

        static const std::string blue_foreground_color;
        static const std::string blue_background_color;

        static const std::string magenta_foreground_color;
        static const std::string magenta_background_color;

        static const std::string cyan_foreground_color;
        static const std::string cyan_background_color;

        static const std::string white_foreground_color;
        static const std::string white_background_color;

        static const std::string reset;

        static const std::string bold;
        static const std::string underline;
        static const std::string reverse;
        static const std::string no_underline;
    };
} // namespace xenorig::logging

#endif // XENORIG_LOGGING_ANSIESCAPECODECONSTANTS_H
