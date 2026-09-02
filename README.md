# 🛒 InventoryZero - Liquidation Marketplace Prototype

> **⚠️ PROTOTYPE NOTICE:** This is a demonstration/prototype application created to showcase the concept of a liquidation and excess inventory marketplace. It is not a production-ready application and is intended for portfolio and demonstration purposes only.

## 📋 Overview

InventoryZero is a full-stack web application prototype that connects businesses with excess inventory to buyers looking for discounted products. The platform enables sellers to list liquidation items, manage their shops, and process orders, while buyers can browse, save, and purchase discounted products.(please note that the website it self only has an admin no users/sellers/products or any important data to test it , so to test it please fell free to register as a buyer , and the site will give you the option to become a seller and add products remove products and place an order as a user , every detail you may need is indicated below take you time , full testing for the product can take atleast 15 minutes)

### 🎯 Project Purpose

This prototype demonstrates:
- **Marketplace Concept**: A platform for buying and selling excess/liquidation inventory
- **Technical Implementation**: Full-stack development with .NET 8, PostgreSQL, and vanilla JavaScript
- **UI/UX Design**: Clean, modern interface with role-based dashboards
- **E-commerce Features**: Product listing, saving, ordering, and payment flow simulation

## 🚀 Live Demo

**URL:** [https://inventoryzero-production.up.railway.app](https://inventoryzero-production.up.railway.app)

> **Note:** This is hosted on Railway's free tier, so the first request may take a few seconds to wake up the service.

## 🔐 Admin Login Credentials
📧 Email: admin@inventoryzero.com
🔑 Password: Admin@123

text

> Feel free to use these credentials to explore the admin dashboard and all platform features. You can also register as a new user to experience the buyer and seller workflows.

## ✨ Key Features

### For Buyers
- Browse products by category, price, and location
- Save favorite products to wishlist
- View product details with image gallery
- Place orders (simulated payment flow)
- Track order status
- Rate sellers after purchase

### For Sellers
- Create and manage shop profiles
- List products with images and pricing
- Update order statuses (Pending → Shipped → Delivered)
- View sales analytics and payouts
- Manage product inventory

### For Admins
- Approve/reject new shop registrations
- Manage users (view, deactivate, change roles)
- View platform analytics (users, orders, revenue)
- Process pending payouts
- Moderate products

## 🛠️ Technology Stack

### Backend
- **Framework**: .NET 8 (ASP.NET Core Web API)
- **Database**: PostgreSQL (Neon)
- **ORM**: Entity Framework Core
- **Authentication**: JWT (JSON Web Tokens)
- **Documentation**: Swagger/OpenAPI

### Frontend
- **Languages**: HTML5, CSS3, JavaScript (Vanilla)
- **Styling**: Custom CSS with modern design
- **Icons**: Tabler Icons

### Deployment
- **Hosting**: Railway
- **Containerization**: Docker
- **Database**: Neon PostgreSQL (Cloud)

## 📁 Project Structure
InventoryZeroAPI/
├── Controllers/ # API endpoints
│ ├── AdminController.cs
│ ├── AuthController.cs
│ ├── CategoriesController.cs
│ ├── OrdersController.cs
│ ├── PaymentsController.cs
│ ├── ProductsController.cs
│ ├── SavedProductsController.cs
│ ├── SellerController.cs
│ ├── ShopsController.cs
│ └── UserController.cs
├── Services/ # Business logic
│ ├── AdminService.cs
│ ├── AuthService.cs
│ ├── CategoryService.cs
│ ├── IAdminService.cs
│ ├── IAuthService.cs
│ ├── ICategoryService.cs
│ ├── IOrderService.cs
│ ├── IProductService.cs
│ ├── ISavedProductService.cs
│ ├── ISellerService.cs
│ ├── IShopService.cs
│ ├── IUserService.cs
│ ├── OrderService.cs
│ ├── ProductService.cs
│ ├── SavedProductService.cs
│ ├── SellerService.cs
│ ├── ShopService.cs
│ └── UserService.cs
├── Models/ # Entity models
│ ├── ActivityLog.cs
│ ├── Category.cs
│ ├── Dispute.cs
│ ├── Notification.cs
│ ├── Order.cs
│ ├── OrderItem.cs
│ ├── Payout.cs
│ ├── Product.cs
│ ├── ProductImage.cs
│ ├── Review.cs
│ ├── SavedProduct.cs
│ ├── Shop.cs
│ ├── User.cs
│ └── UserAddress.cs
├── DTOs/ # Data transfer objects
│ ├── Admin/
│ ├── Auth/
│ ├── Categories/
│ ├── Orders/
│ ├── Products/
│ ├── SavedProducts/
│ ├── Seller/
│ ├── Shops/
│ └── User/
├── Data/ # DbContext and migrations
│ └── InventoryZeroDbContext.cs
├── wwwroot/ # Frontend files
│ ├── css/
│ │ ├── admin-dashboard.css
│ │ ├── auth.css
│ │ ├── browse.css
│ │ ├── buyer-dashboard.css
│ │ ├── checkout.css
│ │ ├── global.css
│ │ ├── home.css
│ │ ├── order-detail.css
│ │ ├── payment.css
│ │ ├── seller-dashboard.css
│ │ └── shop-profile.css
│ ├── js/
│ │ ├── admin-dashboard.js
│ │ ├── auth.js
│ │ ├── browse.js
│ │ ├── buyer-dashboard.js
│ │ ├── checkout.js
│ │ ├── home.js
│ │ ├── order-detail.js
│ │ ├── payment.js
│ │ ├── seller-dashboard.js
│ │ └── shop-profile.js
│ ├── pages/
│ │ ├── admin-dashboard.html
│ │ ├── browse.html
│ │ ├── buyer-dashboard.html
│ │ ├── checkout.html
│ │ ├── create-shop.html
│ │ ├── index.html
│ │ ├── login.html
│ │ ├── order-detail.html
│ │ ├── order-success.html
│ │ ├── payment.html
│ │ ├── register.html
│ │ ├── seller-dashboard.html
│ │ └── shop-profile.html
│ └── uploads/
│ └── products/ # Product images
├── Properties/
│ └── launchSettings.json
├── Program.cs # Application entry point
├── appsettings.json # Configuration
├── Dockerfile # Docker configuration
├── .dockerignore # Docker ignore file
└── InventoryZeroAPI.csproj

text

## 🏗️ Architecture Highlights

### Clean Service Layer
Each service follows a single responsibility principle with clear separation of concerns:
- `AuthService` - Authentication and authorization
- `ProductService` - Product management and searching
- `OrderService` - Order processing and tracking
- `SellerService` - Shop and product management for sellers
- `AdminService` - Admin-only operations
- `UserService` - User profile and address management
- `CategoryService` - Category management
- `SavedProductService` - Wishlist functionality
- `ShopService` - Shop browsing and details

### Database Design
The database schema is designed for an e-commerce marketplace with 15 tables:
- **Users**: Role-based access (Buyer, Seller, Admin) with profile information
- **Shops**: Business profiles with verification status and commission rates
- **Products**: Inventory items with pricing, condition, images, and listing dates
- **Orders**: Transaction records with status tracking and shipping details
- **OrderItems**: Individual items within orders
- **SavedProducts**: User wishlist functionality
- **Reviews**: Rating and review system for products and sellers
- **Categories**: Hierarchical product categorization
- **Payouts**: Seller payment processing
- **Disputes**: Order dispute resolution
- **Notifications**: User notifications
- **ActivityLogs**: Admin activity tracking
- **UserAddresses**: Shipping address management
- **ProductImages**: Product image gallery

### Security Features
- JWT authentication with role-based authorization
- Password hashing with BCrypt
- Input validation and sanitization
- CORS configuration for secure API access
- SQL injection protection via Entity Framework
- XSS protection with HTML escaping
- Secure JWT key management using user secrets

### Performance Optimizations
- **AsNoTracking()** for read-only queries to reduce memory overhead
- **AsSplitQuery()** for complex includes to prevent Cartesian explosion
- **Projection to DTOs** to minimize data transferred from database
- **Early returns** for empty results to prevent unnecessary database operations
- **Batch queries** to avoid N+1 query problems
- **Indexes** on frequently queried columns

## 🚀 Local Development Setup

### Prerequisites
- .NET 8 SDK
- PostgreSQL (or use Neon cloud)
- Git

### Clone and Run

```bash
# Clone the repository
git clone https://github.com/Mawande57/InventoryZeroAPI.git
cd InventoryZeroAPI

# Install dependencies
dotnet restore

# Install PostgreSQL package (if using local PostgreSQL)
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

# Update appsettings.json with your connection string
# Or use user secrets for development

# Apply database migrations
dotnet ef database update

# Run the application
dotnet run
Using User Secrets (Recommended for Development)
bash
# Initialize user secrets
dotnet user-secrets init

# Add connection string
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=InventoryZeroDB;Username=your_user;Password=your_password"

# Add JWT key
dotnet user-secrets set "Jwt:Key" "your-secret-key-minimum-32-characters"

# Add JWT issuer and audience
dotnet user-secrets set "Jwt:Issuer" "InventoryZeroAPI"
dotnet user-secrets set "Jwt:Audience" "InventoryZeroClient"
Environment Variables (for Production)
When deploying to Railway or other platforms, set these environment variables:

env
ConnectionStrings__DefaultConnection=your_postgres_connection_string
Jwt__Key=your_jwt_secret_key
Jwt__Issuer=InventoryZeroAPI
Jwt__Audience=InventoryZeroClient
ASPNETCORE_ENVIRONMENT=Production
📊 API Endpoints
Public Endpoints
GET /api/categories - List all categories

GET /api/products - Browse products with filtering

GET /api/products/{slug} - Get product details

GET /api/shops/{id} - Get shop details

GET /api/shops/{id}/products - Get shop products

GET /test-db - Test database connection

Authentication
POST /api/auth/register - Create new account

POST /api/auth/login - Login and receive JWT token

Authenticated Endpoints
GET /api/users/profile - Get user profile

PUT /api/users/profile - Update profile

POST /api/users/address - Add address

DELETE /api/users/address/{id} - Delete address

PUT /api/users/address/{id}/default - Set default address

GET /api/saved-products - Get saved products

POST /api/saved-products - Save a product

DELETE /api/saved-products/{id} - Unsave a product

GET /api/orders - Get user orders

POST /api/orders - Place an order

GET /api/orders/{id} - Get order details

Seller Endpoints
GET /api/seller/stats - Get shop stats

GET /api/seller/products - Get seller's products

POST /api/seller/products - Create product

PUT /api/seller/products/{id} - Update product

DELETE /api/seller/products/{id} - Delete product

GET /api/seller/orders - Get shop orders

PUT /api/seller/orders/{id}/status - Update order status

GET /api/seller/payouts - Get payout history

POST /api/seller/shops - Create a shop

GET /api/seller/shops - Get seller's shops

Admin Endpoints
GET /api/admin/stats - Platform analytics

GET /api/admin/users - List users

PUT /api/admin/users/{id}/role - Change user role

PUT /api/admin/users/{id}/toggle - Toggle user status

GET /api/admin/shops - List shops

PUT /api/admin/shops/{id}/approve - Approve shop

PUT /api/admin/shops/{id}/reject - Reject shop

GET /api/admin/products - List products

PUT /api/admin/products/{id}/toggle - Toggle product status

GET /api/admin/orders - List orders

GET /api/admin/payouts - List payouts

POST /api/admin/payouts/process - Process pending payouts

💡 Design Philosophy
Performance Optimizations
AsNoTracking() for read-only queries to reduce memory overhead

AsSplitQuery() for complex includes to prevent Cartesian explosion

Projection to DTOs to minimize data transferred from database

Early returns for empty results to prevent unnecessary database operations

Batch queries to avoid N+1 query problems

Proper indexing on frequently queried columns

Security Considerations
All user input is escaped using escapeHtml() function

JWT tokens for authentication

Password hashing with BCrypt

Role-based access control

No sensitive data exposed in logs

CORS properly configured

SQL injection prevention via EF Core

XSS protection through content escaping

Secure JWT key management using user secrets or environment variables

User Experience
Responsive design for mobile and desktop

Intuitive navigation with role-based dashboards

Real-time feedback with toasts and loading states

Clean, modern UI with consistent design language

Accessible forms with proper labels and validation

Smooth transitions and animations

🎨 UI Features
Design System
The interface features a custom design system with:

Color Palette: Primary green (#1D9E75) for a trustworthy, eco-friendly feel

Typography: Clean sans-serif fonts with proper hierarchy

Components: Cards, badges, buttons, forms, and navigation

Icons: Tabler Icons library for consistent iconography

Responsive: Mobile-first design that works on all screen sizes

Page Overview
Homepage: Category browsing, featured deals, how it works section

Browse Page: Advanced filtering by category, price, condition, and urgency

Dashboards: Role-specific dashboards for buyers, sellers, and admins

Checkout: Simulated order placement with address management

Shop Profile: Shop details with product listings and seller rating

Order Detail: Complete order tracking with timeline view

🐳 Docker Deployment
The project includes a Dockerfile for containerized deployment:

bash
# Build the Docker image
docker build -t inventoryzero-api .

# Run the container
docker run -p 8080:8080 -e ConnectionStrings__DefaultConnection="your_connection_string" -e Jwt__Key="your_jwt_key" inventoryzero-api
📝 License
This project is for demonstration purposes only. All rights reserved.
