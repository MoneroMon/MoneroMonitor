using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading;
using MoneroMonitor;

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
        private static DateTime LastStarted = new DateTime(1970,01,01);

        static void Main()
        {
            config = new Config();
            botManager = new BotManager(config);
            botManager.bot.OnMessage += Bot_OnMessage;

            MinerMonitorMain();
        }

        static void MinerMonitorMain()
        {
            while (MinerSetupState != State.readyToStart)
            {
                Thread.Sleep(500);
            }
            InitiateMiningProcess();

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
            if (miningProcess.HasExited && MinerSetupState != State.stopped)
            {
                _ = botManager.SendMessage("Program crashed (or was stopped manually).");
                Console.WriteLine("Program crashed (or was stopped manually).");
                InitiateMiningProcess();
                Thread.Sleep(5000);
            }
        }

        private static void InitiateMiningProcess()
        {
            if (LastStarted > (DateTime.Now - new TimeSpan(0, 2, 0)))
            {
                Console.WriteLine("Started too shortly ago. Waiting for a while to avoid spamming the server.");
                _ = botManager.SendMessage("Started too shortly ago. Waiting for a little while to avoid spamming the server.");
                Thread.Sleep(new TimeSpan(0, 2, 0));
            }
            _ = botManager.SendMessage($"Starting Mining Process. Current mode for sending messages is {monitorMode}");
            miningProcess.StartInfo.UseShellExecute = false;
            miningProcess.StartInfo.RedirectStandardOutput = true;
            miningProcess.StartInfo.Verb = "runas";
            if (MinerSetupState == State.readyToStart)
            {
                miningProcess.Start();
            }
            LastStarted = DateTime.Now;
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
                _ = botManager.SendMessage($"{lineRead}");
            }
            else if (monitorMode == MonitorMode.errorOnly)
            {
                string lineReadLowerCase = lineRead.ToLower();
                bool containsAbnormalPhrase = !lineReadLowerCase.Contains("accepted (")
                    && !lineReadLowerCase.Contains("speed 10s/60s/15m")
                    && !lineReadLowerCase.Contains("new job from");
                if (containsAbnormalPhrase)
                {
                    _ = botManager.SendMessage($"{lineRead}");
                }
                else if (lineReadLowerCase.Contains("error") || lineReadLowerCase.Contains("rejected"))
                {
                    _ = botManager.SendMessage($"{lineRead}");
                }
                else if (lineRead.Contains(MsrFail))
                {
                    Console.WriteLine(MsrFail);
                    _ = botManager.SendMessage($"{MsrFail}");
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
                        _ = botManager.SendMessage("Registered your chat ID.");
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
                                            _ = botManager.SendMessage($"Selecting {miner.Key}");
                                            MinerSetupState = State.readyToStart;
                                            break;
                                        }
                                    }
                                    if (MinerSetupState != State.readyToStart)
                                    {
                                        _ = botManager.SendMessage("Not a valid answer");
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
                _ = botManager.SendMessage("Okay.");
                miningProcess.Kill();
                MinerSetupState = State.stopped;
            }
            else if (incomingText == "restart")
            {
                _ = botManager.SendMessage("Okay.");
                miningProcess.Kill();
            }
            else if (incomingText == "start")
            {
                _ = botManager.SendMessage("Okay.");
                MinerSetupState = State.readyToStart;
            }
            else if (incomingText == "show all")
            {
                _ = botManager.SendMessage("Okay, I can do that.");
                monitorMode = MonitorMode.all;
            }
            else if (incomingText == "show errors only")
            {
                _ = botManager.SendMessage("Okay, I can do that.");
                monitorMode = MonitorMode.errorOnly;
            }
            else if (incomingText == "show restarts only")
            {
                _ = botManager.SendMessage("Okay, I can do that.");
                monitorMode = MonitorMode.restartOnly;
            }
            else if (incomingText == "change miner")
            {
                _ = botManager.SendMessage("Okay, I can do that.");
                MinerSetupState = State.noMiner;
                miningProcess.Kill();
                botManager.ChooseMiner();
            }
            else if (incomingText == "last speed")
            {
                if (lastSpeed != null)
                {
                    _ = botManager.SendMessage(lastSpeed);
                }
                else
                {
                    _ = botManager.SendMessage("There hasn't been one yet.");
                }
            }
            else
            {
                _ = botManager.SendMessage("Not a recognised command.");
            }
        }


    }
}