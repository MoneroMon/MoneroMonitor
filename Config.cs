using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace MoneroMonitor
{
    class Config
    {
        static IConfiguration config;

        public Config()
        {
            // Read config in from JSON file
            config = new ConfigurationBuilder().AddJsonFile("config.json", false, true).Build();
        }

        public static IEnumerable<IConfigurationSection> GetMiners()
        {
            IEnumerable<IConfigurationSection> miners;
            try
            {
                miners = config.GetSection("MinerInstances").GetChildren();
            }
            catch
            {
                Console.WriteLine("Exception Occurred trying to read miner instances from config file. " +
                    "Please check formatting is correct and there is at least one in there.");
                return null;
            }
            return miners;
        }

        public bool IdIsValid(int idToCheck)
        {
            var allowedTelegramUserIds = config.GetSection("AllowedTelegramUserIds").GetChildren();
            foreach (IConfigurationSection id in allowedTelegramUserIds)
            {
                try
                {
                    if (idToCheck == Convert.ToInt32(id.Value))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception Occurred:{ex.Message} at {ex.StackTrace}");
                    return false;
                }
            }
            return false;
        }


        public static List<int> GetIds()
        {
            var ids = new List<int>();
            var allowedTelegramUserIds = config.GetSection("AllowedTelegramUserIds").GetChildren();
            foreach (IConfigurationSection id in allowedTelegramUserIds)
            {
                try
                {
                    ids.Add(Convert.ToInt32(id.Value));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception Occurred:{ex.Message} at {ex.StackTrace}");
                }
            }
            return ids;
        }

        public static string GetTelegramBotToken()
        {
            string token = config["BotToken"];
            if (token != null)
            {
                return token;
            }
            else
            {
                Console.WriteLine("Unable to read token from config file.");
                return null;
            }
        }
    }
}
