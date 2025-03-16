# eShop Observability with OpenTelemetry, Prometheus, Jaeger and Grafana

This guide explains how to build and run the eShop environment with OpenTelemetry tracing and metrics, set up the OpenTelemetry Collector and exporters, and configure Grafana to visualize the collected data.

---

## 1. How to Build and Run the eShop Environment with Your Changes

### Prerequisites

- Docker & Docker Compose installed
- .NET 8 SDK installed
- Git installed

### Steps to Run the eShop Application

1. Clone the eShop repository with your changes:

   ```bash
   git clone https://github.com/liacr301/eShop_AS_1
   cd eshop
   ```

2. Build and run the eShop services:

   ```bash
   docker compose up -d
   ```

   This will start all microservices, the OpenTelemetry Collector, Prometheus, and Grafana.

3. If running locally, you can start the E-Shop manually:

   ```bash
   dotnet build
   dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj
   ```

---

## 2. Steps to Configure and Launch the OpenTelemetry Collectors/Exporters

The eShop services export telemetry data using OpenTelemetry's OTLP exporter. The OpenTelemetry Collector is used to process and forward this data to Prometheus and Jaeger.

### OpenTelemetry Collector Configuration

The **otel-collector-config.yaml** file is already included in the repository and configured to:

- Receive telemetry data via OTLP from the eShop services
- Export traces to **Jaeger**
- Export metrics to **Prometheus**

To start the OpenTelemetry Collector manually:

```bash
docker compose up -d otel-collector
```

To verify the collector is running and receiving data, check the logs:

```bash
docker logs otel-collector -f
```

### Verify Exported Data

- **Traces (Jaeger)**: Open [http://localhost:16686](http://localhost:16686) and search for traces.
- **Metrics (Prometheus)**: Open [http://localhost:9090](http://localhost:9090) and run queries like:
  ```promql
  sum(rate(basket_request_total[5m])) by (endpoint)
  ```

---

## 3. Instructions to Set Up or View the Grafana Dashboard

### Accessing Grafana

Grafana is included in the Docker setup. You can access it at:

[http://localhost:3000](http://localhost:3000)

Default credentials:

```
Username: admin
Password: admin
```

### Adding Prometheus as a Data Source

1. Go to **Configuration** > **Data Sources**.
2. Click **Add data source** and select **Prometheus**.
3. Set the URL to:
   ```
   http://prometheus:9090
   ```
   or, if running locally,
   ```
   http://localhost:9090
   ```
4. Click **Save & Test**.

### Importing a Dashboard

1. Go to **Dashboards** > **Import**.
2. Upload the provided JSON file (`grafana-dashboard.json`) or use an existing template.
3. Select **Prometheus** as the data source.
4. Click **Import**.

### Example Queries for Dashboards

- **Total Requests per Endpoint:**
  ```promql
  sum(basket_request_total) by (endpoint)
  ```
- **Error Rate per Endpoint:**
  ```promql
  sum(rate(basket_error_total[5m])) by (endpoint)
  ```
- **95th Percentile Response Time:**
  ```promql
  histogram_quantile(0.95, sum(rate(basket_request_duration_seconds_bucket[5m])) by (le, endpoint))
  ```

### Testing Your Setup

To generate test data, you can send requests to the Basket API:

```bash
curl -X POST http://localhost:5103/api/v1/basket -H "Content-Type: application/json" -d '{ "userId": "123", "items": [{"productId": 1, "quantity": 2}] }'
```

Now, go to Grafana and check your dashboard for real-time data.

---


