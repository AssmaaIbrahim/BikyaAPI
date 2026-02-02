# Bikya Frontend - E-commerce Platform

## 📋 Project Overview

**Bikya** is a web-based marketplace platform that enables users to buy and sell products through a secure, role-based system.

This project was developed as the **ITI Final Project** using **ASP.NET Core Web API** and **Angular**, with a focus on clean architecture, RESTful APIs, and frontend–backend integration.

## Demo
- Demo Video: https://drive.google.com/drive/folders/1ZeuULby__rndtX7PD2SNkJ3pEjobQZiP?usp=sharing

---

## Related Repositories
-  Frontend (Angular): https://github.com/AssmaaIbrahim/BikyaFrontend.git


## Tech Stack

### Backend
- ASP.NET Core Web API (.NET 7 / 8)
- Entity Framework Core
- SQL Server
- JWT Authentication
- Repository Pattern
- Swagger (OpenAPI)

### Frontend
- Angular (v16+)
- TypeScript
- Angular Material / Bootstrap

### Tools
- Git & GitHub
- Postman
- Swagger UI

---

## Architecture

### Backend Architecture
- API Layer (Controllers)
- Application Layer (Services, DTOs)
- Domain Layer (Entities, Interfaces)
- Infrastructure Layer (EF Core, Repositories)

### Frontend Architecture
- Core (Guards, Interceptors)
- Shared (Reusable Components)
- Features (Auth, Products, Orders)

---


## 🚀 Features

### 🔐 Authentication & User Management
- User registration and login
- Email verification
- Password reset functionality
- Role-based access control (User, Admin)
- Profile management

### 🛍️ Product Management
- Product listing with categories
- Product details with images
- Add/Edit/Delete products (for sellers)
- Product search and filtering
- Product condition badges (New/Used)
- Exchange functionality

### 💳 Payment System
- Stripe payment integration
- Secure payment processing
- Payment history tracking
- Webhook handling for payment status updates
- Multiple payment methods support

### 📦 Order Management
- Order creation and tracking
- Order status updates (Pending, Paid, Shipped, Completed, Cancelled)
- Real-time order status synchronization
- Admin order management dashboard
- Order history for users

### 🚚 Shipping & Delivery
- Shipping cost calculation
- Shipping tracking
- Admin shipping management
- Multiple shipping options

### ⭐ Reviews & Ratings
- Product reviews and ratings
- Review management system
- User feedback system

### 💰 Wallet System
- User wallet management
- Transaction history
- Balance tracking
- Payment processing

### 👨‍💼 Admin Dashboard
- User management
- Product management
- Order management with real-time updates
- Category management
- Shipping management
- Payment monitoring
- Auto-refresh functionality (30 seconds)


## 📁 Project Structure

```
BikyaFrontend/
├── src/
│   ├── app/
│   │   ├── core/
│   │   │   ├── guards/           # Route guards
│   │   │   ├── interceptors/     # HTTP interceptors
│   │   │   ├── models/          # Data models
│   │   │   └── services/        # Core services
│   │   ├── features/
│   │   │   ├── admin/           # Admin features
│   │   │   ├── auth/            # Authentication
│   │   │   ├── orders/          # Order management
│   │   │   ├── payment/         # Payment processing
│   │   │   ├── products/        # Product management
│   │   │   ├── profile/         # User profile
│   │   │   ├── review/          # Reviews system
│   │   │   ├── shipping/        # Shipping management
│   │   │   └── wallet/          # Wallet system
│   │   └── shared/
│   │       └── components/      # Shared components
│   ├── environments/            # Environment configs
│   └── assets/                  # Static assets
├── angular.json                 # Angular config
├── package.json                 # Dependencies
└── README.md                    # This file
```






