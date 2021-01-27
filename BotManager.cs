using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;

namespace MoneroMonitor
{
    class BotManager
    {
        public ITelegramBotClient bot;
        public ChatId chatId;

        private Config config;

        public BotManager(Config config)
        {
            this.config = config;

            //Setup bot client
            bot = new TelegramBotClient(Config.GetTelegramBotToken());
            var me = bot.GetMeAsync().Result;
            Console.WriteLine($"Starting bot {me.Id} with name of {me.FirstName}.");
            bot.StartReceiving();

            var sendMessageTask = bot.SendTextMessageAsync(chatId: Config.GetIds()[0], "Started");
            if (sendMessageTask.Wait(5000))
            {
                var result = sendMessageTask.IsCompletedSuccessfully;
                if (result)
                {
                    Console.WriteLine("Sent started message to first authorised user in config");
                }
                else
                {
                    Console.WriteLine("Failed to send Started message. " +
                        "This doesn't necessarily mean the rest of the program won't work so please carry on as normal.");
                }
            }
            else
            {
                Console.WriteLine("Failed to send Started message. " +
                        "This doesn't necessarily mean the rest of the program won't work so please carry on as normal.");
            }

        }

        public void ChooseMiner()
        {
            var replyMarkup = new Telegram.Bot.Types.ReplyMarkups.ForceReplyMarkup();
            var sendMessageTask = bot.SendTextMessageAsync(chatId: chatId, "Choose one of these miners to run:", replyMarkup: replyMarkup);
            sendMessageTask.Wait(5000);
            string minersToSend = "";
            foreach (IConfigurationSection miner in Config.GetMiners())
            {
                minersToSend += $"{miner.Key} \n";
            }
            if (minersToSend != "")
            {
                _ = bot.SendTextMessageAsync(chatId: chatId, text: minersToSend);
            }
            else
            {
                _ = bot.SendTextMessageAsync(chatId: chatId, text: "No miners in config file! You need to put some in first.");
            }

        }

        public void SendMessage(string message)
        {
            if (chatId != null)
            {
                if (message != null && message.Length > 0)
                {
                    _ = bot.SendTextMessageAsync(chatId: chatId, message);
                }
            }
            else
            {
                Console.WriteLine("No chat ID. Telegram user not connected.");
            }
        }

    }
}
