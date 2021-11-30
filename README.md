# MoneroMon
This program interfaces with XMRig to allow you to control basic functions of the miner from your phone. It also sends you messages from the XMRig console and can filter which ones you get sent, such as only errors. This allows you to set it up and then not worry about checking your miner is still working - you'll get a message if there's a problem (except if the computer the miner is running on loses internet connection, power, or this program is shut - in that case it can't send you a message!) However if your internet goes down you'll still get notified about it once it is back up.

MoneroMon is mainly useful for monitoring if there's a problem with the pool or if your shares are getting rejected because of some unstable overclocking. 

The program works by making use of a telegram bot. You have to create a telegram bot yourself and then copy the token it gives into the config.json file of MoneroMon. You also have to add your ID to the allowed list. This is to stop others from acessing your bot and controlling your miner. 

# Features
- Choose from multiple instances of XMRig. You can copy your XMRig folder and have it setup for different pools in case one goes down. You can then change the pool from your phone.
- Stop, start and restart miner
- Show the last speed from XMRig
- Multiple modes for monitoring. The program reads from the console of XMRig. By default it only filters out routine messages that occur when the miner is running normally, so you will receive all the messages on startup of XMRig and then only errors after that.
- Automatically restarts XMRig if it crashes or closes.

# Setup
1. Create a telegram bot. For this you need the telegram app on your phone. This is a good guide: https://docs.microsoft.com/en-us/azure/bot-service/bot-service-channel-connect-telegram?view=azure-bot-service-4.0. You only need to follow it until you get the token.
2. Once you have the token, copy it into the config file.
3. Start MoneroMonitor
4. Add your bot on telegram and send it a message.
5. Setup your ID in the whitelist. This is needed so that other people can't access your bot and control the miner. The message you sent in the last step should come up in the console with your ID. Copy this ID into the config file and save it. 
6. Set the path of the miner. Find the exe and copy the path into the setup file. You can set multiple miners up. The bot will ask you which one to use when it starts up. You can also give them a name. You have to replace all \ characters in the file path with \\ otherwise it will throw an error.

# How to use
Follow setup then run the program as administrator (this is so it can start XMRig as administrator and get it to apply MSR mod).
Check your phone and select a miner to start.
You should see all the startup messages from XMRig appear as messages.
You can send the following commands:
- Stop
- Start
- Restart
- Show all. This causes the program to send every console line that XMRig generates as a message to your phone. You probably don't want to leave this on all the time.
- Show errors only. This filters out certain lines for routine operations and only sends errors as a message to your phone.
- show restarts only
- change miner
- last speed. This shows the last hashrate.

# Supported plaforms.
Only Windows 10 64 bit has been tested but you're welcome to try on other operating systems.

This program is written for .NET 5 so you will need to have it installed. You only need the runtime, not SDK. https://dotnet.microsoft.com/download

# Disclaimer
This program is still in development and no guarantees are provided that it will work correctly, or that updates won't break things!
