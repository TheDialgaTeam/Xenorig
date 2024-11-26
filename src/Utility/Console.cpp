#include "Console.h"

#ifdef _WIN32
    #include <Windows.h>
    #pragma push_macro("SetConsoleTitle")
    #undef SetConsoleTitle
#endif

namespace Xenorig::Utility::Console
{
    void SetConsoleTitle(const std::string &title)
    {
#ifdef _WIN32
    #pragma pop_macro("SetConsoleTitle")
        SetConsoleTitle(title.c_str());
#elif defined(__linux__) || defined(__APPLE__)
        std::cout << "\033]0;" << title << "\007"; // ANSI escape sequence
#endif
    }
} // namespace Xenorig::Utility::Console
