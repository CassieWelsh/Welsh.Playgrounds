using GovnoBot;
using VkNet;
using VkNet.Enums.StringEnums;
using VkNet.Model;

var vk = new VkApi();
vk.Authorize(new() { AccessToken = "vk1.a.uWwDdrB4HG37L-8VpyMstIPStQy1IHqdCmEIdLb6aKnJG5n0Nj8mYvdlU9kJ7dY9vvwdWGbQ6SIslPiRcAtGv9QrvS_oxIUvPPeDLGESrK8E2-_RK-V7H0_PEfw7AZk58lUMLbWWsbhpsEWgapn-3vTRu4WSqiIiEQnMjuEQCmWG0xw4Mm6bXIWeqsaO65MqY_yDonDq7ZxSAbR4t6CXYQ" });

var server = vk.Groups.GetLongPollServer(225796199);
Console.WriteLine("Connected");
var @params = new BotsLongPollHistoryParams() { Key = server.Key, Server = server.Server, Ts = server.Ts, Wait = 10 };

const string DEFAULT_TEXT = "Выберите функционал";
var currentState = BotStates.Default;

var orders = new List<CartItem>();

while (true)
{
    var poll = await vk.Groups.GetBotsLongPollHistoryAsync(@params);

    if (poll.Updates.Count > 0)
    {
        @params.Ts += (ulong)poll.Updates.Count;
        try
        {
            foreach (var update in poll.Updates)
            {
                var message = ((MessageNew)update.Instance).Message;

                var attachments = message.Attachments;

                if (message.Attachments.Count == 1 && message.Attachments[0].Instance is Market product)
                {
                    await AddProductAction(vk, currentState, message, product);
                    continue;
                }

                switch (currentState)
                {
                    case BotStates.FAQ:
                        await FAQAction(vk, message);
                        continue;
                    case BotStates.Cart:
                        await CartAction(vk, message);
                        continue;
                    case BotStates.Refund:
                        if (int.TryParse(message.Text, out _))
                            vk.Messages.Send(new() { PeerId = message.PeerId, RandomId = 0, Message = "Запрос на возврат отправлен. Менеджер свяжется с вами в ближайшее время" });
                        else
                            vk.Messages.Send(new() { PeerId = message.PeerId, RandomId = 0, Message = "Некорректный номер заказа" });
                        currentState = BotStates.Default;
                        break;
                    case BotStates.AddressConfirmation:
                        if (message.Text == "Отмена")
                        {
                            await vk.Messages.SendAsync(new()
                            {
                                PeerId = message.PeerId,
                                RandomId = 0,
                                Message = $"Заказ отменён",
                            });
                            currentState = BotStates.Default;
                            break;
                        }

                        var paymentGuid = Guid.NewGuid().ToString();
                        await vk.Messages.SendAsync(new()
                        {
                            PeerId = message.PeerId,
                            RandomId = 0,
                            Message = $"Оплатите заказ в течении 15 минут и нажмите кнопку далее\nhttps://payment-url-example/{paymentGuid}",
                            Keyboard = new() { OneTime = true, Buttons = [[CreateButton("Далее", KeyboardButtonColor.Default)]] }
                        });
                        currentState = BotStates.Payment;
                        continue;
                    case BotStates.Payment:
                        orders.RemoveAll(_ => true);
                        await vk.Messages.SendAsync(new()
                        {
                            PeerId = message.PeerId,
                            RandomId = 0,
                            Message = "Оплата прошла успешно"
                        });
                        currentState = BotStates.Default;
                        break;
                    default:
                        break;
                }

                switch (message.Text.ToLower())
                {
                    case "частые вопросы":
                        await FaqAction(vk, message);
                        break;
                    case "просмотреть корзину":
                        await PrintCart(vk, message);
                        break;
                    case "возврат":
                        await Refund(vk, message);
                        break;
                    case "оформить заказ":
                        await MakeOrder(vk, message);
                        break;
                    default:
                        await DefaultAction(vk, message);
                        break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }
}

async Task MakeOrder(VkApi vk, Message message)
{
    if (orders.Count <= 0)
    {
        await vk.Messages.SendAsync(new()
        {
            PeerId = message.PeerId,
            RandomId = 0,
            Message = "Корзина пуста"
        });
        await DefaultAction(vk, message);
        return;
    }

    await vk.Messages.SendAsync(new()
    {
        PeerId = message.PeerId,
        RandomId = 0,
        Message = "Уточните адрес доставки",
        Keyboard = new()
        {
            OneTime = true,
            Buttons = [[CreateButton("Отмена", KeyboardButtonColor.Negative)]]
        }
    });
    currentState = BotStates.AddressConfirmation;
}

async Task Refund(VkApi vk, Message message)
{
    await vk.Messages.SendAsync(new()
    {
        PeerId = message.PeerId,
        Message = "Укажите номер заказа",
        RandomId = message.RandomId,
        Keyboard = new()
        {
            OneTime = true,
            Buttons = [[CreateButton("Назад", KeyboardButtonColor.Negative)]]
        }
    });
    currentState = BotStates.Refund;
}

async Task CartAction(VkApi vk, Message message)
{
    switch (message.Text)
    {
        case "Убрать товар":

            var buttons = new List<List<MessageKeyboardButton>>(orders.Count + 1);
            for (int i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                buttons.Add([CreateButton($"{i + 1}", KeyboardButtonColor.Primary)]);
            }
            buttons.Add([CreateButton("Назад", KeyboardButtonColor.Negative)]);

            await vk.Messages.SendAsync(new()
            {
                PeerId = message.PeerId,
                Message = "Выберите товар",
                RandomId = 0,
                Keyboard = new() { OneTime = true, Buttons = buttons }
            });
            break;
        case "Назад":
            currentState = BotStates.Default;
            await DefaultAction(vk, message);
            break;
        default:
            if (!int.TryParse(message.Text, out var ind))
                ind = -1;
            if (ind < 0)
                return;
            orders.RemoveAt(ind - 1);

            await vk.Messages.SendAsync(new()
            {
                PeerId = message.PeerId,
                Message = "Товар успешно удалён из корзины",
                RandomId = 0
            });

            currentState = BotStates.Default;
            await DefaultAction(vk, message);
            break;
    }
}

async Task PrintCart(VkApi vk, Message message)
{
    if (orders.Count == 0)
    {
        await vk.Messages.SendAsync(new()
        {
            PeerId = message.PeerId,
            Message = "Товары отсутствуют в корзине",
            RandomId = 0,
        });
        await DefaultAction(vk, message);
        return;
    }

    currentState = BotStates.Cart;

    var response =
        """
        Список товаров в корзине:

        """;

    for (int i = 0; i < orders.Count; i++)
    {
        CartItem? order = orders[i];
        response +=
            $"""
            {i + 1}. {order.Name}

            """;
    }

    await vk.Messages.SendAsync(new()
    {
        PeerId = message.PeerId,
        Message = response,
        RandomId = 0,
        Keyboard = new()
        {
            //Inline = true,
            OneTime = true,
            Buttons = [
                       [
                            CreateButton("Убрать товар", KeyboardButtonColor.Negative)
                       ],
                       [
                            CreateButton("Назад", KeyboardButtonColor.Primary)
                       ]
                      ]
        }
    });
}

static Task DefaultAction(VkApi vk, Message message)
{
    return vk.Messages.SendAsync(new()
    {
        PeerId = message.PeerId,
        Message = DEFAULT_TEXT,
        RandomId = 0,
        Keyboard = new()
        {
            //Inline = true,
            OneTime = true,
            Buttons = [
                       [
                            CreateButton("Возврат", KeyboardButtonColor.Primary)
                       ],
                       [
                            CreateButton("Частые вопросы", KeyboardButtonColor.Primary)
                       ],
                       [
                            CreateButton("Просмотреть корзину", KeyboardButtonColor.Primary)
                       ],
                       [
                            CreateButton("Оформить заказ", KeyboardButtonColor.Positive)
                       ]
                      ]
        }
    });
}

Task FaqAction(VkApi vk, Message message)
{
    currentState = BotStates.FAQ;
    return vk.Messages.SendAsync(new()
    {
        PeerId = message.PeerId,
        Message =
        """
        Уточните какой вопрос вас интересует:
        1. Способы доставки
        2. Стоимость доставки
        3. Способы оплаты
        4. Сроки доставки 
        """,
        RandomId = 0,
        Keyboard = new()
        {
            OneTime = true,
            Buttons =
                [
                 [
                    CreateButton("1", KeyboardButtonColor.Primary)
                 ],
                 [
                    CreateButton("2", KeyboardButtonColor.Primary)
                 ],
                 [
                    CreateButton("3", KeyboardButtonColor.Primary)
                 ],
                 [
                    CreateButton("4", KeyboardButtonColor.Primary)
                 ],
                 [
                    CreateButton("Назад", KeyboardButtonColor.Negative)
                 ]
                ]
        }
    });
}

Task AddProductAction(VkApi vk, BotStates currentState, Message message, Market product)
{
    orders.Add(new((int)product.Id.GetValueOrDefault(), product.Title));

    return vk.Messages.SendAsync(new()
    {
        PeerId = message.PeerId,
        Message = "Товар был добавлен в козину",
        RandomId = 0,
        Keyboard = new()
        {
            //Inline = true,
            OneTime = true,
            Buttons = [
                       [
                            CreateButton("Возврат", KeyboardButtonColor.Primary)
                       ],
                       [
                            CreateButton("Частые вопросы", KeyboardButtonColor.Primary)
                       ],
                       [
                            CreateButton("Просмотреть корзину", KeyboardButtonColor.Primary)
                       ],
                       [
                            CreateButton("Оформить заказ", KeyboardButtonColor.Positive)
                       ]
                      ]
        }
    });
}

static MessageKeyboardButton CreateButton(string text, KeyboardButtonColor color)
    => new() { Action = new() { Type = KeyboardButtonActionType.Text, Label = text, Payload = "{}" }, Color = color };

async Task FAQAction(VkApi vk, Message message)
{
    string? responseMessage = null;

    switch (message.Text)
    {
        case "1":
            responseMessage = "Информация про способы доставки";
            break;
        case "2":
            responseMessage = "Информация про стоимость доставки";
            break;
        case "3":
            responseMessage = "Информация про способы оплаты";
            break;
        case "4":
            responseMessage = "Информация про сроки доставки";
            break;
    }

    if (responseMessage == null)
    {
        currentState = BotStates.Default;
        await DefaultAction(vk, message);
        return;
    }

    await vk.Messages.SendAsync(new()
    {
        PeerId = message.PeerId,
        Message = responseMessage,
        RandomId = 0,
        Keyboard = new()
        {
            OneTime = true,
            Buttons =
            [
                 [
                                        CreateButton("1", KeyboardButtonColor.Primary)
                                     ],
                                     [
                                        CreateButton("2", KeyboardButtonColor.Primary)
                                     ],
                                     [
                                        CreateButton("3", KeyboardButtonColor.Primary)
                                     ],
                                     [
                                        CreateButton("4", KeyboardButtonColor.Primary)
                                     ],
                                     [
                                        CreateButton("Назад", KeyboardButtonColor.Negative)
                                     ]
            ]
        }
    });
}