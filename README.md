# 🐠 Distributed Sensor System - TP2 SD (2024/2025)

Practical Assignment 2 for the **Distributed Systems** course (Computer Engineering - IPVC/ESTG).  
This project simulates a modular and secure distributed sensor system, composed of WAVY devices, intermediate aggregators, a central server for data processing, and auxiliary forecasting services.

---

## 👨‍💻 Authors

- **Rui Requeijo** - al79138  
- **João Mendes** - al79229  
- **Matilde Coelho** - al79908  

---

## 🎯 Objectives

- Apply distributed systems principles in practice:
  - Communication using **TCP Sockets** and **RabbitMQ (Pub/Sub)**
  - **Concurrency** using threads and `SemaphoreSlim`
  - **Hybrid encryption** (RSA + AES)
  - Distributed architecture using gRPC and RESTful services
- Additional value:
  - **Sensor data validation (preprocessing)**
  - **Forecasting of sensor data (HPC)**
  - **Interactive web interface for analysis**

---

## 🗺️ System Architecture

![image](https://github.com/user-attachments/assets/9a606d13-52e4-4439-a562-bc31316ea6a8)


## 📂 Project Structure

```bash
├── Wavy/                     # Simuladores de sensores WAVY
├── agregators/              # Agregadores com RabbitMQ + gRPC
├── server/                  # Validação + armazenamento central + chamada HPC
├── preprocessrpc/           # Serviço gRPC de pré-processamento
├── hpc/                     # Serviço gRPC de previsão (modelo A e B)
├── SDVisualizer.API/        # API RESTful (endpoints de consulta)
├── HPCVisualizerFE/         # Interface web em ASP.NET MVC
├── docker-compose.yml       # Infraestrutura Docker
└── README.md
```

## 🔐 Security
Hybrid encryption:

AES key generated per session by aggregator

Encrypted using RSA (server’s public key)

Handshake authentication using pre-shared tokens (from environment variables)

Authorization list: autorizacoes/AGG_XX.txt defines allowed WAVY devices per aggregator


## 🔧 Technologies Used
.NET 8 (C#)

RabbitMQ (message broker)

MySQL (distributed and central databases)

gRPC (structured services)

Docker & Docker Compose

Chart.js + ASP.NET MVC (frontend visualizations)
---

## 🐳 Docker Compose
All components are containerized using Docker:

rabbitmq: message broker (5672/15672)

mysql_*: MySQL databases for each aggregator and central server

agregator_app_X: listens to topics, processes data and sends to server

server_app: centralizes and forwards data to the HPC component

preprocessrpc: gRPC service for preprocessing sensor data

hpc: gRPC service with statistical forecasting models

SDVisualizer.API and HPCVisualizerFE: API and web UI for visualization

Environment variables define ports, tokens, database credentials, and routing.
Persistent volumes are used for database storage and config files.
---

## 📈 Visualization Interface
View sensors by type or WAVY device

Display actual + forecasted values over time

Toggle light/dark mode, download charts, identify risky trends

Data served via a REST API documented with Swagger
