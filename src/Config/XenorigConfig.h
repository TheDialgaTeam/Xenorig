#ifndef XENORIGCONFIG_H
#define XENORIGCONFIG_H

#include <cstdint>
#include <string>

namespace Config
{
    struct XenorigConfig
    {
        int32_t PrintTime = 10;
        int32_t MaxPingTime = 1000;
        int32_t MaxRetryCount = 10;
        int32_t DonatePercentage = 0;

        std::string Url;
        std::string Username;
        std::string Password;
        std::string UserAgent;
        bool SoloMining;

        int32_t Threads;
        uint64_t ThreadAffinity[];
    };

} // namespace Config

#endif // XENORIGCONFIG_H
