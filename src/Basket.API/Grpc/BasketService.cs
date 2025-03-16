using System.Diagnostics; // For ActivitySource, Stopwatch
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using eShop.Basket.API.Repositories;
using eShop.Basket.API.Extensions;
using eShop.Basket.API.Model;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace eShop.Basket.API.Grpc;

public class BasketService(
    IBasketRepository repository,
    ILogger<BasketService> logger) : Basket.BasketBase
{
    // ActivitySource for tracing spans
    private static readonly ActivitySource activitySource = new("BasketService");

    // Meter for metrics
    private static readonly Meter meter = new("BasketService");

    // Counter for total basket requests processed (with tag "endpoint")
    private static readonly Counter<long> requestCounter = meter.CreateCounter<long>(
        "basket_request_total", 
        description: "Total number of basket requests processed");

    // Histogram for recording request durations in seconds
    private static readonly Histogram<double> requestDuration = meter.CreateHistogram<double>(
        "basket_request_duration_seconds", 
        description: "Histogram of basket request durations in seconds");

    // Counter for errors
    private static readonly Counter<long> errorCounter = meter.CreateCounter<long>(
        "basket_error_total", 
        description: "Total number of basket errors");

    public override async Task<CustomerBasketResponse> GetBasket(GetBasketRequest request, ServerCallContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        using var activity = activitySource.StartActivity("GetBasket", ActivityKind.Server);
        try
        {
            var userId = context.GetUserIdentity();
            if (string.IsNullOrEmpty(userId))
            {
                errorCounter.Add(1, [new("endpoint", "GetBasket")]);
                return new();
            }

            // Add tags to the span
            activity?.SetTag("basket.userId", RedactId(userId));
            activity?.SetTag("grpc.method", context.Method);

            logger.LogDebug("Begin GetBasketById call from method {Method} for basket id {Id}", context.Method, userId);

            var data = await repository.GetBasketAsync(userId);

            if (data is not null)
            {
                activity?.SetTag("basket.itemCount", data.Items.Count);
                return MapToCustomerBasketResponse(data);
            }

            return new();
        }
        finally
        {
            stopwatch.Stop();
            // Record metrics with disambiguated array of tags
            requestDuration.Record(stopwatch.Elapsed.TotalSeconds, [new("endpoint", "GetBasket")]);
            requestCounter.Add(1, [new("endpoint", "GetBasket")]);
        }
    }

    public override async Task<CustomerBasketResponse> UpdateBasket(UpdateBasketRequest request, ServerCallContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        using var activity = activitySource.StartActivity("UpdateBasket-Logic", ActivityKind.Server);
        try
        {
            var userId = context.GetUserIdentity();
            if (string.IsNullOrEmpty(userId))
            {
                errorCounter.Add(1, [new("endpoint", "UpdateBasket")]);
                ThrowNotAuthenticated();
            }

            activity?.SetTag("basket.userId", RedactId(userId));
            activity?.SetTag("grpc.method", context.Method);
            activity?.SetTag("grpc.requestItemsCount", request.Items.Count);

            logger.LogDebug("Begin UpdateBasket call from method {Method} for basket id {Id}", context.Method, userId);

            var customerBasket = MapToCustomerBasket(userId, request);
            var response = await repository.UpdateBasketAsync(customerBasket);
            if (response is null)
            {
                errorCounter.Add(1, [new("endpoint", "UpdateBasket")]);
                ThrowBasketDoesNotExist(userId);
            }

            activity?.SetTag("basket.updatedItemCount", response.Items.Count);
            return MapToCustomerBasketResponse(response);
        }
        finally
        {
            stopwatch.Stop();
            requestDuration.Record(stopwatch.Elapsed.TotalSeconds, [new("endpoint", "UpdateBasket")]);
            requestCounter.Add(1, [new("endpoint", "UpdateBasket")]);
        }
    }

    public override async Task<DeleteBasketResponse> DeleteBasket(DeleteBasketRequest request, ServerCallContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        using var activity = activitySource.StartActivity("DeleteBasket-Logic", ActivityKind.Server);
        try
        {
            var userId = context.GetUserIdentity();
            if (string.IsNullOrEmpty(userId))
            {
                errorCounter.Add(1, [new("endpoint", "DeleteBasket")]);
                ThrowNotAuthenticated();
            }

            activity?.SetTag("basket.userId", RedactId(userId));
            activity?.SetTag("grpc.method", context.Method);

            await repository.DeleteBasketAsync(userId);
            activity?.SetTag("basket.deleteStatus", "success");

            return new();
        }
        finally
        {
            stopwatch.Stop();
            requestDuration.Record(stopwatch.Elapsed.TotalSeconds, [new("endpoint", "DeleteBasket")]);
            requestCounter.Add(1, [new("endpoint", "DeleteBasket")]);
        }
    }

    [DoesNotReturn]
    private static void ThrowNotAuthenticated() =>
        throw new RpcException(new Status(StatusCode.Unauthenticated, "The caller is not authenticated."));

    [DoesNotReturn]
    private static void ThrowBasketDoesNotExist(string userId) =>
        throw new RpcException(new Status(StatusCode.NotFound, $"Basket with buyer id {userId} does not exist"));

    private static CustomerBasketResponse MapToCustomerBasketResponse(CustomerBasket customerBasket)
    {
        var response = new CustomerBasketResponse();
        foreach (var item in customerBasket.Items)
        {
            response.Items.Add(new BasketItem()
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
            });
        }
        return response;
    }

    private static CustomerBasket MapToCustomerBasket(string userId, UpdateBasketRequest customerBasketRequest)
    {
        var response = new CustomerBasket
        {
            BuyerId = userId
        };
        foreach (var item in customerBasketRequest.Items)
        {
            response.Items.Add(new()
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
            });
        }
        return response;
    }

    private static string RedactId(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex > 1)
        {
            return email[0] + new string('*', atIndex - 1) + email.Substring(atIndex);
        }
        return "REDACTED";
    }
}
