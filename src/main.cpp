#include "about.h"
#include "logging/logger.h"
#include "utility/console.h"

int main(int argv, char *argc[])
{
    xenorig::logging::configure_logging();
    xenorig::utility::console::set_console_title(PROJECT_NAME);

    xenorig::logging::print_about();
    xenorig::logging::print_cpu();

    return 0;
}
