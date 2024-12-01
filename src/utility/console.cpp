#include "console.h"

#ifdef _WIN32
    #include <Windows.h>
#elif defined(__linux__) || defined(__APPLE__)
    #include <iostream>
#endif

namespace xenorig::utility::console
{
    void set_console_title(const std::string &title)
    {
#ifdef _WIN32
        SetConsoleTitle(title.c_str());
#elif defined(__linux__) || defined(__APPLE__)
        std::cout << "\033]0;" << title << "\007"; // ANSI escape sequence
#endif
    }
} // namespace xenorig::utility::console
