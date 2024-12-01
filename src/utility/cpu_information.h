#ifndef XENORIG_UTILITY_CPUINFORMATION_H
#define XENORIG_UTILITY_CPUINFORMATION_H

#include <cstdint>
#include <string>

namespace xenorig::utility::cpu_information
{
    struct data
    {
        std::string processor_name;
        uint32_t l2_cache_size = 0;
        uint32_t l3_cache_size = 0;
        uint32_t processor_core_count = 0;
        uint32_t processor_count = 0;
    };

    data get_cpu_information();
} // namespace xenorig::utility::cpu_information

#endif // XENORIG_UTILITY_CPUINFORMATION_H
