# Xirorig
***The name is inspired by [XMRig](https://github.com/xmrig/xmrig) and thus the similarity in design. The codes are 100% original.***

[![Build status](https://ci.appveyor.com/api/projects/status/bub2nmai1dhy9aah?svg=true)](https://ci.appveyor.com/project/jianmingyong/xirorig)

Xirorig is a high performance Xiropht (XIRO) CPU pool miner, with official support for Windows, Linux and MacOS. Originally based on [Xiropht-Miner](https://github.com/XIROPHT/Xiropht-Miner) with heavy optimizations/rewrites.

- This is the **CPU pool mining** version. There is no solo or GPU version at the moment.

![Console_Image](https://i.imgur.com/6TckOMz.png)

#### Table of contents
- [Features](#Features)
- [Downloads](#Downloads)
- [Other information](#Other-information)
- [Donations](#Donations)
- [Developers](#Developers)

## Features
- High performance.
- Official Windows support, Linux and MacOS. (Includes Raspberry PI as well)
- x86/x64/arm support.
- Support for backup (failover) mining server.
- It's open source software.

## Downloads
- Binary releases: https://github.com/TheDialgaTeam/Xirorig/releases

## Other information
- Default donation 5% (5 minutes in 100 minutes) can be reduced to 1% via option donate-level/DonateLevel.

### CPU mining performance
- Intel i7-8750H - 90 KH/s (6 threads)

Please note performance is highly dependent on system load. The numbers above are obtained on an idle system. Tasks heavily using a processor, such as video playback, can greatly degrade hashrate. Optimal number of threads depends on the number of cores you have on your cpu.

### Maximum performance checklist
- Idle operating system.
- Do not exceed optimal thread count.
- Use modern CPUs with AES-NI instruction set.
- Try setup optimal cpu affinity.

## Donations
- BTC: `3Dc5jpiyuts136YhamcRbAeue7mi44gW8d`
- LTC: `LUU9Avuanafmq1vMp53AWS1mr3GCCc2X42`
- XMR: `42oj7eV68BK8Z8wcGzLMFEJgAQG22Z3ajGdtpmJx5p7iDqEgG91wNybWbwaVe4vUMveKAzAiA4j8xgUi29TpKXpm3zwfwWN`
- Xiropht: `brjxVl3Ge1Sv60vXFDarqqQds5cxiS4Bec2p4Zld1v1yvslgY7caGebIF83mSb`

## Developers
- The Dialga Team (Yong Jian Ming)
