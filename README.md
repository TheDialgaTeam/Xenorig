# Xirorig
***This miner is inspired by [XMRig](https://github.com/xmrig/xmrig) and thus the similarity in UI design. The codes are 100% original.***

[![Build status](https://ci.appveyor.com/api/projects/status/bub2nmai1dhy9aah?svg=true)](https://ci.appveyor.com/project/jianmingyong/xirorig)

Xirorig is a high performance Xiropht (XIRO) CPU miner, with official support for Windows, Linux and MacOS. Originally based on [Xiropht-Solo-Miner](https://github.com/XIROPHT/Xiropht-Solo-Miner) with heavy optimizations/rewrites.

- This is the **CPU mining** version. There is no GPU version at the moment.

![Console_Image](https://raw.githubusercontent.com/TheDialgaTeam/Xirorig/new_master/Screenshot.png)

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
- Pool support has dropped.

### CPU mining performance
- Intel i7-4790K @ 4.00GHz - 136 KH/s (1 thread)
- Intel i7-8750H @ 2.22GHz - 250 KH/s (1 thread)

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

## Developers
- The Dialga Team (Yong Jian Ming)
