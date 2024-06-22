using VkNet;
using VkNet.Enums.StringEnums;
using VkNet.Model;

var vk = new VkApi();
vk.Authorize(new() { AccessToken = "" });

var server = vk.Groups.GetLongPollServer(225796199);
Console.WriteLine("Connected");
var @params = new BotsLongPollHistoryParams() { Key = server.Key, Server = server.Server, Ts = server.Ts, Wait = 10 };

const string DEFAULT_TEXT = "Donda";

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
                switch (message.Text.ToLower())
                {
                    case "/faq":
                        await vk.Messages.SendAsync(new() { PeerId = message.PeerId, Message = message.Text, RandomId = 0, Keyboard = new() { Buttons = [[]] } });
                        break;
                    case "/addproduct":
                        break;
                    case "/order":
                        break;
                    case "/":
                    default:
                        await vk.Messages.SendAsync(new()
                        {
                            PeerId = message.PeerId,
                            Message = DEFAULT_TEXT,
                            RandomId = 0,
                            Keyboard = new()
                            {
                                //Inline = true,
                                OneTime = true,
                                Buttons = [new List<MessageKeyboardButton>
                                {
                                    new()
                                    {
                                        Action = new() { Type = KeyboardButtonActionType.Text, Label = "Возврат", Payload = "{}"  }
                                    },
                                    new()
                                    {
                                        Action = new() { Type = KeyboardButtonActionType.Text, Label = "Частые вопросы", Payload = "{}" }
                                    },
                                    new()
                                    {
                                        Action = new() { Type = KeyboardButtonActionType.Text, Label = "Добавить продукт в заказ", Payload = "{}" }
                                    },
                                    new()
                                    {
                                        Action = new() { Type = KeyboardButtonActionType.Text, Label = "Оформить заказ", Payload = "{}" },
                                        Color = KeyboardButtonColor.Primary
                                    },
                            }]
                            }
                        });
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
