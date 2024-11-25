#include "Console.h"

#ifdef _WIN32
    #include <Windows.h>
#endif

namespace Xenorig::Utility::Console
{
    void SetTitle(const std::string &title)
    {
#ifdef _WIN32
        SetConsoleTitle(title.c_str());
#elif defined(__linux__) || defined(__APPLE__)
        std::cout << "\033]0;" << title << "\007"; // ANSI escape sequence
#endif
    }
} // namespace Xenorig::Utility::Console
