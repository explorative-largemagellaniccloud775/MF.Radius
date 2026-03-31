# MF.Radius 🛰️
**English** | [🇷🇺 Русский язык](./README_ru.md) 

**MF.Radius** is a high-performance RADIUS protocol framework...

**MF.Radius** is a high-performance framework for implementing the RADIUS protocol on the **.NET 10** platform.
Designed to operate under extreme loads (AAA traffic) with a strong focus on **Zero-Allocation** architecture and **Clean Architecture** principles.

The repository contains two projects:
- **MF.Radius.Core** - the core of the framework, containing high-level abstractions and low-level protocol logic;
- **MF.Radius.SampleServer** - an example of a RADIUS server implementation, demonstrating the best practices of using the library.

## 🚀 What does the project do?
**MF.Radius** provides the tools to create your own RADIUS servers.
The library handles all the heavy lifting of parsing, validating, and building packets:
- **Authentication**: full support for `Access-Request`, `Accept` and `Reject` cycles;
- **Accounting**: processing `Accounting-Request` requests and `Response` acknowledgments;
- **Session Management**: support for dynamic `CoA-Request` and `Disconnect-Request` (DM) requests;
- **Cryptography**: built-in implementation of `PAP`, `CHAP` and `MS-CHAPv2` algorithms.

## ✨ Why is the project useful?
In modern network solutions, every microsecond and every Garbage Collector (GC) pause directly impacts system throughput.
- **Performance**: critical data processing paths (`hot paths`) are optimized using `Span<T>`, `Memory<T>`, `ArrayPool` and `MemoryPool`, which minimizes heap allocations and reduces CPU load;
- **Flexibility and Extensibility**: the architecture allows for easy addition of Vendor-Specific Attributes (VSA) for Cisco, MikroTik and any other hardware vendors;
- **Clean Architecture**: the Sample Server demonstrates how seamlessly the library integrates into projects built on **SOLID**, **DDD** and **CQRS** principles;
- **Modern Ecosystem**: you are not limited in your choice of infrastructure, use any databases, caches, message brokers, and monitoring tools (OpenTelemetry);
- **Ready-made Template**: the Sample Server can be used as a boilerplate for your Enterprise solution, saving you time on designing the basic structure.

## 🏁 How to get started?

### 1. Dependency Installation
The package is available on [NuGet](https://www.nuget.org/packages/MF.Radius.Core). To install, run the following command:
```bash
dotnet add package MF.Radius.Core
```

### 2. Choosing an Integration Approach
You can integrate the library into your project at different levels of abstraction:
- **Using the Template**: base your project on [MF.Radius.SampleServer](./src/MF.Radius.SampleServer). All business logic in it is centralized in the `IspRadiusProcessor` class. This is the fastest way to build a typical server;
- **Inheritance**: create your own processor by inheriting from `RadiusProcessorBase`, this gives you a ready-made wrapper and lets you focus purely on packet processing logic;
- **Full Control**: implement the `IRadiusProcessor` interface yourself if you need custom behavior at a low level.

### 3. Running and Configuration
After configuring DI and registering your processor, the server will automatically start listening for incoming UDP requests.
Ports and network parameters are specified in the standard `appsettings.json` configuration file:
```json
  "RadiusListener": {
    "BindAddress": "0.0.0.0",
    "Ports": [1812, 1813],
    "InboundQueueSize": 1000,
    "ConcurrentWorkers": 2
  },
  "RadiusSender": {
    "BindAddress": "0.0.0.0",
    "Port": 3799,
    "DefaultTimeout": "00:00:03"
  },
  "RadiusIsp": {
    "NasTimeout": "00:00:05",
    "FramedMtu": 1492,
    "SessionTimeout": 86400,
    "IdleTimeout": 3600,
    "AcctInterimInterval": 300,
    "ReplyMessage": "Welcome to the MoRFaiR Network!"
  },
  "DemoSecurity": {
    "SharedSecret": "testing123"
  }
```

In the directory [src/MF.Radius.SampleServer](src/MF.Radius.SampleServer) you will find an example of a full-fledged server with REST API integration, logs and metrics.

## 🆘 Where to get help?

If you encounter a problem, have a question, or have an idea to improve the library — I will be glad to hear your feedback!
- [Issues](./issues): use this for bug reports or feature requests;
- [Discussions](./discussions): come here if you want to discuss architecture or ask "how do I do X?".
- [Telegram](https://t.me/morfair_ru): contact me directly.

## 🤝 Contributing and Support

The project was created and is maintained by Mikhail Kalinin ([morfair](https://github.com/morfair)).

I am always open to your Pull Requests!
If you want to help make the project better, please follow this simple workflow:
1. **Fork**: fork the repository to your account;
2. **Branch**: create a branch for your feature or fix: `git checkout -b feature/amazing-feature`;
3. **Code & Test**: implement your changes, ensure the project builds successfully (`dotnet build`), and all tests pass;
4. **PR**: submit a Pull Request to the main branch.

### 📝 Important rules for contributors
- **Language**: please use **English** for code comments and documentation;
- **Performance**: stick to the **zero-allocation** approach in the protocol logic. Use `Span<T>`, `Memory<T>`, and array and memory pools;
- **Standards**: follow the existing coding style and architectural patterns of the project.

