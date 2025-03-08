using System.Diagnostics; // Para ActivitySource
using System.Diagnostics.CodeAnalysis;
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
    // Fonte de spans customizados
    private static readonly ActivitySource ActivitySource = new("Basket.API.Grpc.BasketService");

    public override async Task<CustomerBasketResponse> GetBasket(GetBasketRequest request, ServerCallContext context)
    {
        // Cria um span específico para lógica de "GetBasket"
        using var activity = ActivitySource.StartActivity("GetBasket-Logic");

        var userId = context.GetUserIdentity();
        if (string.IsNullOrEmpty(userId))
        {
            return new();
        }

        // Exemplo: adicionar tag mascarando o userId se for email
        activity?.SetTag("basket.userId", RedactEmail(userId)); 
        activity?.SetTag("grpc.method", context.Method); // Qual método gRPC foi chamado

        logger.LogDebug("Begin GetBasketById call from method {Method} for basket id {Id}", context.Method, userId);

        var data = await repository.GetBasketAsync(userId);

        if (data is not null)
        {
            // Adiciona a quantidade de itens como tag
            activity?.SetTag("basket.itemCount", data.Items.Count);

            return MapToCustomerBasketResponse(data);
        }

        return new();
    }

    public override async Task<CustomerBasketResponse> UpdateBasket(UpdateBasketRequest request, ServerCallContext context)
    {
        using var activity = ActivitySource.StartActivity("UpdateBasket-Logic");

        var userId = context.GetUserIdentity();
        if (string.IsNullOrEmpty(userId))
        {
            ThrowNotAuthenticated();
        }

        activity?.SetTag("basket.userId", RedactEmail(userId));
        activity?.SetTag("grpc.method", context.Method);
        activity?.SetTag("grpc.requestItemsCount", request.Items.Count);  // Informa quantos itens foram enviados

        logger.LogDebug("Begin UpdateBasket call from method {Method} for basket id {Id}", context.Method, userId);

        var customerBasket = MapToCustomerBasket(userId, request);
        var response = await repository.UpdateBasketAsync(customerBasket);
        if (response is null)
        {
            ThrowBasketDoesNotExist(userId);
        }

        // Se chegou aqui, a basket foi atualizada com sucesso
        activity?.SetTag("basket.updatedItemCount", response.Items.Count);

        return MapToCustomerBasketResponse(response);
    }

    public override async Task<DeleteBasketResponse> DeleteBasket(DeleteBasketRequest request, ServerCallContext context)
    {
        using var activity = ActivitySource.StartActivity("DeleteBasket-Logic");

        var userId = context.GetUserIdentity();
        if (string.IsNullOrEmpty(userId))
        {
            ThrowNotAuthenticated();
        }

        activity?.SetTag("basket.userId", RedactEmail(userId));
        activity?.SetTag("grpc.method", context.Method);

        await repository.DeleteBasketAsync(userId);
        // Aqui pode definir alguma tag de status, se quiser
        activity?.SetTag("basket.deleteStatus", "success");

        return new();
    }

    [DoesNotReturn]
    private static void ThrowNotAuthenticated() => throw new RpcException(new Status(StatusCode.Unauthenticated, "The caller is not authenticated."));

    [DoesNotReturn]
    private static void ThrowBasketDoesNotExist(string userId) => throw new RpcException(new Status(StatusCode.NotFound, $"Basket with buyer id {userId} does not exist"));

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

    // >>> Função auxiliar para mascarar
    private static string RedactEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex > 1)
        {
            return email[0] + new string('*', atIndex - 1) + email.Substring(atIndex);
        }
        return "REDACTED";
    }
}
