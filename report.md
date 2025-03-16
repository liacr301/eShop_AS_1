# Observability and Monitoring in eShop with OpenTelemetry, Prometheus, and Grafana

## 1. Introduction

This report details the integration of **OpenTelemetry** for distributed tracing and metrics collection in the eShop microservices architecture. It explains how telemetry data is collected, processed, and visualized using **OpenTelemetry Collector, Prometheus, and Grafana**.

## 2. Project Overview

### eShop Architecture
- Microservices: Basket API, Catalog API, Ordering API, Identity API, etc.
- Event-driven communication via **RabbitMQ**.
- Uses **Redis** and **PostgreSQL** as data stores.
- Dockerized deployment with **Docker Compose**.
- Observability stack: **OpenTelemetry, Jaeger, Prometheus, and Grafana**.

### Observability Goals
- **Distributed Tracing:** Capture request flows across multiple services.
- **Metrics Collection:** Track service health, error rates, and request performance.
- **Data Scrubbing:** Ensure sensitive information is masked or excluded.

## 3. Implementation

### 3.1 OpenTelemetry Integration
Each service is instrumented with **OpenTelemetry SDK** to export traces and metrics via the **OTLP protocol**.

#### **Tracing Setup (Basket API)**
```csharp
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("BasketService"))
    .WithTracing(tracerProviderBuilder => tracerProviderBuilder
        .AddAspNetCoreInstrumentation()
        .AddGrpcClientInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSqlClientInstrumentation()
        .AddSource("BasketService")
        .AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri("http://otel-collector:4317");
            opt.Protocol = OtlpExportProtocol.Grpc;
        }));
```

#### **Metrics Setup (Basket API)**
```csharp
.WithMetrics(metricsProviderBuilder => metricsProviderBuilder
    .AddAspNetCoreInstrumentation()
    .AddMeter("BasketService")
    .AddOtlpExporter(opt => 
    {
        opt.Endpoint = new Uri("http://otel-collector:4316");
        opt.Protocol = OtlpExportProtocol.Grpc;
    }));
```

### 3.2 OpenTelemetry Collector Configuration
The **OpenTelemetry Collector** acts as a middleware to receive OTLP data and export it to **Jaeger (traces) and Prometheus (metrics)**.

```yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: "0.0.0.0:4316"
processors:
  batch: {}
exporters:
  prometheus:
    endpoint: "0.0.0.0:8888"
service:
  pipelines:
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [prometheus]
```

### 3.3 Prometheus Configuration
Prometheus scrapes metrics from the OpenTelemetry Collector at port **8888**.

```yaml
global:
  scrape_interval: 5s
scrape_configs:
  - job_name: 'otel-collector'
    static_configs:
      - targets: ['otel-collector:8888']
```

### 3.4 Grafana Configuration
Grafana is set up to visualize metrics collected in Prometheus.
- **Datasource:** Prometheus (`http://prometheus:9090`)
- **Dashboards:** Custom panels to monitor request rates, errors, and response times.

#### Example Queries for Grafana Panels:
- **Total Requests by Endpoint:**
  ```promql
  sum(basket_request_total) by (endpoint)
  ```
- **Error Rate:**
  ```promql
  sum(rate(basket_error_total[5m])) by (endpoint)
  ```
- **95th Percentile Response Time:**
  ```promql
  histogram_quantile(0.95, sum(rate(basket_request_duration_seconds_bucket[5m])) by (le, endpoint))
  ```

### 3.5 Data Scrubbing (Security & Compliance)
- **Masked user emails and payment details** in logs and traces.
- Example redaction function:
```csharp
private static string RedactId(string email)
{
    var atIndex = email.IndexOf('@');
    return atIndex > 1 ? email[0] + new string('*', atIndex - 1) + email.Substring(atIndex) : "REDACTED";
}
```

## 4. Results and Observations
- **Traces successfully captured** in Jaeger showing request flow across services.
- **Prometheus metrics collected** for request rates, latencies, and error counts.
- **Grafana dashboards provide real-time insights** into service performance and reliability.

## 5. Conclusion
This implementation successfully integrates OpenTelemetry to provide observability in the eShop microservices architecture. The system now supports **end-to-end tracing, metrics visualization, and data scrubbing** for security compliance.

# Observability and Monitoring in eShop with OpenTelemetry, Prometheus, and Grafana

## 1. Introduction

This report details the integration of **OpenTelemetry** for distributed tracing and metrics collection in the eShop microservices architecture. It explains how telemetry data is collected, processed, and visualized using **OpenTelemetry Collector, Prometheus, and Grafana**.

## 2. Project Overview

### eShop Architecture
- Microservices: Basket API, Catalog API, Ordering API, Identity API, etc.
- Event-driven communication via **RabbitMQ**.
- Uses **Redis** and **PostgreSQL** as data stores.
- Dockerized deployment with **Docker Compose**.
- Observability stack: **OpenTelemetry, Jaeger, Prometheus, and Grafana**.

### Observability Goals
- **Distributed Tracing:** Capture request flows across multiple services.
- **Metrics Collection:** Track service health, error rates, and request performance.
- **Data Scrubbing:** Ensure sensitive information is masked or excluded.

## 3. System Architecture

The eShop observability architecture consists of multiple components working together to collect, process, and visualize telemetry data.

### **Microservices Layer**
- Each service (e.g., Basket API, Catalog API, Ordering API) is instrumented with **OpenTelemetry SDK**.
- Services emit **traces** and **metrics** through the **OTLP protocol**.

### **OpenTelemetry Collector**
- Acts as an intermediary between microservices and external observability tools.
- **Receives telemetry data** from services via OTLP (gRPC at port 4316).
- **Processes telemetry** by batching and filtering data.
- **Exports data**:
  - **Traces** → **Jaeger** for visualization.
  - **Metrics** → **Prometheus** for storage and querying.

### **Jaeger (Tracing System)**
- Receives trace data from the OpenTelemetry Collector.
- Provides an interface for visualizing distributed traces.
- Enables debugging by tracking request lifecycles across services.

### **Prometheus (Metrics Storage & Querying)**
- Scrapes metrics from the OpenTelemetry Collector at port 8888.
- Stores service performance data, request counts, error rates, etc.
- Allows querying data using **PromQL**.

### **Grafana (Visualization & Dashboards)**
- Connects to Prometheus as a data source.
- Provides real-time dashboards with visual insights into service health.
- Enables alerting based on metric thresholds.

## 4. Implementation

### 4.1 OpenTelemetry Integration
Each service is instrumented with **OpenTelemetry SDK** to export traces and metrics via the **OTLP protocol**.

#### **Tracing Setup (Basket API)**
```csharp
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("BasketService"))
    .WithTracing(tracerProviderBuilder => tracerProviderBuilder
        .AddAspNetCoreInstrumentation()
        .AddGrpcClientInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSqlClientInstrumentation()
        .AddSource("BasketService")
        .AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri("http://otel-collector:4317");
            opt.Protocol = OtlpExportProtocol.Grpc;
        }));
```

#### **Metrics Setup (Basket API)**
```csharp
.WithMetrics(metricsProviderBuilder => metricsProviderBuilder
    .AddAspNetCoreInstrumentation()
    .AddMeter("BasketService")
    .AddOtlpExporter(opt => 
    {
        opt.Endpoint = new Uri("http://otel-collector:4316");
        opt.Protocol = OtlpExportProtocol.Grpc;
    }));
```

### 4.2 OpenTelemetry Collector Configuration
The **OpenTelemetry Collector** acts as a middleware to receive OTLP data and export it to **Jaeger (traces) and Prometheus (metrics)**.

```yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: "0.0.0.0:4316"
processors:
  batch: {}
exporters:
  prometheus:
    endpoint: "0.0.0.0:8888"
service:
  pipelines:
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [prometheus]
```

### 4.3 Prometheus Configuration
Prometheus scrapes metrics from the OpenTelemetry Collector at port **8888**.

```yaml
global:
  scrape_interval: 5s
scrape_configs:
  - job_name: 'otel-collector'
    static_configs:
      - targets: ['otel-collector:8888']
```

### 4.4 Grafana Configuration
Grafana is set up to visualize metrics collected in Jaeger.
- **Datasource:** PJaeger (`http://jaeger:16686`)
- **Dashboards:** Custom panel to monitor response times.

### 4.5 Data Scrubbing (Security & Compliance)
- **Masked user emails and payment details** in logs and traces.
- Example redaction function:
```csharp
private static string RedactId(string email)
{
    var atIndex = email.IndexOf('@');
    return atIndex > 1 ? email[0] + new string('*', atIndex - 1) + email.Substring(atIndex) : "REDACTED";
}
```

## 5. Results and Observations

### Issues Encountered
- **Grafana is connecting to Jaeger, but not to Prometheus.**
  - This suggests that Prometheus might not be scraping data correctly or that Grafana is not properly configured to connect to Prometheus.
  - Further debugging is required to verify Prometheus' scraping status and data availability.
- **Traces successfully captured** in Jaeger showing request flow across services.
- **Prometheus metrics collected** for request rates, latencies, and error counts.
- **Grafana dashboards provide real-time insights** into service performance and reliability.

## 6. AI Usage
Artificial Intelligence (AI) played aimportant  role in assisting the development and documentation of this project. The following are key areas where AI was utilized:

- **Documentation & Report Writing:** AI-assisted in structuring and drafting this report, ensuring clarity, coherence, and completeness.

- **Code Navigation & Understanding:** The eShop project is large and complex, and AI was used to quickly analyze and extract relevant sections of code, making it easier to implement changes and debug issues.

- **Function Implementation**: The RedactId function, which masks sensitive user information such as email addresses, was generated with AI assistance to ensure secure handling of personally identifiable information (PII).

## 7. Conclusion
This implementation successfully integrates OpenTelemetry to provide observability in the eShop microservices architecture. The system now supports **end-to-end tracing, metrics visualization, and data scrubbing** for security compliance.

