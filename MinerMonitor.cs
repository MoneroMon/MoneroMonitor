using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.Threading;
using Telegram.Bot.Args;
using Telegram.Bot.Types;

namespace MoneroMonitor
{
    class MinerMonitor
    {
        enum MonitorMode
        {
            all,
            errorOnly,
            restartOnly
        }

        enum State
        {
            noChatId,
            noMiner,
            readyToStart,
            stopped
        }

        private static MonitorMode monitorMode = MonitorMode.errorOnly;
        private static string lastSpeed;
        private static Process miningProcess = new Process();

        private static State MinerSetupState = State.noChatId;
        private static BotManager botManager;
        private static Config config;
        private static DateTime LastStarted = new DateTime(1970, 01, 01);

        static void Main()
        {
            config = new Config();
            botManager = new BotManager(config);
            botManager.bot.OnMessage += Bot_OnMessage;

            while (MinerSetupState != State.readyToStart)
            {
                Thread.Sleep(5000);
            }

            while (true)
            {
                string lineRead = miningProcess.StandardOutput.ReadLine();
                if (lineRead != null && lineRead != "")
                {
                    Console.WriteLine(lineRead);
                    ProcessLineRead(lineRead);
                }
                Thread.Sleep(100);
                CheckIfStoppedAndRestart();
            }
        }

        private static void CheckIfStoppedAndRestart()
        {
            if (miningProcess.HasExited && MinerSetupState == State.readyToStart)
            {
                botManager.SendMessage("Program crashed (or was stopped manually).");
                Console.WriteLine("Program crashed (or was stopped manually).");
                InitiateMiningProcess(false);
            }
        }

        private static void InitiateMiningProcess(bool manualStart)
        {
            if (LastStarted > (DateTime.Now - new TimeSpan(0, 2, 0)) && !manualStart && MinerSetupState == State.readyToStart)
            {
                Console.WriteLine("Started too shortly ago. Waiting for a while to avoid spamming the server.");
                botManager.SendMessage("Started too shortly ago. Waiting for a little while to avoid spamming the server.");
                Thread.Sleep(new TimeSpan(0, 2, 0));
            }
            if (manualStart || MinerSetupState == State.readyToStart)
            {
                botManager.SendMessage($"Starting Mining Process. Current mode for sending messages is {monitorMode}");
                miningProcess.StartInfo.UseShellExecute = false;
                miningProcess.StartInfo.RedirectStandardOutput = true;
                miningProcess.StartInfo.Verb = "runas";
                miningProcess.Start();
                LastStarted = DateTime.Now;
            }
        }

        private static void ProcessLineRead(string lineRead)
        {
            string MsrFail = "FAILED TO APPLY MSR MOD, HASHRATE WILL BE LOW";
            if (lineRead.Contains("speed"))
            {
                lastSpeed = lineRead;
            }

            if (monitorMode == MonitorMode.all)
            {
                botManager.SendMessage($"{lineRead}");
            }
            else if (monitorMode == MonitorMode.errorOnly)
            {
                string lineReadLowerCase = lineRead.ToLower();
                bool containsAbnormalPhrase = !lineReadLowerCase.Contains("accepted (")
                    && !lineReadLowerCase.Contains("speed 10s/60s/15m")
                    && !lineReadLowerCase.Contains("new job from");
                if (containsAbnormalPhrase)
                {
                    botManager.SendMessage($"{lineRead}");
                }
                else if (lineReadLowerCase.Contains("error") || lineReadLowerCase.Contains("rejected"))
                {
                    botManager.SendMessage($"{lineRead}");
                }
                else if (lineRead.Contains(MsrFail))
                {
                    Console.WriteLine(MsrFail);
                    botManager.SendMessage($"{MsrFail}");
                }
            }
        }

        private static void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            if (config.IdIsValid(e.Message.From.Id))
            {
                if (e.Message.Text != null && e.Message.Text.Length > 0)
                {
                    if (botManager.chatId == null)
                    {
                        Console.WriteLine($"Chat ID: {e.Message.Chat.Id}, Name: {e.Message.From.FirstName}, User ID: {e.Message.From.Id}, Message: {e.Message.Text}");

                        botManager.chatId = e.Message.Chat.Id;
                        MinerSetupState = State.noMiner;
                        botManager.SendMessage("Registered your chat ID.");
                        botManager.ChooseMiner();
                    }
                    else
                    {
                        if (e.Message.ReplyToMessage != null) // If this is a reply to a message
                        {
                            switch (e.Message.ReplyToMessage.Text)
                            {
                                case "Choose one of these miners to run:":
                                    foreach (IConfigurationSection miner in Config.GetMiners())
                                    {
                                        if (e.Message.Text.ToLower() == miner.Key.ToLower())
                                        {
                                            miningProcess.StartInfo.FileName = miner.Value;
                                            botManager.SendMessage($"Selecting {miner.Key}");
                                            InitiateMiningProcess(true);
                                            MinerSetupState = State.readyToStart;
                                            break;
                                        }
                                    }
                                    if (MinerSetupState != State.readyToStart)
                                    {
                                        botManager.SendMessage("Not a valid answer");
                                        botManager.ChooseMiner();
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        else if (MinerSetupState == State.noMiner)
                        {
                            botManager.ChooseMiner();
                        }
                        else
                        {
                            HandleNormalIncomingMessage(e.Message);
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine($"Message received from user not in allowed list. ID:{e.Message.From.Id}, " +
                    $"First name:{e.Message.From.FirstName}, Last name:{e.Message.From.LastName}, Username:{e.Message.From.Username}, " +
                    $"IsBot:{e.Message.From.IsBot}, Language:{e.Message.From.LanguageCode}");
                _ = botManager.bot.SendTextMessageAsync(chatId: e.Message.Chat.Id, "Sorry, your ID couldn't be validated.");
            }
        }


        private static void HandleNormalIncomingMessage(Message message)
        {
            string incomingText = message.Text.ToLower();

            if (incomingText == "stop")
            {
                botManager.SendMessage("Okay.");
                miningProcess.Kill();
                MinerSetupState = State.stopped;
            }
            else if (incomingText == "restart")
            {
                botManager.SendMessage("Okay.");
                miningProcess.Kill();
            }
            else if (incomingText == "start")
            {
                botManager.SendMessage("Okay.");
                MinerSetupState = State.readyToStart;
            }
            else if (incomingText == "show all")
            {
                botManager.SendMessage("Okay, I can do that.");
                monitorMode = MonitorMode.all;
            }
            else if (incomingText == "show errors only")
            {
                botManager.SendMessage("Okay, I can do that.");
                monitorMode = MonitorMode.errorOnly;
            }
            else if (incomingText == "show restarts only")
            {
                botManager.SendMessage("Okay, I can do that.");
                monitorMode = MonitorMode.restartOnly;
            }
            else if (incomingText == "change miner")
            {
                botManager.SendMessage("Okay, I can do that.");
                MinerSetupState = State.noMiner;
                miningProcess.Kill();
                botManager.ChooseMiner();
            }
            else if (incomingText == "last speed")
            {
                if (lastSpeed != null)
                {
                    botManager.SendMessage(lastSpeed);
                }
                else
                {
                    botManager.SendMessage("There hasn't been one yet.");
                }
            }
            else
            {
                botManager.SendMessage("Not a recognised command.");
            }
        }


    }
}