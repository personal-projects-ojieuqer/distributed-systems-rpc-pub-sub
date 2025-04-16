# 🐠 Distributed Sensor System - WAVY (SD 2024/2025)

Practical Assignment 1 for the course unit **Distributed Systems** (Computer Engineering - IPVC/ESTG).  
This project simulates a **distributed sensor ecosystem** — composed of WAVY sensors, aggregators, and a central server — for real-time data collection and consolidation.

## 📌 Authors

- **Rui Requeijo** - al79138  
- **João Mendes** - al79229  
- **Matilde Coelho** - al79908  

---

## 🧠 Goals

- Apply fundamental distributed systems concepts:
  - Communication using **TCP Sockets**
  - **Concurrent execution (Multithreading)**
  - **Synchronization using Mutex**
  - Logical separation of concerns (aggregators, sensors, and server)

- Simulate a realistic system with:
  - **Local database per aggregator (MySQL)**
  - A **central server** that consolidates data
  - **Device authorization** using tokens and permission lists
  - **Interactive console-based sensor management**

---
## 🧠 Architecture Map

![image](https://github.com/user-attachments/assets/d79849b4-69e6-4943-836f-e8b551d5e08b)

---

- **WAVY**: simulates sensors (temperature, accelerometer, gyroscope, hydrophones)
- **Aggregators**: receive data from WAVIES, store it locally, and synchronize with the central server
- **Central Server**: collects and centralizes data from all aggregators

---

## 📂 Project Structure

```bash
├── agregators/             # Aggregator logic
│   ├── AggregatorHandler.cs
│   ├── AggregatorSender.cs
│   └── Program.cs
├── server/                 # Central server
│   └── Program.cs
├── Wavy/                   # Simulated WAVY sensors
│   ├── WavyManager.cs
│   ├── WavyRunner.cs
│   ├── WavySecondaryFunctions.cs
│   ├── ProjectExplanation.cs
│   └── Program.cs
├── docker-compose.yml      # Docker infrastructure
├── README.md               # This file
```

### 🔧 Requirements

- [.NET 7 or 8](https://dotnet.microsoft.com/)
- [Docker + Docker Compose](https://docs.docker.com/compose/)
- (Optional) MySQL Workbench for data exploration

---

## 🐳 Docker

- Each aggregator has its own **MySQL database**
- The central server collects data from all aggregators
- Includes .env files to configure ports, credentials, and tokens
- Modular and scalable infrastructure

---
