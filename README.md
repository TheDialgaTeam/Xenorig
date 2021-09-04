# Xirorig
***This miner is inspired by [XMRig](https://github.com/xmrig/xmrig) and thus the similarity in UI design. The codes are 100% original.***

[![Build status](https://ci.appveyor.com/api/projects/status/bub2nmai1dhy9aah?svg=true)](https://ci.appveyor.com/project/jianmingyong/xirorig)

Xirorig is a high performance Xiropht (XIRO) CPU miner, with official support for Windows, Linux and MacOS. Originally based on [SeguraChain-Solo-Miner](https://github.com/SamSegura/segurachain) with heavy optimizations/rewrites.

- This is the **CPU mining** version. There is no GPU version at the moment.

![Console_Image](https://raw.githubusercontent.com/TheDialgaTeam/Xirorig/xirorig_future/Screenshot.png)

#### Table of contents
- [Features](#Features)
- [Downloads](#Downloads)
- [Other information](#Other-information)
- [Donations](#Donations)
- [Developers](#Developers)

## Features
- High performance.
- Official Windows support, Linux and MacOS. (Includes Raspberry PI as well)
- x86/x64/arm/arm64 support.
- Support for backup (failover) mining server.
- It's open source software.

## Downloads
- Binary releases: https://github.com/TheDialgaTeam/Xirorig/releases

## Other information
- Donation is optional.

### CPU mining performance
- Intel i7-4790K @ 4.00GHz - NA KH/s (1 thread)
- Intel i7-8750H @ 2.22GHz - 9.5 KH/s (1 thread)

Please note performance is highly dependent on system load. The numbers above are obtained on an idle system. Tasks heavily using a processor, such as video playback, can greatly degrade hashrate. Optimal number of threads depends on the number of cores you have on your cpu.

### Maximum performance checklist
- Idle operating system.
- Do not exceed optimal thread count.
- Try setup optimal cpu affinity.
- Use modern hardware that supports SIMD instructions.
- Use [libxirorig_native](https://github.com/TheDialgaTeam/Xirorig/tree/xirorig_future/Xirorig.Native/xirorig_native) library. Only Windows (x64, x86) and Linux (x64) is precompiled. You can set up your own custom toolchain to build the required library.

## Donations
- BTC: `3Dc5jpiyuts136YhamcRbAeue7mi44gW8d`
- LTC: `LUU9Avuanafmq1vMp53AWS1mr3GCCc2X42`
- XMR: `42oj7eV68BK8Z8wcGzLMFEJgAQG22Z3ajGdtpmJx5p7iDqEgG91wNybWbwaVe4vUMveKAzAiA4j8xgUi29TpKXpm3zwfwWN`

## Developers
- The Dialga Team (Yong Jian Ming)
