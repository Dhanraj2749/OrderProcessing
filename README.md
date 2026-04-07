# Event-Driven Order Processing System

A cloud-native, event-driven microservices system built with **C# and .NET 8** — simulating a real-world e-commerce order pipeline where services communicate exclusively through events.

## Architecture

```
                    ┌─────────────────────────────┐
                    │   REST API (ASP.NET Core)    │
                    │   POST /api/orders           │
                    │   GET  /api/orders/{id}      │
                    │   GET  /api/orders/stats     │
                    └─────────────┬───────────────┘
                                  │ OrderPlacedEvent
                                  ▼
                    ┌─────────────────────────────┐
                    │      In-Memory Event Bus     │
                    │  (Production: Azure Service  │
                    │   Bus / RabbitMQ)            │
                    └──┬──────────┬───────────┬───┘
                       │          │           │
              ┌────────▼──┐  ┌────▼────┐  ┌──▼──────────┐
              │  Payment  │  │Inventory│  │Notification │
              │  Service  │  │ Service │  │  Service    │
              └────────┬──┘  └────┬────┘  └─────────────┘
                       │          │
              PaymentProcessed  InventoryReserved
                       │          │
                       └────┬─────┘
                            │ OrderShipped
                            ▼
                    Order Status Updated

Event Flow:
1. POST /api/orders → OrderPlacedEvent published
2. PaymentService listens → processes payment → PaymentProcessedEvent
3. InventoryService listens → reserves stock → InventoryReservedEvent + OrderShippedEvent
4. NotificationService listens → sends Email/SMS at each step
5. OrderService listens to all → updates order status
```

## Tech Stack

| Layer | Technology |
|-------|-----------|
| API | ASP.NET Core 8, Swagger |
| Event Bus | In-memory (Azure Service Bus ready) |
| Services | .NET BackgroundService workers |
| Logging | Serilog structured logging |
| Container | Docker |
| Orchestration | Kubernetes (AKS ready) |

## Getting Started

### Run locally
```bash
cd services/OrderService
dotnet run
```
Open Swagger: http://localhost:5000/swagger

### Run with Docker
```bash
docker build -t order-processing .
docker run -p 8080:8080 order-processing
```

### Deploy to Kubernetes (AKS)
```bash
kubectl apply -f k8s/deployment.yaml
```

## API Usage

### Place an order
```json
POST /api/orders
{
  "customerId": "CUST-001",
  "items": [
    {
      "productId": "PROD-001",
      "productName": "Laptop",
      "quantity": 1,
      "unitPrice": 999.99
    }
  ]
}
```

### Watch the event flow in terminal:
```
[PaymentService]      Processing payment for Order abc123 — Amount: $999.99
[PaymentService]      Payment APPROVED for Order abc123 | TxnId: TXN-A1B2C3D4
[InventoryService]    Reserved 1x PROD-001
[InventoryService]    Order abc123 shipped
[NotificationService] EMAIL → Customer CUST-001: 'Order received'
[NotificationService] EMAIL → Customer CUST-001: 'Payment confirmed'
[NotificationService] SMS  → Customer CUST-001: 'Order shipped! Tracking: SHIP-X1Y2Z3'
```

## Key Features

- **Event-driven architecture** — services are fully decoupled, communicate only via events
- **4 independent services** — Order, Payment, Inventory, Notification
- **Saga pattern** — distributed transaction across services with compensating actions
- **Automatic saga rollback** — order cancelled if payment or inventory fails
- **Kubernetes ready** — deployment manifest with HPA auto-scaling (2-10 replicas)
- **Health checks** — liveness and readiness probes for AKS
- **Structured logging** — Serilog with full event trace across services
- **Azure Service Bus ready** — swap InMemoryEventBus with one line

## Production Swap Guide

| Current (Local) | Production |
|----------------|-----------|
| InMemoryEventBus | Azure Service Bus |
| In-memory order store | Azure SQL / CosmosDB |
| Simulated payment | Stripe / Azure Payment Services |
| Simulated notifications | SendGrid / Azure Communication Services |
| Docker local | Azure Kubernetes Service (AKS) |
