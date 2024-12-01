#ifndef XENORIG_CONFIGURATION_XENORIGOPTIONS_H
#define XENORIG_CONFIGURATION_XENORIGOPTIONS_H

#include <cstdint>
#include <string>
#include <vector>

namespace xenorig::Configuration
{
    struct XenorigOptions
    {
        struct CpuMiner
        {
            int32_t Threads;
        };

        struct Pool
        {
            std::string Algorithm;
            std::string Coin;
            std::string Url;
            std::string Username;
            std::string Password;
            std::string UserAgent;
            bool SoloMining = false;
        };

        int32_t PrintTime = 10;
        int32_t MaxPingTime = 1000;
        int32_t MaxRetryCount = 5;
        int32_t DonatePercentage = 0;

        std::vector<Pool> Pool;
    };

} // namespace Xenorig::Configuration

#endif // XENORIG_CONFIGURATION_XENORIGOPTIONS_H
