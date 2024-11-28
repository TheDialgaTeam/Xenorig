#include "About.h"
#include "Logging/Logger.h"
#include "Utility/Console.h"

int main(int argv, char *argc[])
{
    Xenorig::Logging::ConfigureLogging();
    Xenorig::Utility::Console::SetTitle(PROJECT_NAME);

    Xenorig::Logging::PrintAbout();
    Xenorig::Logging::PrintCpu();

    return 0;
}
