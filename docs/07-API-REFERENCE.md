# API Reference

## Enterprise Retail ERP Platform for Georgia

**Version:** 1.0
**Base URL:** `http://localhost:5000/api/v1`
**Authentication:** JWT Bearer token (unless noted as `AllowAnonymous`)
**Content-Type:** `application/json`

---

## Table of Contents

1. [General Information](#1-general-information)
2. [Authentication (Auth)](#2-authentication)
3. [Users](#3-users)
4. [Products](#4-products)
5. [Pricing](#5-pricing)
6. [Inventory](#6-inventory)
7. [Point of Sale (POS)](#7-point-of-sale)
8. [Procurement](#8-procurement)
9. [Finance](#9-finance)
10. [Compliance (RS.GE)](#10-compliance)
11. [Customers (CRM)](#11-customers)
12. [Organization](#12-organization)
13. [Reports](#13-reports)
14. [Licensing](#14-licensing)
15. [Updates](#15-updates)
16. [Health Check](#16-health-check)

---

## 1. General Information

### Base Route Convention

All controllers extend `ApiControllerBase` and follow the route pattern:

```
/api/v1/{controller}/{action}
```

### Authentication

Most endpoints require a valid JWT Bearer token in the `Authorization` header:

```
Authorization: Bearer <access_token>
```

Tokens are obtained via the login endpoint and refreshed via the refresh endpoint.

### Rate Limiting

| Policy  | Limit              | Applied To                  |
|---------|--------------------|-----------------------------|
| `fixed` | 100 requests / min | All controller endpoints    |
| `auth`  | 10 requests / min  | Login, refresh, license activation |

### Standard Error Response

```json
{
  "error": "Human-readable error message",
  "errorCode": "NOT_FOUND | VALIDATION_ERROR | ...",
  "errors": ["optional", "validation", "details"]
}
```

### HTTP Status Codes

| Code | Meaning |
|------|---------|
| 200  | Success |
| 201  | Created |
| 202  | Accepted (async processing) |
| 400  | Bad Request / Validation Error |
| 401  | Unauthorized (invalid/missing token) |
| 404  | Not Found |
| 429  | Rate limit exceeded |

### Pagination

Paginated endpoints accept these query parameters:

| Parameter  | Type | Default | Description          |
|-----------|------|---------|----------------------|
| `page`    | int  | 1       | Page number          |
| `pageSize`| int  | 20      | Items per page       |

Paginated responses include:

```json
{
  "items": [],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20
}
```

---

## 2. Authentication

**Base path:** `/api/v1/auth`

### POST /auth/login

Log in and receive JWT tokens.

- **Auth:** Anonymous
- **Rate Limit:** `auth` (10/min)

**Request Body:**

```json
{
  "username": "admin",
  "password": "SecureP@ss123",
  "twoFactorCode": "123456"
}
```

| Field          | Type    | Required | Description                    |
|---------------|---------|----------|--------------------------------|
| username      | string  | Yes      | User's login name              |
| password      | string  | Yes      | User's password                |
| twoFactorCode | string  | No       | TOTP code if 2FA is enabled    |

**Success Response (200):**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g...",
  "expiresAt": "2026-06-21T12:00:00+00:00",
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "username": "admin",
    "email": "admin@example.com",
    "firstName": "Admin",
    "lastName": "User",
    "firstNameKa": null,
    "lastNameKa": null,
    "defaultLanguage": "en",
    "roles": ["super_admin"],
    "defaultStoreId": null
  }
}
```

**Error Response (401):**

```json
{
  "error": "Invalid username or password"
}
```

---

### POST /auth/refresh

Refresh an expired access token.

- **Auth:** Anonymous
- **Rate Limit:** `auth` (10/min)

**Request Body:**

```json
{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g..."
}
```

**Success Response (200):** Same as login response.

**Error Response (401):**

```json
{
  "error": "Invalid or expired refresh token"
}
```

---

### POST /auth/logout

Revoke a refresh token.

- **Auth:** Required

**Request Body:**

```json
{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g..."
}
```

**Success Response (200):**

```json
{
  "message": "Logged out successfully"
}
```

---

### GET /auth/me

Get current authenticated user information.

- **Auth:** Required

**Success Response (200):**

```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "companyId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "username": "admin",
  "email": "admin@example.com",
  "roles": ["super_admin", "company_admin"]
}
```

---

### POST /auth/2fa/setup

Begin two-factor authentication setup. Returns a TOTP secret and QR code URI.

- **Auth:** Required

**Request Body:** None

**Success Response (200):** Returns setup data including the shared secret.

---

### POST /auth/2fa/confirm

Confirm two-factor authentication setup with a TOTP code.

- **Auth:** Required

**Request Body:**

```json
{
  "code": "123456"
}
```

**Success Response (200):** Empty response.

**Error Response (400):**

```json
{
  "error": "Invalid verification code"
}
```

---

### POST /auth/2fa/disable

Disable two-factor authentication.

- **Auth:** Required

**Request Body:**

```json
{
  "code": "123456"
}
```

**Success Response (200):** Empty response.

---

## 3. Users

**Base path:** `/api/v1/users`
**Auth:** Required for all endpoints

### GET /users

List users with optional filtering.

**Query Parameters:**

| Parameter  | Type   | Default | Description            |
|-----------|--------|---------|------------------------|
| page      | int    | 1       | Page number            |
| pageSize  | int    | 20      | Items per page         |
| search    | string | null    | Search by name/email   |
| isActive  | bool   | null    | Filter by active status|

**Success Response (200):**

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "username": "admin",
    "email": "admin@example.com",
    "firstName": "Admin",
    "lastName": "User",
    "firstNameKa": null,
    "lastNameKa": null,
    "phone": "+995555123456",
    "defaultStoreId": null,
    "defaultLanguage": "en",
    "is2FaEnabled": false,
    "isActive": true,
    "lastLoginAt": "2026-06-20T10:30:00+00:00",
    "createdAt": "2026-06-01T00:00:00+00:00",
    "roles": ["super_admin"]
  }
]
```

---

### POST /users

Create a new user.

**Request Body:**

```json
{
  "username": "cashier1",
  "email": "cashier1@store.ge",
  "password": "SecureP@ss123",
  "firstName": "Nino",
  "lastName": "Kapanadze",
  "firstNameKa": "ნინო",
  "lastNameKa": "კაპანაძე",
  "phone": "+995555123456",
  "defaultStoreId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "defaultLanguage": "ka",
  "roleIds": ["3fa85f64-5717-4562-b3fc-2c963f66afa6"]
}
```

| Field          | Type       | Required | Description                      |
|---------------|------------|----------|----------------------------------|
| username      | string     | Yes      | Unique username                  |
| email         | string     | Yes      | Email address                    |
| password      | string     | Yes      | Password (minimum requirements apply) |
| firstName     | string     | Yes      | First name (Latin)               |
| lastName      | string     | Yes      | Last name (Latin)                |
| firstNameKa   | string     | No       | First name (Georgian)            |
| lastNameKa    | string     | No       | Last name (Georgian)             |
| phone         | string     | No       | Phone number                     |
| defaultStoreId| guid       | No       | Default store assignment         |
| defaultLanguage| string    | Yes      | `en` or `ka`                     |
| roleIds       | guid[]     | Yes      | List of role IDs to assign       |

**Success Response (201):** Returns the created user DTO.

---

## 4. Products

**Base path:** `/api/v1/products`
**Auth:** Required for all endpoints

### GET /products

List products with optional filtering and pagination.

**Query Parameters:**

| Parameter   | Type   | Default | Description              |
|------------|--------|---------|--------------------------|
| page       | int    | 1       | Page number              |
| pageSize   | int    | 20      | Items per page           |
| search     | string | null    | Search by name/SKU       |
| categoryId | guid   | null    | Filter by category       |
| isActive   | bool   | null    | Filter by active status  |

**Success Response (200):**

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "sku": "PROD-001",
    "name": "Borjomi Mineral Water 500ml",
    "nameKa": "ბორჯომი",
    "description": "Natural mineral water",
    "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "categoryName": "Beverages",
    "unitOfMeasure": "PIECE",
    "vatApplicable": true,
    "weightKg": 0.5,
    "isSerialized": false,
    "isBatchTracked": false,
    "hasExpiry": true,
    "isActive": true,
    "createdAt": "2026-06-01T00:00:00+00:00",
    "barcodes": [
      {
        "id": "...",
        "barcode": "4860019001568",
        "barcodeType": "EAN13",
        "isPrimary": true
      }
    ],
    "variants": []
  }
]
```

---

### GET /products/{id}

Get a single product by ID.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| id        | guid | Product ID  |

**Success Response (200):** Returns the product DTO.

**Error Response (404):** Product not found.

---

### POST /products

Create a new product.

**Request Body:**

```json
{
  "sku": "PROD-001",
  "name": "Borjomi Mineral Water 500ml",
  "nameKa": "ბორჯომი",
  "description": "Natural mineral water",
  "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "unitOfMeasure": "PIECE",
  "vatApplicable": true,
  "weightKg": 0.5,
  "volumeL": 0.5,
  "widthCm": null,
  "heightCm": null,
  "depthCm": null,
  "minStockLevel": 10,
  "maxStockLevel": 500,
  "reorderPoint": 20,
  "reorderQty": 100,
  "isSerialized": false,
  "isBatchTracked": false,
  "hasExpiry": true,
  "barcodes": [
    {
      "barcode": "4860019001568",
      "barcodeType": "EAN13",
      "isPrimary": true
    }
  ]
}
```

| Field          | Type       | Required | Description                       |
|---------------|------------|----------|-----------------------------------|
| sku           | string     | Yes      | Unique product SKU                |
| name          | string     | Yes      | Product name (Latin)              |
| nameKa        | string     | No       | Product name (Georgian)           |
| description   | string     | No       | Product description               |
| categoryId    | guid       | Yes      | Category ID                       |
| unitOfMeasure | string     | Yes      | Unit: PIECE, KG, LITER, etc.     |
| vatApplicable | bool       | Yes      | Subject to 18% VAT               |
| weightKg      | decimal    | No       | Weight in kilograms              |
| volumeL       | decimal    | No       | Volume in liters                 |
| widthCm       | decimal    | No       | Width in centimeters             |
| heightCm      | decimal    | No       | Height in centimeters            |
| depthCm       | decimal    | No       | Depth in centimeters             |
| minStockLevel | decimal    | No       | Minimum stock alert threshold    |
| maxStockLevel | decimal    | No       | Maximum stock level              |
| reorderPoint  | decimal    | No       | Stock level triggering reorder   |
| reorderQty    | decimal    | No       | Default reorder quantity         |
| isSerialized  | bool       | Yes      | Track individual serial numbers  |
| isBatchTracked| bool       | Yes      | Track by batch/lot number        |
| hasExpiry     | bool       | Yes      | Has expiration date tracking     |
| barcodes      | array      | No       | List of product barcodes         |

**Success Response (201):** Returns the created product DTO with `Location` header.

---

### GET /products/categories

List product categories.

**Query Parameters:**

| Parameter | Type | Default | Description              |
|-----------|------|---------|--------------------------|
| parentId  | guid | null    | Filter by parent category|
| isActive  | bool | null    | Filter by active status  |

**Success Response (200):**

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "parentId": null,
    "code": "BEV",
    "name": "Beverages",
    "nameKa": "სასმელები",
    "sortOrder": 1,
    "isActive": true,
    "productCount": 45
  }
]
```

---

## 5. Pricing

**Base path:** `/api/v1/pricing`
**Auth:** Required for all endpoints

### GET /pricing/price-lists

List price lists with optional filtering.

**Query Parameters:**

| Parameter  | Type   | Default | Description                      |
|-----------|--------|---------|----------------------------------|
| priceType | string | null    | Filter: Retail, Wholesale, etc. |
| isActive  | bool   | null    | Filter by active status          |
| page      | int    | 1       | Page number                      |
| pageSize  | int    | 20      | Items per page                   |

**Success Response (200):**

```json
[
  {
    "id": "...",
    "code": "RTL-2026",
    "name": "Retail Prices 2026",
    "nameKa": null,
    "currency": "GEL",
    "priceType": "Retail",
    "storeId": null,
    "validFrom": "2026-01-01T00:00:00+00:00",
    "validTo": null,
    "isActive": true,
    "priority": 1,
    "itemCount": 150,
    "createdAt": "2026-01-01T00:00:00+00:00"
  }
]
```

---

### POST /pricing/price-lists

Create a new price list.

**Request Body:**

```json
{
  "code": "RTL-2026",
  "name": "Retail Prices 2026",
  "nameKa": null,
  "priceType": "Retail",
  "storeId": null,
  "validFrom": "2026-01-01T00:00:00+00:00",
  "validTo": null,
  "priority": 1
}
```

| Field     | Type   | Required | Description                   |
|-----------|--------|----------|-------------------------------|
| code      | string | Yes      | Unique price list code        |
| name      | string | Yes      | Price list name               |
| nameKa    | string | No       | Name in Georgian              |
| priceType | string | Yes      | Retail, Wholesale, etc.       |
| storeId   | guid   | No       | Store-specific price list     |
| validFrom | datetime| Yes     | Start of validity period      |
| validTo   | datetime| No      | End of validity period        |
| priority  | int    | Yes      | Priority for price resolution |

**Success Response (201):** Returns the created price list DTO.

---

### GET /pricing/price-lists/{priceListId}/items

List items in a price list.

**Path Parameters:**

| Parameter   | Type | Description   |
|-------------|------|---------------|
| priceListId | guid | Price list ID |

**Query Parameters:**

| Parameter | Type   | Default | Description              |
|-----------|--------|---------|--------------------------|
| search    | string | null    | Search by product name   |
| page      | int    | 1       | Page number              |
| pageSize  | int    | 20      | Items per page           |

**Success Response (200):**

```json
[
  {
    "id": "...",
    "priceListId": "...",
    "productId": "...",
    "productName": "Borjomi 500ml",
    "variantId": null,
    "price": 2.50,
    "minQty": 1
  }
]
```

---

### POST /pricing/prices

Set or update a price for a product in a price list.

**Request Body:**

```json
{
  "priceListId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "price": 2.50,
  "minQty": 1,
  "variantId": null
}
```

| Field       | Type    | Required | Description                    |
|------------|---------|----------|--------------------------------|
| priceListId| guid    | Yes      | Target price list              |
| productId  | guid    | Yes      | Product to price               |
| price      | decimal | Yes      | Unit price                     |
| minQty     | decimal | Yes      | Minimum quantity for this price|
| variantId  | guid    | No       | Specific variant               |

**Success Response (200):** Returns the price list item DTO.

---

### GET /pricing/promotions

List promotions.

**Query Parameters:**

| Parameter | Type | Default | Description              |
|-----------|------|---------|--------------------------|
| isActive  | bool | null    | Filter by active status  |
| page      | int  | 1       | Page number              |
| pageSize  | int  | 20      | Items per page           |

**Success Response (200):**

```json
[
  {
    "id": "...",
    "code": "SUMMER26",
    "name": "Summer Sale 2026",
    "nameKa": null,
    "promotionType": "PercentageDiscount",
    "discountValue": 15.0,
    "conditions": null,
    "validFrom": "2026-06-01T00:00:00+00:00",
    "validTo": "2026-08-31T23:59:59+00:00",
    "isActive": true,
    "maxUses": 1000,
    "currentUses": 42,
    "createdAt": "2026-05-15T00:00:00+00:00"
  }
]
```

---

### POST /pricing/promotions

Create a new promotion.

**Request Body:**

```json
{
  "code": "SUMMER26",
  "name": "Summer Sale 2026",
  "nameKa": null,
  "promotionType": "PercentageDiscount",
  "discountValue": 15.0,
  "conditions": null,
  "validFrom": "2026-06-01T00:00:00+00:00",
  "validTo": "2026-08-31T23:59:59+00:00",
  "maxUses": 1000
}
```

| Field         | Type    | Required | Description                          |
|--------------|---------|----------|--------------------------------------|
| code         | string  | Yes      | Unique promotion code                |
| name         | string  | Yes      | Promotion name                       |
| nameKa       | string  | No       | Name in Georgian                     |
| promotionType| string  | Yes      | PercentageDiscount, FixedDiscount, etc.|
| discountValue| decimal | No       | Discount amount or percentage        |
| conditions   | string  | No       | JSON conditions for applicability    |
| validFrom    | datetime| Yes      | Start date                           |
| validTo      | datetime| No       | End date                             |
| maxUses      | int     | No       | Maximum number of uses               |

**Success Response (201):** Returns the created promotion DTO.

---

## 6. Inventory

**Base path:** `/api/v1/inventory`
**Auth:** Required for all endpoints

### GET /inventory/stock-levels

Query current stock levels.

**Query Parameters:**

| Parameter    | Type | Default | Description                   |
|-------------|------|---------|-------------------------------|
| warehouseId | guid | null    | Filter by warehouse           |
| productId   | guid | null    | Filter by product             |
| lowStockOnly| bool | false   | Only show items below reorder point|
| page        | int  | 1       | Page number                   |
| pageSize    | int  | 50      | Items per page                |

**Success Response (200):**

```json
[
  {
    "id": "...",
    "productId": "...",
    "productName": "Borjomi 500ml",
    "variantId": null,
    "warehouseId": "...",
    "warehouseName": "Main Warehouse",
    "locationCode": "A-01-03",
    "quantityOnHand": 150,
    "quantityReserved": 10,
    "quantityInTransit": 25,
    "availableQty": 140,
    "costPrice": 1.20,
    "lastCountDate": "2026-06-15T00:00:00+00:00",
    "updatedAt": "2026-06-20T08:00:00+00:00"
  }
]
```

---

### GET /inventory/movements

Query stock movements history.

**Query Parameters:**

| Parameter   | Type | Default | Description         |
|------------|------|---------|---------------------|
| warehouseId| guid | null    | Filter by warehouse |
| productId  | guid | null    | Filter by product   |
| page       | int  | 1       | Page number         |
| pageSize   | int  | 50      | Items per page      |

**Success Response (200):**

```json
[
  {
    "id": "...",
    "movementType": "Adjustment",
    "productId": "...",
    "productName": "Borjomi 500ml",
    "variantId": null,
    "warehouseId": "...",
    "warehouseName": "Main Warehouse",
    "quantity": -5,
    "costPrice": 1.20,
    "referenceType": "StockCount",
    "referenceId": "...",
    "batchNumber": null,
    "serialNumber": null,
    "expiryDate": null,
    "notes": "Shrinkage adjustment",
    "createdAt": "2026-06-20T08:00:00+00:00",
    "createdBy": "..."
  }
]
```

---

### POST /inventory/adjust

Adjust stock levels (increase or decrease).

**Request Body:**

```json
{
  "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "warehouseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "quantity": -5,
  "adjustedBy": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "notes": "Shrinkage adjustment",
  "variantId": null
}
```

| Field       | Type    | Required | Description                               |
|------------|---------|----------|-------------------------------------------|
| productId  | guid    | Yes      | Product to adjust                         |
| warehouseId| guid    | Yes      | Warehouse location                        |
| quantity   | decimal | Yes      | Positive = add, negative = deduct. Non-zero. |
| adjustedBy | guid    | Yes      | User performing adjustment                |
| notes      | string  | No       | Reason for adjustment                     |
| variantId  | guid    | No       | Specific product variant                  |

**Success Response (200):** Empty response.

---

### GET /inventory/transfers

List transfer orders.

**Query Parameters:**

| Parameter   | Type   | Default | Description                |
|------------|--------|---------|----------------------------|
| warehouseId| guid   | null    | Filter by warehouse        |
| status     | string | null    | Draft, Approved, InTransit, Received, Cancelled |
| page       | int    | 1       | Page number                |
| pageSize   | int    | 20      | Items per page             |

**Success Response (200):**

```json
[
  {
    "id": "...",
    "transferNumber": "TR-260620120000-1234",
    "sourceWarehouseId": "...",
    "sourceWarehouseName": "Main Warehouse",
    "destWarehouseId": "...",
    "destWarehouseName": "Store Warehouse",
    "status": "Draft",
    "rsGeWaybillId": null,
    "requestedBy": "...",
    "createdAt": "2026-06-20T12:00:00+00:00"
  }
]
```

---

### POST /inventory/transfers

Create a new transfer order.

**Request Body:**

```json
{
  "sourceWarehouseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "destWarehouseId": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
  "requestedBy": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "notes": "Restocking branch store",
  "lines": [
    {
      "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "quantity": 50,
      "variantId": null
    }
  ]
}
```

| Field             | Type   | Required | Description                    |
|------------------|--------|----------|--------------------------------|
| sourceWarehouseId| guid   | Yes      | Source warehouse                |
| destWarehouseId  | guid   | Yes      | Destination warehouse           |
| requestedBy      | guid   | Yes      | User creating the transfer      |
| notes            | string | No       | Additional notes                |
| lines            | array  | Yes      | Transfer line items             |
| lines[].productId| guid  | Yes      | Product to transfer             |
| lines[].quantity | decimal| Yes      | Quantity to transfer            |
| lines[].variantId| guid  | No       | Specific variant                |

**Success Response (201):**

```json
{
  "id": "...",
  "transferNumber": "TR-260620120000-1234",
  "status": "Draft"
}
```

---

### POST /inventory/transfers/{id}/approve

Approve a draft transfer order.

- **Path:** `id` (guid) - Transfer order ID
- **Success (200):** Empty response
- **Error (400):** Only draft orders can be approved

---

### POST /inventory/transfers/{id}/ship

Ship an approved transfer order. Deducts stock from source warehouse.

- **Path:** `id` (guid) - Transfer order ID
- **Success (200):** Empty response
- **Error (400):** Only approved orders can be shipped, or insufficient stock

---

### POST /inventory/transfers/{id}/receive

Receive a shipped transfer. Adds stock to destination warehouse.

- **Path:** `id` (guid) - Transfer order ID

**Request Body (optional):**

```json
{
  "lines": [
    {
      "lineId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "receivedQty": 48
    }
  ]
}
```

If no body is provided, all lines are received at their shipped quantities.

**Success (200):** Empty response.

---

### POST /inventory/transfers/{id}/cancel

Cancel a transfer order. Cannot cancel received or already cancelled orders.

- **Path:** `id` (guid) - Transfer order ID
- **Success (200):** Empty response

---

### GET /inventory/counts

List stock count sessions.

**Query Parameters:**

| Parameter   | Type   | Default | Description                   |
|------------|--------|---------|-------------------------------|
| warehouseId| guid   | null    | Filter by warehouse           |
| status     | string | null    | Filter by status              |
| page       | int    | 1       | Page number                   |
| pageSize   | int    | 20      | Items per page                |

---

### POST /inventory/counts

Create a new stock count session.

**Request Body:**

```json
{
  "warehouseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "countType": "Full",
  "createdBy": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "productIds": null
}
```

| Field       | Type    | Required | Description                          |
|------------|---------|----------|--------------------------------------|
| warehouseId| guid    | Yes      | Warehouse to count                   |
| countType  | string  | Yes      | Full, Cycle, Spot                    |
| createdBy  | guid    | Yes      | User initiating the count            |
| productIds | guid[]  | No       | Specific products (null = all stock) |

**Success Response (201):**

```json
{
  "id": "...",
  "status": "InProgress",
  "lineCount": 45
}
```

---

### POST /inventory/counts/{countId}/lines/{lineId}/record

Record the counted quantity for a specific stock count line.

**Path Parameters:**

| Parameter | Type | Description       |
|-----------|------|-------------------|
| countId   | guid | Stock count ID    |
| lineId    | guid | Count line ID     |

**Request Body:**

```json
{
  "countedQty": 148
}
```

**Success (200):** Empty response.

---

### POST /inventory/counts/{countId}/complete

Complete a stock count session and apply adjustments.

- **Path:** `countId` (guid) - Stock count ID
- **Success (200):** Empty response

---

## 7. Point of Sale

**Base path:** `/api/v1/pos`
**Auth:** Required for all endpoints

### POST /pos/sessions

Open a new POS session on a terminal.

**Request Body:**

```json
{
  "terminalId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "cashierId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "openingBalance": 200.00
}
```

| Field         | Type    | Required | Description                |
|--------------|---------|----------|----------------------------|
| terminalId   | guid    | Yes      | POS terminal ID            |
| cashierId    | guid    | Yes      | Cashier user ID            |
| openingBalance| decimal| Yes      | Cash drawer opening amount |

**Success Response (200):**

```json
{
  "sessionId": "...",
  "terminalCode": "T-001",
  "status": "Open",
  "openedAt": "2026-06-20T08:00:00+00:00"
}
```

**Error Response (400):**

```json
{
  "error": "Terminal already has an open session. Close it first."
}
```

---

### POST /pos/sessions/{sessionId}/close

Close an open POS session.

**Path Parameters:**

| Parameter | Type | Description   |
|-----------|------|---------------|
| sessionId | guid | Session ID    |

**Request Body:**

```json
{
  "closingBalance": 1250.50,
  "notes": "End of shift"
}
```

**Success Response (200):** Returns the session closing summary.

---

### GET /pos/sessions

List POS sessions.

**Query Parameters:**

| Parameter  | Type   | Default | Description              |
|-----------|--------|---------|--------------------------|
| terminalId| guid   | null    | Filter by terminal       |
| status    | string | null    | Open, Closed             |
| page      | int    | 1       | Page number              |
| pageSize  | int    | 20      | Items per page           |

---

### POST /pos/transactions

Create a new POS transaction (sale).

**Request Body:**

```json
{
  "sessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "customerId": null,
  "lines": [
    {
      "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "barcode": null,
      "quantity": 2,
      "unitPrice": 2.50,
      "discountAmount": 0,
      "discountReason": null
    }
  ],
  "payments": [
    {
      "paymentMethod": "Cash",
      "amount": 5.00,
      "reference": null,
      "terminalRef": null
    }
  ]
}
```

| Field                    | Type    | Required | Description                     |
|-------------------------|---------|----------|----------------------------------|
| sessionId               | guid    | Yes      | Active POS session ID            |
| customerId              | guid    | No       | Customer for loyalty tracking    |
| lines                   | array   | Yes      | Line items                       |
| lines[].productId       | guid    | No*      | Product ID (*or barcode)         |
| lines[].barcode         | string  | No*      | Barcode (*or productId)          |
| lines[].quantity        | decimal | Yes      | Quantity sold                    |
| lines[].unitPrice       | decimal | Yes      | Unit price                       |
| lines[].discountAmount  | decimal | No       | Discount applied (default: 0)    |
| lines[].discountReason  | string  | No       | Reason for discount              |
| payments                | array   | Yes      | Payment methods                  |
| payments[].paymentMethod| string  | Yes      | Cash, Card, BankTransfer, etc.   |
| payments[].amount       | decimal | Yes      | Payment amount                   |
| payments[].reference    | string  | No       | External payment reference       |
| payments[].terminalRef  | string  | No       | Card terminal reference          |

**Success Response (201):**

```json
{
  "transactionId": "...",
  "transactionNumber": "TXN-260620-00001",
  "subtotal": 5.00,
  "discountTotal": 0,
  "vatTotal": 0.90,
  "total": 5.00,
  "fiscalDocumentId": "...",
  "status": "Completed"
}
```

---

### GET /pos/transactions

List POS transactions with filtering.

**Query Parameters:**

| Parameter | Type     | Default | Description              |
|-----------|----------|---------|--------------------------|
| sessionId | guid     | null    | Filter by session        |
| storeId   | guid     | null    | Filter by store          |
| status    | string   | null    | Completed, Voided        |
| from      | datetime | null    | Start date               |
| to        | datetime | null    | End date                 |
| page      | int      | 1       | Page number              |
| pageSize  | int      | 20      | Items per page           |

---

### GET /pos/transactions/{transactionId}

Get a single transaction with full details.

- **Path:** `transactionId` (guid)
- **Success (200):** Returns transaction detail DTO
- **Error (404):** Transaction not found

---

### POST /pos/transactions/{transactionId}/void

Void a completed transaction.

**Path Parameters:**

| Parameter     | Type | Description    |
|---------------|------|----------------|
| transactionId | guid | Transaction ID |

**Request Body:**

```json
{
  "reason": "Customer returned item within 15 minutes"
}
```

**Success (200):** Empty response.

---

## 8. Procurement

**Base path:** `/api/v1/procurement`
**Auth:** Required for all endpoints

### GET /procurement/suppliers

List suppliers.

**Query Parameters:**

| Parameter | Type   | Default | Description              |
|-----------|--------|---------|--------------------------|
| search    | string | null    | Search by name/code      |
| isActive  | bool   | null    | Filter by active status  |
| page      | int    | 1       | Page number              |
| pageSize  | int    | 20      | Items per page           |

**Success Response (200):**

```json
[
  {
    "id": "...",
    "code": "SUP-001",
    "name": "Georgian Beverages Ltd",
    "nameKa": null,
    "tin": "123456789",
    "isVatPayer": true,
    "contactPerson": "Giorgi Beridze",
    "phone": "+995555000111",
    "email": "info@gevbev.ge",
    "address": "12 Rustaveli Ave, Tbilisi",
    "paymentTerms": "Net 30",
    "creditLimit": 50000.00,
    "rating": 4,
    "isActive": true,
    "createdAt": "2026-01-15T00:00:00+00:00"
  }
]
```

---

### POST /procurement/suppliers

Create a new supplier.

**Request Body:**

```json
{
  "code": "SUP-001",
  "name": "Georgian Beverages Ltd",
  "nameKa": null,
  "tin": "123456789",
  "isVatPayer": true,
  "contactPerson": "Giorgi Beridze",
  "phone": "+995555000111",
  "email": "info@gevbev.ge",
  "address": "12 Rustaveli Ave, Tbilisi",
  "paymentTerms": "Net 30",
  "creditLimit": 50000.00
}
```

**Success Response (201):**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

---

### GET /procurement/purchase-orders

List purchase orders.

**Query Parameters:**

| Parameter  | Type   | Default | Description                |
|-----------|--------|---------|----------------------------|
| supplierId| guid   | null    | Filter by supplier         |
| status    | string | null    | Draft, Approved, Sent, PartiallyReceived, Received, Cancelled |
| page      | int    | 1       | Page number                |
| pageSize  | int    | 20      | Items per page             |

---

### POST /procurement/purchase-orders

Create a new purchase order.

**Request Body:**

```json
{
  "supplierId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "warehouseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "createdBy": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "expectedDate": "2026-07-01T00:00:00+00:00",
  "notes": "Monthly restock",
  "lines": [
    {
      "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "quantity": 100,
      "unitPrice": 1.20,
      "variantId": null
    }
  ]
}
```

| Field              | Type     | Required | Description                    |
|-------------------|----------|----------|--------------------------------|
| supplierId        | guid     | Yes      | Supplier                       |
| warehouseId       | guid     | Yes      | Receiving warehouse            |
| createdBy         | guid     | Yes      | User creating the PO           |
| expectedDate      | datetime | No       | Expected delivery date         |
| notes             | string   | No       | Additional notes               |
| lines             | array    | Yes      | Order line items               |
| lines[].productId | guid     | Yes      | Product to order               |
| lines[].quantity  | decimal  | Yes      | Order quantity                 |
| lines[].unitPrice | decimal  | Yes      | Agreed unit price              |
| lines[].variantId | guid     | No       | Specific variant               |

**Success Response (201):**

```json
{
  "id": "...",
  "poNumber": "PO-260620-12345",
  "total": 141.60
}
```

VAT is automatically calculated at 18% per line.

---

### POST /procurement/purchase-orders/{id}/approve

Approve a draft purchase order.

- **Path:** `id` (guid)
- **Success (200):** Empty response

---

### POST /procurement/purchase-orders/{id}/send

Mark a purchase order as sent to the supplier.

- **Path:** `id` (guid)
- **Success (200):** Empty response

---

### POST /procurement/purchase-orders/{id}/cancel

Cancel a purchase order.

- **Path:** `id` (guid)
- **Success (200):** Empty response

---

### POST /procurement/purchase-orders/{id}/receive

Receive goods against a purchase order. Creates a Goods Receipt Note (GRN).

**Path Parameters:**

| Parameter | Type | Description       |
|-----------|------|-------------------|
| id        | guid | Purchase order ID |

**Request Body:**

```json
{
  "notes": "Received in good condition",
  "lines": [
    {
      "poLineId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "receivedQty": 100,
      "acceptedQty": 98,
      "rejectedQty": 2,
      "batchNumber": "BATCH-2026-06-001",
      "expiryDate": "2027-06-20T00:00:00+00:00"
    }
  ]
}
```

| Field              | Type     | Required | Description                    |
|-------------------|----------|----------|--------------------------------|
| notes             | string   | No       | Receipt notes                  |
| lines             | array    | Yes      | Lines being received           |
| lines[].poLineId  | guid     | Yes      | PO line being received         |
| lines[].receivedQty| decimal | Yes      | Quantity received              |
| lines[].acceptedQty| decimal | No       | Quantity accepted (default: receivedQty)|
| lines[].rejectedQty| decimal | No       | Quantity rejected              |
| lines[].batchNumber| string  | No       | Batch/lot number               |
| lines[].expiryDate| datetime | No       | Expiry date for perishables    |

**Success Response (201):**

```json
{
  "grnId": "...",
  "grnNumber": "GRN-260620-12345",
  "linesReceived": 1
}
```

---

## 9. Finance

**Base path:** `/api/v1/finance`
**Auth:** Required for all endpoints

### GET /finance/chart-of-accounts

List chart of accounts.

**Query Parameters:**

| Parameter | Type | Default | Description              |
|-----------|------|---------|--------------------------|
| isActive  | bool | null    | Filter by active status  |

**Success Response (200):**

```json
[
  {
    "id": "...",
    "accountCode": "1000",
    "name": "Cash and Cash Equivalents",
    "nameKa": null,
    "accountType": "Asset",
    "parentId": null,
    "isHeader": true,
    "isSystem": true,
    "balanceType": "Debit",
    "isActive": true
  }
]
```

---

### POST /finance/chart-of-accounts

Create a new account in the chart of accounts.

**Request Body:**

```json
{
  "accountCode": "1010",
  "name": "Petty Cash",
  "nameKa": null,
  "accountType": "Asset",
  "balanceType": "Debit",
  "parentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "isHeader": false
}
```

| Field       | Type   | Required | Description                              |
|------------|--------|----------|------------------------------------------|
| accountCode| string | Yes      | Unique account code                      |
| name       | string | Yes      | Account name                             |
| nameKa     | string | No       | Name in Georgian                         |
| accountType| string | Yes      | Asset, Liability, Equity, Revenue, Expense|
| balanceType| string | Yes      | Debit or Credit                          |
| parentId   | guid   | No       | Parent account for hierarchy             |
| isHeader   | bool   | Yes      | Header account (cannot post to)          |

**Success Response (201):**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

---

### GET /finance/journal-entries

List journal entries.

**Query Parameters:**

| Parameter | Type   | Default | Description                     |
|-----------|--------|---------|---------------------------------|
| status    | string | null    | Draft, Posted, Reversed         |
| page      | int    | 1       | Page number                     |
| pageSize  | int    | 20      | Items per page                  |

**Success Response (200):**

```json
{
  "items": [
    {
      "id": "...",
      "entryNumber": "JE-260620-12345",
      "entryDate": "2026-06-20T00:00:00+00:00",
      "description": "Daily sales journal",
      "status": "Posted",
      "totalDebit": 1000.00,
      "totalCredit": 1000.00,
      "postedAt": "2026-06-20T18:00:00+00:00",
      "createdAt": "2026-06-20T17:00:00+00:00"
    }
  ],
  "totalCount": 150,
  "page": 1,
  "pageSize": 20
}
```

---

### POST /finance/journal-entries

Create a new journal entry. Debits must equal credits.

**Request Body:**

```json
{
  "entryDate": "2026-06-20T00:00:00+00:00",
  "description": "Daily sales journal",
  "sourceType": "POS",
  "sourceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "createdBy": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "lines": [
    {
      "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "debitAmount": 1000.00,
      "creditAmount": 0,
      "description": "Cash received"
    },
    {
      "accountId": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
      "debitAmount": 0,
      "creditAmount": 1000.00,
      "description": "Sales revenue"
    }
  ]
}
```

| Field                 | Type     | Required | Description                    |
|----------------------|----------|----------|--------------------------------|
| entryDate            | datetime | Yes      | Date of the entry              |
| description          | string   | No       | Entry description              |
| sourceType           | string   | No       | Source module (POS, Procurement, etc.)|
| sourceId             | guid     | No       | Source document ID             |
| createdBy            | guid     | Yes      | User creating the entry        |
| lines                | array    | Yes      | Journal entry lines            |
| lines[].accountId    | guid     | Yes      | GL account ID (must be active, non-header)|
| lines[].debitAmount  | decimal  | Yes      | Debit amount (0 if credit)     |
| lines[].creditAmount | decimal  | Yes      | Credit amount (0 if debit)     |
| lines[].description  | string   | No       | Line description               |

**Validation:** Total debits must equal total credits.

**Success Response (201):**

```json
{
  "id": "...",
  "entryNumber": "JE-260620-12345",
  "totalDebit": 1000.00,
  "totalCredit": 1000.00
}
```

---

### POST /finance/journal-entries/{id}/post

Post a draft journal entry to the general ledger.

- **Path:** `id` (guid)
- **Success (200):** Empty response

---

### GET /finance/bank-accounts

List bank accounts.

**Success Response (200):**

```json
[
  {
    "id": "...",
    "accountName": "Main Operating Account",
    "bankName": "TBC Bank",
    "accountNumber": "GE12345678901234",
    "iban": "GE29TB7890123456789012",
    "currency": "GEL",
    "currentBalance": 45000.00,
    "isActive": true
  }
]
```

---

### POST /finance/bank-accounts

Create a new bank account.

**Request Body:**

```json
{
  "accountName": "Main Operating Account",
  "bankName": "TBC Bank",
  "accountNumber": "GE12345678901234",
  "iban": "GE29TB7890123456789012",
  "currency": "GEL",
  "glAccountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

| Field        | Type   | Required | Description                     |
|-------------|--------|----------|---------------------------------|
| accountName | string | Yes      | Display name for the account    |
| bankName    | string | Yes      | Bank name                       |
| accountNumber| string| Yes      | Bank account number (unique)    |
| iban        | string | No       | IBAN                            |
| currency    | string | Yes      | Currency code (GEL, USD, EUR)   |
| glAccountId | guid   | No       | Linked GL account ID            |

**Success Response (201):**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

---

## 10. Compliance

**Base path:** `/api/v1/compliance`
**Auth:** Required for all endpoints

### RS.GE Integration Endpoints

#### GET /compliance/rsge/health

Check RS.GE service connectivity.

**Success Response (200):**

```json
{
  "service": "RS.GE Integration",
  "status": "Connected",
  "timestamp": "2026-06-20T12:00:00+00:00"
}
```

When RS.GE is unreachable:

```json
{
  "service": "RS.GE Integration",
  "status": "Unavailable",
  "timestamp": "2026-06-20T12:00:00+00:00"
}
```

---

#### GET /compliance/rsge/units

Get RS.GE measurement units reference data.

**Success Response (200):** Array of RS.GE unit objects.

---

#### GET /compliance/rsge/transport-types

Get RS.GE transport types reference data.

**Success Response (200):** Array of transport type objects.

---

#### GET /compliance/rsge/waybill-types

Get RS.GE waybill types reference data.

**Success Response (200):** Array of waybill type objects.

---

#### GET /compliance/rsge/tin/{tin}/name

Look up a company/person name by TIN from RS.GE.

**Path Parameters:**

| Parameter | Type   | Description                 |
|-----------|--------|-----------------------------|
| tin       | string | Tax Identification Number (9-11 digits) |

**Success Response (200):** Returns the name associated with the TIN.

**Error Response (400):**

```json
{
  "error": "TIN must be 9-11 digits."
}
```

---

#### GET /compliance/rsge/tin/{tin}/vat-status

Check if a TIN is a registered VAT payer.

**Path Parameters:**

| Parameter | Type   | Description                 |
|-----------|--------|-----------------------------|
| tin       | string | Tax Identification Number (9-11 digits) |

**Success Response (200):**

```json
{
  "tin": "123456789",
  "isVatPayer": true
}
```

---

### Waybill Endpoints

#### POST /compliance/waybills

Create a new waybill and enqueue for RS.GE submission.

This endpoint returns immediately (HTTP 202 Accepted). The actual RS.GE SOAP submission happens asynchronously via the worker service.

**Request Body:**

```json
{
  "waybillType": 1,
  "buyerTin": "123456789",
  "buyerName": "Example Buyer LLC",
  "sellerTin": "987654321",
  "sellerName": null,
  "startAddress": "12 Rustaveli Ave, Tbilisi",
  "endAddress": "5 Chavchavadze Str, Batumi",
  "vehicleNumber": "AA-123-BB",
  "driverTin": "12345678901",
  "transportType": "Vehicle",
  "internalRef": "SO-2026-001",
  "referenceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "referenceType": "SalesOrder",
  "goods": [
    {
      "productName": "Borjomi 500ml",
      "unitId": 1,
      "quantity": 100,
      "price": 2.50,
      "barCode": "4860019001568"
    }
  ]
}
```

| Field         | Type    | Required | Description                        |
|--------------|---------|----------|------------------------------------|
| waybillType  | int     | Yes      | RS.GE waybill type code            |
| buyerTin     | string  | Yes      | Buyer's TIN (9-11 digits)          |
| buyerName    | string  | No       | Buyer company name                 |
| sellerTin    | string  | No       | Seller's TIN (defaults to company) |
| sellerName   | string  | No       | Seller company name                |
| startAddress | string  | Yes      | Start/shipping address             |
| endAddress   | string  | Yes      | Destination address                |
| vehicleNumber| string  | No       | Vehicle registration number        |
| driverTin    | string  | No       | Driver's personal TIN              |
| transportType| string  | No       | Transport type                     |
| internalRef  | string  | No       | Internal reference number          |
| referenceId  | guid    | No       | Source document ID                 |
| referenceType| string  | No       | Source document type               |
| goods        | array   | Yes      | Goods items                        |
| goods[].productName| string| Yes  | Product name for RS.GE             |
| goods[].unitId| int    | Yes      | RS.GE unit ID                      |
| goods[].quantity| decimal| Yes    | Quantity                           |
| goods[].price| decimal | Yes      | Unit price                         |
| goods[].barCode| string| No      | Product barcode                    |

**Success Response (202):**

```json
{
  "fiscalDocumentId": "...",
  "waybillId": "...",
  "status": "Queued"
}
```

---

#### POST /compliance/waybills/{fiscalDocumentId}/confirm

Enqueue a waybill for confirmation with RS.GE.

- **Path:** `fiscalDocumentId` (guid)
- **Success (202):** Accepted for async processing

---

#### POST /compliance/waybills/{fiscalDocumentId}/close

Enqueue a waybill for closure with RS.GE.

- **Path:** `fiscalDocumentId` (guid)
- **Success (202):** Accepted for async processing

---

#### GET /compliance/waybills

List waybills.

**Query Parameters:**

| Parameter | Type | Default | Description    |
|-----------|------|---------|----------------|
| page      | int  | 1       | Page number    |
| pageSize  | int  | 20      | Items per page (max 100) |

**Success Response (200):**

```json
[
  {
    "id": "...",
    "fiscalDocumentId": "...",
    "waybillNumber": "WB-123456",
    "waybillType": 1,
    "sellerTin": "987654321",
    "buyerTin": "123456789",
    "buyerName": "Example Buyer LLC",
    "status": "Submitted",
    "totalAmount": 250.00,
    "startAddress": "12 Rustaveli Ave, Tbilisi",
    "endAddress": "5 Chavchavadze Str, Batumi",
    "createdAt": "2026-06-20T12:00:00+00:00"
  }
]
```

---

### Fiscal Document Endpoints

#### GET /compliance/fiscal-documents

List fiscal documents (invoices, waybills, receipts).

**Query Parameters:**

| Parameter | Type   | Default | Description              |
|-----------|--------|---------|--------------------------|
| type      | string | null    | Filter by document type  |
| status    | string | null    | Pending, Queued, Submitted, Confirmed, Failed |
| page      | int    | 1       | Page number              |
| pageSize  | int    | 20      | Items per page (max 100) |

**Success Response (200):**

```json
[
  {
    "id": "...",
    "documentType": "Waybill",
    "documentNumber": "WB-123456",
    "internalRef": "SO-2026-001",
    "status": "Confirmed",
    "rsGeId": "ext-12345",
    "rsGeStatus": "Active",
    "submissionDeadline": "2026-06-22T00:00:00+00:00",
    "submittedAt": "2026-06-20T12:05:00+00:00",
    "confirmedAt": "2026-06-20T12:06:00+00:00",
    "retryCount": 0,
    "lastError": null,
    "createdAt": "2026-06-20T12:00:00+00:00"
  }
]
```

---

### VAT and Compliance Reporting

#### GET /compliance/vat-summary

Get VAT summary for a given period.

**Query Parameters:**

| Parameter | Type | Default       | Description    |
|-----------|------|---------------|----------------|
| year      | int  | current year  | Tax year       |
| month     | int  | current month | Tax month      |

**Success Response (200):**

```json
{
  "period": "2026-06",
  "outputVat": 15000.00,
  "inputVat": 8500.00,
  "netVat": 6500.00,
  "status": "NotFiled",
  "currency": "GEL"
}
```

---

#### GET /compliance/deadlines

Get fiscal documents approaching submission deadlines.

**Query Parameters:**

| Parameter   | Type | Default | Description                         |
|------------|------|---------|-------------------------------------|
| warningDays| int  | 7       | Days ahead to check (1-30)          |

**Success Response (200):**

```json
{
  "checkedAt": "2026-06-20T12:00:00+00:00",
  "overdueCount": 2,
  "dueSoonCount": 5,
  "documents": [
    {
      "id": "...",
      "type": "Waybill",
      "internalRef": "SO-2026-001",
      "status": "Failed",
      "deadline": "2026-06-19T00:00:00+00:00",
      "isOverdue": true,
      "lastError": "RS.GE SOAP timeout"
    }
  ]
}
```

---

## 11. Customers

**Base path:** `/api/v1/customers`
**Auth:** Required for all endpoints

### GET /customers

List customers.

**Query Parameters:**

| Parameter | Type   | Default | Description              |
|-----------|--------|---------|--------------------------|
| search    | string | null    | Search by name/phone     |
| isActive  | bool   | null    | Filter by active status  |
| page      | int    | 1       | Page number              |
| pageSize  | int    | 20      | Items per page           |

**Success Response (200):**

```json
[
  {
    "id": "...",
    "customerNumber": "C-260601-12345",
    "firstName": "Nino",
    "lastName": "Kapanadze",
    "firstNameKa": "ნინო",
    "lastNameKa": "კაპანაძე",
    "companyName": null,
    "tin": null,
    "phone": "+995555123456",
    "email": "nino@example.ge",
    "loyaltyCardNumber": "LC-00001",
    "loyaltyTier": "Gold",
    "loyaltyPoints": 1250,
    "totalPurchases": 5400.00,
    "totalVisits": 32,
    "lastVisitAt": "2026-06-19T14:30:00+00:00",
    "isActive": true,
    "createdAt": "2026-01-15T00:00:00+00:00"
  }
]
```

---

### POST /customers

Create a new customer.

**Request Body:**

```json
{
  "firstName": "Nino",
  "lastName": "Kapanadze",
  "firstNameKa": "ნინო",
  "lastNameKa": "კაპანაძე",
  "companyName": null,
  "tin": null,
  "phone": "+995555123456",
  "email": "nino@example.ge",
  "dateOfBirth": "1990-05-15T00:00:00+00:00",
  "gender": "Female",
  "consentSms": true,
  "consentEmail": true
}
```

| Field       | Type     | Required | Description                    |
|------------|----------|----------|--------------------------------|
| firstName  | string   | Yes      | First name (Latin)             |
| lastName   | string   | Yes      | Last name (Latin)              |
| firstNameKa| string   | No       | First name (Georgian)          |
| lastNameKa | string   | No       | Last name (Georgian)           |
| companyName| string   | No       | Company name (B2B customers)   |
| tin        | string   | No       | Tax Identification Number      |
| phone      | string   | No       | Phone number (unique if provided)|
| email      | string   | No       | Email address                  |
| dateOfBirth| datetime | No       | Date of birth                  |
| gender     | string   | No       | Gender                         |
| consentSms | bool     | Yes      | SMS marketing consent          |
| consentEmail| bool    | Yes      | Email marketing consent        |

**Success Response (201):**

```json
{
  "id": "...",
  "customerNumber": "C-260620-12345"
}
```

---

### POST /customers/{customerId}/loyalty/earn

Award loyalty points to a customer.

**Path Parameters:**

| Parameter  | Type | Description |
|------------|------|-------------|
| customerId | guid | Customer ID |

**Request Body:**

```json
{
  "points": 100,
  "referenceType": "POS",
  "referenceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "description": "Purchase at Store #1"
}
```

| Field         | Type   | Required | Description                  |
|--------------|--------|----------|------------------------------|
| points       | int    | Yes      | Points to award              |
| referenceType| string | No       | Source type (POS, Promotion) |
| referenceId  | guid   | No       | Source document ID           |
| description  | string | No       | Description                  |

**Success Response (200):**

```json
{
  "balance": 1350
}
```

---

### POST /customers/{customerId}/loyalty/redeem

Redeem loyalty points.

**Path Parameters:**

| Parameter  | Type | Description |
|------------|------|-------------|
| customerId | guid | Customer ID |

**Request Body:**

```json
{
  "points": 500,
  "description": "Redeemed for discount"
}
```

**Success Response (200):**

```json
{
  "balance": 850
}
```

---

## 12. Organization

**Base path:** `/api/v1/organization`
**Auth:** Required for all endpoints

### GET /organization/stores

List stores.

**Query Parameters:**

| Parameter | Type | Default | Description              |
|-----------|------|---------|--------------------------|
| isActive  | bool | null    | Filter by active status  |

**Success Response (200):**

```json
[
  {
    "id": "...",
    "code": "STR-001",
    "name": "Tbilisi Main Store",
    "nameKa": null,
    "storeType": "Retail",
    "address": "12 Rustaveli Ave",
    "city": "Tbilisi",
    "region": "Tbilisi",
    "phone": "+995322123456",
    "managerUserId": "...",
    "isActive": true,
    "createdAt": "2026-01-01T00:00:00+00:00"
  }
]
```

---

### GET /organization/warehouses

List warehouses.

**Query Parameters:**

| Parameter | Type | Default | Description              |
|-----------|------|---------|--------------------------|
| isActive  | bool | null    | Filter by active status  |

**Success Response (200):**

```json
[
  {
    "id": "...",
    "code": "WH-001",
    "name": "Main Warehouse",
    "nameKa": null,
    "warehouseType": "Central",
    "address": "45 Industrial Zone",
    "city": "Tbilisi",
    "region": "Tbilisi",
    "linkedStoreId": null,
    "isActive": true,
    "createdAt": "2026-01-01T00:00:00+00:00"
  }
]
```

---

## 13. Reports

**Base path:** `/api/v1/reports`
**Auth:** Required for all endpoints

### GET /reports/sales

Generate a sales report for a date range.

**Query Parameters:**

| Parameter | Type     | Required | Description          |
|-----------|----------|----------|----------------------|
| storeId   | guid     | No       | Filter by store      |
| from      | datetime | Yes      | Start date           |
| to        | datetime | Yes      | End date             |

**Success Response (200):** Returns aggregated sales data for the period.

---

### GET /reports/stock

Generate a stock report.

**Query Parameters:**

| Parameter   | Type | Default | Description          |
|------------|------|---------|----------------------|
| warehouseId| guid | null    | Filter by warehouse  |

**Success Response (200):** Returns stock summary data.

---

### GET /reports/vat

Generate a VAT report for a specific period.

**Query Parameters:**

| Parameter | Type | Default       | Description    |
|-----------|------|---------------|----------------|
| year      | int  | current year  | Tax year       |
| month     | int  | current month | Tax month      |

**Success Response (200):** Returns VAT report data including output VAT, input VAT, and net liability.

---

## 14. Licensing

**Base path:** `/api/v1/license`

### GET /license/status

Get current license status.

- **Auth:** Anonymous

**Success Response (200):** Returns license validation information including expiry, feature limits, and activation status.

---

### POST /license/activate

Activate a license key.

- **Auth:** Anonymous
- **Rate Limit:** `auth` (10/min)

**Request Body:**

```json
{
  "licenseKey": "XXXX-XXXX-XXXX-XXXX",
  "companyName": "My Retail Company",
  "contactEmail": "admin@mycompany.ge"
}
```

| Field        | Type   | Required | Description           |
|-------------|--------|----------|-----------------------|
| licenseKey  | string | Yes      | License key to activate|
| companyName | string | Yes      | Company name          |
| contactEmail| string | No       | Contact email         |

**Success Response (200):**

```json
{
  "activated": true,
  "companyName": "My Retail Company",
  "expiresAt": "2027-06-20T00:00:00+00:00",
  "maxUsers": 50,
  "maxStores": 10
}
```

---

### POST /license/deactivate

Deactivate the current license.

- **Auth:** Required (Roles: `super_admin`, `company_admin`)

**Request Body:** None

**Success Response (200):** Empty response.

---

### POST /license/renew

Renew an existing license.

- **Auth:** Required (Roles: `super_admin`, `company_admin`)

**Request Body:**

```json
{
  "licenseKey": "XXXX-XXXX-XXXX-XXXX"
}
```

**Success Response (200):** Returns updated license information.

---

## 15. Updates

**Base path:** `/api/v1/updates`

### GET /updates/latest

Get the latest application version information.

- **Auth:** Not specified (inherits default)

**Success Response (200):**

```json
{
  "version": "1.2.0",
  "downloadUrl": "https://releases.example.com/erp/1.2.0/setup.exe",
  "releaseNotes": "Bug fixes and performance improvements",
  "sha256": "abc123def456...",
  "fileSize": 0
}
```

---

## 16. Health Check

### GET /health

Application health check endpoint.

- **Auth:** Anonymous
- **Response:** `Healthy` or `Unhealthy`

Checks database connectivity via EF Core `DbContextCheck`.

---

## Appendix: Swagger / OpenAPI

When running in Development mode or with `Swagger:Enabled = true`, the interactive API documentation is available at:

- **Swagger UI:** `http://localhost:5000/swagger`
- **OpenAPI JSON:** `http://localhost:5000/swagger/v1/swagger.json`

The Swagger UI includes JWT authentication support. Use the "Authorize" button and enter your Bearer token.
