namespace Core.Domain;

public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    public string Id { get; }

    public string CustomerId { get; }

    public OrderStatus Status { get; private set; }

    public IReadOnlyList<OrderLine> Lines =>
        _lines.AsReadOnly();

    public decimal Total =>
        _lines.Sum(line => line.Price * line.Quantity);

    private Order(
        string id,
        string customerId)
    {
        Id = id;
        CustomerId = customerId;
        Status = OrderStatus.Draft;
    }

    public static Order Create(
        string id,
        string customerId)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException(
                "Ідентифікатор замовлення не може бути порожнім",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new ArgumentException(
                "Ідентифікатор клієнта не може бути порожнім",
                nameof(customerId));
        }

        return new Order(
            id.Trim(),
            customerId.Trim());
    }

    public void AddLine(
        string productId,
        string name,
        decimal price,
        int quantity)
    {
        if (Status != OrderStatus.Draft)
        {
            throw new InvalidOperationException(
                "Додавати товари можна лише до чернетки");
        }

        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException(
                "Ідентифікатор товару не може бути порожнім",
                nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Назва товару не може бути порожньою",
                nameof(name));
        }

        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(price),
                price,
                "Ціна не може бути від'ємною");
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                quantity,
                "Кількість повинна бути більшою за нуль");
        }

        _lines.Add(
            new OrderLine(
                productId.Trim(),
                name.Trim(),
                price,
                quantity));
    }

    public void Confirm()
    {
        OrderStatus nextStatus = Status switch
        {
            OrderStatus.Draft when _lines.Count > 0
                => OrderStatus.Confirmed,

            OrderStatus.Draft
                => throw new InvalidOperationException(
                    "Не можна підтвердити порожнє замовлення"),

            OrderStatus.Confirmed
                => throw new InvalidOperationException(
                    "Замовлення вже підтверджене"),

            OrderStatus.Cancelled
                => throw new InvalidOperationException(
                    "Скасоване замовлення не можна підтвердити"),

            _ => throw new InvalidOperationException(
                "Невідомий стан замовлення")
        };

        Status = nextStatus;
    }

    public void Cancel()
    {
        OrderStatus nextStatus = Status switch
        {
            OrderStatus.Draft
                => OrderStatus.Cancelled,

            OrderStatus.Confirmed
                => OrderStatus.Cancelled,

            OrderStatus.Cancelled
                => throw new InvalidOperationException(
                    "Замовлення вже скасоване"),

            _ => throw new InvalidOperationException(
                "Невідомий стан замовлення")
        };

        Status = nextStatus;
    }
}