# 🛒 DesiCorner

A production-grade e-commerce platform built with **microservices architecture**, showcasing modern full-stack development practices with Angular, .NET Core, and cloud-native technologies.

![.NET Core](https://img.shields.io/badge/.NET%20Core-8.0-512BD4?style=flat&logo=dotnet)
![Angular](https://img.shields.io/badge/Angular-18-DD0031?style=flat&logo=angular)
![Redis](https://img.shields.io/badge/Redis-Cache-DC382D?style=flat&logo=redis)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2019-CC2927?style=flat&logo=microsoftsqlserver)
![Docker](https://img.shields.io/badge/Docker-Containerized-2496ED?style=flat&logo=docker)

---

## 📋 Overview

DesiCorner is a personal project designed to demonstrate expertise in building **scalable, secure, and maintainable** distributed systems. The platform implements industry best practices including Domain-Driven Design (DDD), Clean Architecture, and Test-Driven Development (TDD).

### Why This Project?

- 🎯 **Hands-on learning** with microservices patterns and distributed systems
- 🏗️ **Architecture showcase** demonstrating real-world design decisions
- 🔧 **Technology exploration** with modern .NET and Angular ecosystems
- 📚 **Best practices implementation** for enterprise-grade applications

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                          CLIENT LAYER                                │
│                    Angular 18 SPA Application                        │
└─────────────────────────────────┬───────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         API GATEWAY                                  │
│                    YARP Reverse Proxy                                │
│         (Routing, Load Balancing, Rate Limiting)                     │
└─────────────────────────────────┬───────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                       MICROSERVICES LAYER                            │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │
│  │  Identity   │  │   Catalog   │  │   Orders    │  │   Payment   │ │
│  │   Service   │  │   Service   │  │   Service   │  │   Service   │ │
│  │ (OpenIddict)│  │             │  │             │  │  (Stripe)   │ │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘ │
└─────────────────────────────────┬───────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         DATA LAYER                                   │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────────┐  │
│  │ SQL Server  │  │    Redis    │  │      Message Queue          │  │
│  │  Databases  │  │    Cache    │  │   (Future: Kafka/RabbitMQ)  │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 🛠️ Technology Stack

### Backend
| Technology | Purpose |
|------------|---------|
| **.NET Core 8** | Microservices APIs |
| **ASP.NET Core** | Web API framework |
| **Entity Framework Core** | ORM & data access |
| **YARP** | API Gateway / Reverse Proxy |
| **OpenIddict** | OAuth 2.0 / OpenID Connect authentication |
| **SQL Server** | Relational database |
| **Redis** | Distributed caching & session management |
| **NUnit** | Unit testing framework | (To be added)

### Frontend
| Technology | Purpose |
|------------|---------|
| **Angular 18** | Single Page Application |
| **TypeScript** | Type-safe JavaScript |
| **RxJS** | Reactive programming |
| **Angular Material** | UI components |
| **Bootstrap** | Responsive styling |

### DevOps & Infrastructure (To be added)
| Technology | Purpose |
|------------|---------|
| **Docker** | Containerization |
| **Docker Compose** | Multi-container orchestration |
| **Git** | Version control |

### Integrations
| Technology | Purpose |
|------------|---------|
| **Stripe** | Payment processing |

---

## ✨ Key Features

### 🔐 Authentication & Security
- OAuth 2.0 / OpenID Connect implementation with OpenIddict
- JWT token-based authentication
- Role-based access control (RBAC)
- Secure password hashing
- Refresh token rotation

### 🛍️ E-Commerce Functionality
- Product catalog with categories and search
- Shopping cart management
- Order processing workflow
- Secure checkout with Stripe integration
- Order history and tracking

### ⚡ Performance & Scalability
- Redis caching for frequently accessed data
- API Gateway for request routing and load balancing
- Rate limiting to prevent abuse
- Optimized database queries with EF Core
- Async/await patterns throughout

### 🏛️ Architecture & Design Patterns
- **Microservices Architecture** - Independent, deployable services
- **Domain-Driven Design (DDD)** - Rich domain models
- **Clean Architecture** - Separation of concerns
- **CQRS Pattern** - Command Query Responsibility Segregation
- **Repository Pattern** - Abstracted data access
- **Unit of Work** - Transaction management


## 📊 API Documentation

Each microservice exposes RESTful APIs with Swagger/OpenAPI documentation.

| Service | Swagger URL |
|---------|-------------|
| API Gateway | `http://localhost:5000/swagger` |
| AuthServer | `http://localhost:7001/swagger` |
| Products | `http://localhost:7101/swagger` |
| Cart | `http://localhost:7301/swagger` |
| Orders | `http://localhost:7401/swagger` |
| Payment | `http://localhost:7501/swagger` |

---

## 🗺️ Roadmap

- [x] Project setup and architecture design
- [x] API Gateway with YARP
- [x] Identity service with OpenIddict
- [x] Catalog service
- [x] Redis caching integration
- [x] Angular frontend foundation
- [x] Stripe payment integration
- [x] Order service completion
- [x] Shopping cart functionality
- [x] Email notifications
- [ ] Admin Dashboard
- [ ] Real-time
- [ ] Kubernetes deployment manifests
- [ ] CI/CD pipeline with GitHub Actions
- [ ] Message queue integration (Kafka/RabbitMQ)

---

## 🎓 Learning Objectives

This project serves as a practical exploration of:

1. **Microservices Patterns** - Service decomposition, API Gateway, inter-service communication
2. **Security** - OAuth 2.0 flows, JWT tokens, secure coding practices
3. **Performance** - Caching strategies, async programming, database optimization
4. **Clean Code** - SOLID principles, design patterns, testable architecture
5. **DevOps** - Containerization, orchestration, infrastructure as code

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 👤 Author

**Hari Venkata Krishna Kotha**

- GitHub: [@HariVenkataKrishnaKotha](https://github.com/HariVenkataKrishnaKotha)
- LinkedIn: [harivenkatakrishnakotha](https://www.linkedin.com/in/harivenkatakrishnakotha)
- Email: harivenkatakrishnak@gmail.com

---

## 🙏 Acknowledgments

- [Microsoft Documentation](https://docs.microsoft.com/) - .NET and Azure resources
- [Angular Documentation](https://angular.io/docs) - Frontend framework guides
- [OpenIddict Documentation](https://documentation.openiddict.com/) - Authentication implementation
- [YARP Documentation](https://microsoft.github.io/reverse-proxy/) - API Gateway patterns

---

<p align="center">
  <i>⭐ If you find this project helpful, please consider giving it a star!</i>
</p>