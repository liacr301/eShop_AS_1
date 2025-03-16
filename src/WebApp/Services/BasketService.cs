using System.Diagnostics;
using eShop.Basket.API.Grpc;
using GrpcBasketItem = eShop.Basket.API.Grpc.BasketItem;
using GrpcBasketClient = eShop.Basket.API.Grpc.Basket.BasketClient;

namespace eShop.WebApp.Services;

public class BasketService(GrpcBasketClient basketClient)
{
    private static readonly ActivitySource ActivitySource = new("WebApp.BasketService.Client");

    public async Task<IReadOnlyCollection<BasketQuantity>> GetBasketAsync()
    {
        using var activity = ActivitySource.StartActivity("GetBasket-ClientLogic");

        activity?.SetTag("client.call", "GetBasketAsync");

        var result = await basketClient.GetBasketAsync(new());
        return MapToBasket(result);
    }

    public async Task DeleteBasketAsync()
    {
        using var activity = ActivitySource.StartActivity("DeleteBasket-ClientLogic");
        activity?.SetTag("client.call", "DeleteBasketAsync");

        await basketClient.DeleteBasketAsync(new DeleteBasketRequest());
    }

    public async Task UpdateBasketAsync(IReadOnlyCollection<BasketQuantity> basket)
    {
        using var activity = ActivitySource.StartActivity("UpdateBasket-ClientLogic");
        activity?.SetTag("client.call", "UpdateBasketAsync");
        activity?.SetTag("client.itemCount", basket.Count);

        var updatePayload = new UpdateBasketRequest();

        foreach (var item in basket)
        {
            var updateItem = new GrpcBasketItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
            };
            updatePayload.Items.Add(updateItem);
        }

        await basketClient.UpdateBasketAsync(updatePayload);
    }

    private static List<BasketQuantity> MapToBasket(CustomerBasketResponse response)
{
    using var activity = ActivitySource.StartActivity("MapToBasklientLogic");

    activity?.SetTag("client.itemCount", response.Items.Count);

    var result = new List<BasketQuantity>();
    foreach (var item in response.Items)
    {
        result.Add(new BasketQuantity(item.ProductId, item.Quantity));
    }

    return result;
}

}

public record BasketQuantity(int ProductId, int Quantity);
