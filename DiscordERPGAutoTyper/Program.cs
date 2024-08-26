﻿using System;
using System.Timers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Diagnostics;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using System.Runtime.InteropServices;

namespace DiscordERPGAutoTyper
{
    internal class Program
    {
        static IWebDriver driver;
        static Timer huntT;
        static Timer workT;
        static Timer farmT;
        static Timer checkMessageT;
        /*static Stopwatch huntS;
        static Stopwatch workS;
        static Stopwatch farmS;*/
        static readonly string baseURL = "https://discord.com";
        static readonly string discordChannelAuto = "https://discord.com/channels/1180781501940518932/1264330502005985391";

        static string lastMessageId = string.Empty;

        static Queue<string> messageQueue = new Queue<string>();
        static string hunt = "rpg hunt h";
        static string work = "rpg chop";
        static string farm = "rpg farm";

        static int huntCooldown = 40000;
        static int workCooldown = 197000;
        static int farmCooldown = 392000;

        static int huntCount = 0;
        static int workCount = 0;
        static int farmCount = 0;

        static int boostCount = 0;

        static double huntRemainingInterval = 0;
        static double workRemainingInterval = 0;
        static double farmRemainingInterval = 0;

        static int area = 10;



        static async Task Main(string[] args)
        {
            string password = "pass";
            string email = "email";

            KillAllChromeProcesses();

            Initialize();

            GoToChannel(discordChannelAuto);

            if (CheckIfLoggedIn() == true)
            {
                Login(email, password);
                GoToChannel(discordChannelAuto);
            }

            PrvniStart();


            huntT = new Timer(huntCooldown); // 20000 milliseconds = 20 seconds
            huntT.Elapsed += (sender, e) => QueueMessage(hunt);
            huntT.AutoReset = true;
            huntT.Enabled = true;
            

            workT = new Timer(workCooldown);
            workT.Elapsed += (sender, e) => QueueMessage(work);
            workT.AutoReset = true;
            workT.Enabled = true;
            
            farmT = new Timer(farmCooldown);
            if (area >= 4)
            {
                farmT.Elapsed += (sender, e) => QueueMessage(farm);
                farmT.AutoReset = true;
                farmT.Enabled = true;
                
            }
            checkMessageT = new Timer(2000);
            checkMessageT.Elapsed += (sender, e) => EventCheck(CheckLastMessage());
            checkMessageT.AutoReset = true;
            checkMessageT.Enabled = true;

            await ProcessQueue();
            // Keep the application running to allow the timers to fire
            Console.WriteLine("Press [Enter] to exit the program...");
            Console.ReadLine();

            // Clean up
            CleanUp();
        }
        static void KillAllChromeProcesses()
        {
            // Get all processes running on the system
            Process[] chromeProcesses = Process.GetProcessesByName("chrome");

            foreach (Process process in chromeProcesses)
            {
                try
                {
                    // Terminate the process
                    process.Kill();
                    process.WaitForExit(); // Optional: Wait for the process to exit to ensure it has been terminated
                    Console.WriteLine($"Killed process {process.Id}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while killing process {process.Id}: {ex.Message}");
                }
            }

            Console.WriteLine("All Chrome processes have been terminated.");
        }
        static void Initialize()
        {
            Console.WriteLine("Initializing");

            //Chrome setup
            ChromeOptions options = new ChromeOptions();
            // Using a user profile

            string userProfile = @"C:\Users\Uživatel\AppData\Local\Google\Chrome\User Data";
<<<<<<< Updated upstream:DiscordERPGAutoTyper/Program.cs
            string profileName = "Profile 2"; // Or the name of the profile you want to use
=======
            string profileName = "Profile 1"; // Or the name of the profile you want to use
>>>>>>> Stashed changes:test/Program.cs
            options.AddArgument($"user-data-dir={userProfile}");
            options.AddArgument($"profile-directory={profileName}");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");

            driver = new ChromeDriver(options);

        }
        static void GoToChannel(string channel)
        {
            Console.WriteLine($"{channel}");
            driver.Navigate().GoToUrl(channel);
        }
        static void CleanUp()
        {
            Console.WriteLine("EXECUTE ORDER 66");
            // Clean up
            huntT.Stop();
            huntT.Dispose();
            workT.Stop();
            workT.Dispose();
            farmT.Stop();
            farmT.Dispose();

            driver.Quit();
        }
        static void PrvniStart()
        {
            Console.WriteLine("Startuju. v: " + DateTime.Now);
            //setup work
            switch (area)
            {
                case 3 or 4 or 5:
                    work = "rpg axe";
                    Console.WriteLine("Používám: " + work);
                    break;
                case 6 or 7 or 8:
                    work = "rpg bowsaw";
                    Console.WriteLine("Používám: " + work);
                    break;
                case 9 or 10 or 11 or 12 or 13 or 14 or 15:
                    work = "rpg chainsaw";
                    Console.WriteLine("Používám: " + work);
                    break;
                default:
                    work = "rpg chop";
                    Console.WriteLine("Používám: " + work);
                    break;
            }
            SendMessage("rpg cd");
            System.Threading.Thread.Sleep(2001);
            SendMessage(hunt);
            System.Threading.Thread.Sleep(2001);
            SendMessage(work);
            System.Threading.Thread.Sleep(2001);
            if (area >= 4)
                SendMessage(farm);
        }
        static void QueueMessage(string message)
        {
            if (message == "rpg hunt h" && huntCount == 0)
            {
                message = "rpg hunt h t";
                huntCount++;
            }
            else if (message == "rpg hunt h" && huntCount >= 1)
            {
                huntCount = 0;
            }
            messageQueue.Enqueue(message);
            Console.WriteLine($"Message '{message}' queued at: " + DateTime.Now);
        }
        static async Task ProcessQueue()
        {
            while (true)
            {
                if (messageQueue.Count > 0)
                {
                    var message = messageQueue.Dequeue();
                    SendMessage(message);
                    await Task.Delay(2000); // Wait for 2 seconds before sending the next message
                }
                else
                {
                    await Task.Delay(500); // Check the queue every 0.5 seconds if empty
                }
            }
        }
        private static void SendMessage(string command)
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                // Locate the chat box element

                IWebElement chatBox = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("span[data-slate-node='text']")));

                // Type the message
                chatBox.SendKeys(command);
                // Submit the message

                chatBox = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("div[role='textbox']")));
                chatBox.SendKeys(Keys.Enter);

                Console.WriteLine(command + " sent at: " + DateTime.Now);
                //Check if cooldown
                System.Threading.Thread.Sleep(2001);

                EventCheck(CheckLastMessage());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SEND MESSAGE    An error occurred: {ex.Message}");
            }
        }
        private static void SendCommand(string command,Timer timer,int count,Stopwatch stopwatch)
        {
            try
            {
<<<<<<< Updated upstream:DiscordERPGAutoTyper/Program.cs
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
=======

                command = HuntMarrySwapper(command);
>>>>>>> Stashed changes:test/Program.cs

                // Locate the chat box element
                IWebElement chatBox = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("span[data-slate-node='text']")));

                // Type the message
                chatBox.SendKeys(command);
                count++;

                // Submit the message
                chatBox = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("div[role='textbox']")));
                chatBox.SendKeys(Keys.Enter);

                Console.WriteLine(command + " number: " + count + " sent at: " + DateTime.Now);
                stopwatch.Restart();
                timer.Interval = 420; //FIX
                //Check if cooldown
                System.Threading.Thread.Sleep(2001);

                EventCheck(CheckLastMessage());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SEND MESSAGE    An error occurred: {ex.Message}");
            }
        }
        static string HuntMarrySwapper(string message)
        {
            if (message == "rpg hunt h" && huntMarryTracker == 0) //alternates between solo and together HARDCORE hunt
            {
                huntMarryTracker = 1;
            }
            else if (message == "rpg hunt h" && huntMarryTracker == 1)
            {
                message = "rpg hunt h t";
                huntMarryTracker = 0;
            }
            if (message == "rpg hunt" && huntMarryTracker == 0) //alternates between solo and together normal hunt
            {
                huntMarryTracker = 1;
            }
            else if (message == "rpg hunt" && huntMarryTracker == 1)
            {
                message = "rpg hunt t";
                huntMarryTracker = 0;
            }
            return message;
        }
        static void EventCheck(string message) //TODO---CHANGE TO SWITCH CASE
        {
            //Console.WriteLine("Event checking with message : " + message);
            if (message.Contains("Select the item of the image above or respond with the item name")) //Guard alert
            {
                ShowToastNotification("POLICIEEE", "Guard seen at: " + DateTime.Now);
                Console.WriteLine("AAAAAAAAAAA PANIC AAAAAAAAAAAAAAAAAAAA");
            }
            else if (message.Contains("TEST")) //INPUT test
            {
                Console.WriteLine("I hear you");
                ShowToastNotification("I hear you", "" + DateTime.Now);
                SendMessage("I hear you");
            }
            else if (message.Contains("wait at least")) //msg Cooldown
            {
                Console.WriteLine("Cooldown wait x");
            }
            else if (message.Contains("BOT HELP")) //INPUT help
            {
                Console.WriteLine("BOT HELP");
<<<<<<< Updated upstream:DiscordERPGAutoTyper/Program.cs
                SendMessage("Change w0rk ch0p/4xe/b0wsaw/ch4insaw");
=======
                SendMessage("Change work - chop / axe / bowsaw / chainsaw ");
                SendMessage("Change farm - farm / potato / carrot / bread");
                SendMessage("Bot farming - will start farming");
                SendMessage("Hunt swap - normal / hard");
>>>>>>> Stashed changes:test/Program.cs
            }
            else if (message.Contains("STOP")) //INPUT stop everything
            {
                Console.WriteLine("STOP");
                StopCommands();
                SendMessage("i stopped :(");
            }
            else if (message.Contains("START")) //INPUT start again
            {
                Console.WriteLine("START");
                SendMessage("rpg cd");
                System.Threading.Thread.Sleep(2001);
                SendMessage(hunt);
                System.Threading.Thread.Sleep(2001);
                SendMessage(work);
                System.Threading.Thread.Sleep(2001);
                if (area >= 4)
                    SendMessage(farm);
                StartCommands();
            }
            else if (message.Contains("HUNT SWAP ")) //INPUT hunt difficulty swap
            {
                if (message.Contains("NORMAL"))
                {
                    hunt = "rpg hunt";
                    SendMessage("L BOZO XDXDXD NOOB HUNTING");
                }
                else if (message.Contains("HARD"))
                {
                    hunt = "rpg hunt h";
                    SendMessage("HARDCORE HUNTING GAMING");
                }
            }
<<<<<<< Updated upstream:DiscordERPGAutoTyper/Program.cs
            else if (message.Contains("Change work"))
=======
            else if (message.Contains("CHANGE WORK")) //INPUT Change work
>>>>>>> Stashed changes:test/Program.cs
            {
                if (message.Contains("chop"))
                    work = "rpg chop";
                else if (message.Contains("axe"))
                    work = "rpg axe";
                else if (message.Contains("bowsaw"))
                    work = "rpg bowsaw";
                else if (message.Contains("chainsaw"))
                    work = "rpg chainsaw";
<<<<<<< Updated upstream:DiscordERPGAutoTyper/Program.cs
=======
                }
            }
            else if (message.Contains("CHANGE FARM")) //INPUT Change Farm
            {
                if (message.Contains("FARM FARM"))
                {
                    farm = "rpg farm";
                    SendMessage("I am farming normally");
                }
                else if (message.Contains("CARROT"))
                {
                    farm = "rpg farm carrot";
                    SendMessage("I am farming carrots");
                }
                else if (message.Contains("POTATO"))
                {
                    farm = "rpg farm potato";
                    SendMessage("I am farming potatoes");
                }
                else if (message.Contains("BREAD"))
                {
                    farm = "rpg farm bread";
                    SendMessage("I am farming bread");
                }

            }
            else if (message.Contains("BOT FARMING")) //INPUT turn on farming
            {
                SendMessage("I am farming");
                farmT = new Timer(farmCooldown);
                farmT.Elapsed += (sender, e) => SendCommand(farm, 3);
                farmT.AutoReset = true;
                farmT.Enabled = true;
                farmRunning = true;
>>>>>>> Stashed changes:test/Program.cs
            }
            else if (message.Contains("You were about to hunt a defenseless monster, but then you notice a zombie horde coming your way")) //Zombie event
            {
                Console.WriteLine("Zombie horde event at: " + DateTime.Now);
                ShowToastNotification("Zombie Horde", "" + DateTime.Now);
            }
            else if (message.Contains("megarace boost"))
            {
                boostCount++;
                Console.WriteLine(boostCount + " Event mega boost at: " + DateTime.Now);
                SendMessage("yes");
            }
            else if (message.Contains("AN EPIC TREE HAS JUST GROWN")) //Tree
            {
                Console.WriteLine("Tree event at: " + DateTime.Now);
                SendMessage("CUT");
            }
            else if (message.Contains("A MEGALODON HAS SPAWNED IN THE RIVER")) //fish
            {
                Console.WriteLine("Fish event at: " + DateTime.Now);
                SendMessage("LURE");
            }
            else if (message.Contains("IT'S RAINING COINS")) //Coins
            {
                Console.WriteLine("Coins event at: " + DateTime.Now);
                SendMessage("CATCH");
            }
            else if (message.Contains("God accidentally dropped an EPIC coin")) //Epic coin
            {
                Console.WriteLine("Epic coin event at: " + DateTime.Now);
                if (message.Contains("I SHALL BRING THE EPIC TO THE COIN"))
                    SendMessage("I SHALL BRING THE EPIC TO THE COIN");
                else if (message.Contains("MY PRECIOUS"))
                    SendMessage("MY PRECIOUS");
                else if (message.Contains("WHAT IS EPIC? THIS COIN"))
                    SendMessage("WHAT IS EPIC? THIS COIN");
                else if (message.Contains("YES! AN EPIC COIN"))
                    SendMessage("YES! AN EPIC COIN");
                else if (message.Contains("MY PRECIOUS"))
                    SendMessage("MY PRECIOUS");
                else if (message.Contains("OPERATION: EPIC COIN"))
                    SendMessage("OPERATION: EPIC COIN");
            }
            else if (message.Contains("OOPS! God accidentally dropped")) //solo coins
            {

                Console.WriteLine("Solo Coin event at: " + DateTime.Now);
                if (message.Contains("BACK OFF THIS IS MINE!!"))
                    SendMessage("BACK OFF THIS IS MINE!!");
                else if (message.Contains("HACOINA MATATA"))
                    SendMessage("HACOINA MATATA");
                else if (message.Contains("THIS IS MINE"))
                    SendMessage("THIS IS MINE");
                else if (message.Contains("ALL THE COINS BELONG TO ME"))
                    SendMessage("ALL THE COINS BELONG TO ME");
                else if (message.Contains("GIMME DA MONEY"))
                    SendMessage("GIMME DA MONEY");
                else if (message.Contains("OPERATION: COINS"))
                    SendMessage("OPERATION: COINS");
            }
            else if (message.Contains("EPIC NPC: I have a special trade today!")) //epic npc
            {
                Console.WriteLine("Epic NPC trade event at: " + DateTime.Now);
                if (message.Contains("YUP I WILL DO THAT"))
                    SendMessage("YUP I WILL DO THAT");
                else if (message.Contains("I WANT THAT"))
                    SendMessage("I WANT THAT");
                else if (message.Contains("HEY EPIC NPC! I WANT TO TRADE WITH YOU"))
                    SendMessage("HEY EPIC NPC! I WANT TO TRADE WITH YOU");
                else if (message.Contains("THAT SOUNDS LIKE AN OP BUSINESS"))
                    SendMessage("THAT SOUNDS LIKE AN OP BUSINESS");
                else if (message.Contains("OWO ME!!!"))
                    SendMessage("OWO ME!!!");
            }
            else if (message.Contains("A LOOTBOX SUMMONING HAS STARTED")) //lootboxes
            {
                Console.WriteLine("Lootbox event at: " + DateTime.Now);
                SendMessage("SUMMON");
<<<<<<< Updated upstream:DiscordERPGAutoTyper/Program.cs
=======

>>>>>>> Stashed changes:test/Program.cs
            }
            else if (message.Contains("A LEGENDARY BOSS JUST SPAWNED"))
            {
                Console.WriteLine("Boss event at: " + DateTime.Now);
                SendMessage("TIME TO FIGHT");
            }

        }
        static void StopCommands()
        {
            /*workRemainingInterval = GetRemainingTime(workS, workCooldown);
            huntRemainingInterval = GetRemainingTime(huntS, huntCooldown);
            farmRemainingInterval = GetRemainingTime(farmS, farmCooldown);*/
            workT.Stop();
            huntT.Stop();
            farmT.Stop();
        }
        static void StartCommands()
        {

            workT.Start();
            huntT.Start();
            farmT.Start();
        }
        static double GetRemainingTime(Stopwatch stopwatch, int cooldown)
        {
            // Calculate the remaining time based on the cooldown and the elapsed time
            double elapsed = stopwatch.ElapsedMilliseconds;
            return cooldown - elapsed;
        }
        static string CheckLastMessage()
        {
            try
            {

                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(2));
                IList<IWebElement> messages = driver.FindElements(By.CssSelector("li[id^='chat-messages-']"));

                if (messages.Count == 0)
                {
                    //Console.WriteLine("No messages found");
                    return string.Empty;
                }

                IWebElement lastMessageElement = messages[messages.Count - 1];
                string currentMessageId = lastMessageElement.GetAttribute("id");

                if (currentMessageId == lastMessageId)
                {
                    //Console.WriteLine("CheckLastMessage ID is the same");
                    return string.Empty;
                }

                lastMessageId = currentMessageId;

                //Console.WriteLine(lastMessageElement.Text);
                return lastMessageElement.Text;
            }
            catch
            {
                Console.WriteLine("nenašel jsem žádnou zprávu v CheckLastMessage");
            }
            return string.Empty;
        }
        static void Login(string email, string password)
        {
            Console.WriteLine("I am logging in");
            driver.Navigate().GoToUrl(baseURL);

            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

            // Wait for the login button and click it   
            IWebElement loginButton = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("a[data-track-nav = 'login']")));

            Console.WriteLine("button found");
            loginButton.Click();
            if (wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[text()='Choose an account']"))) != null)
            {
                Console.WriteLine("Našel jsem choose an account");
                IWebElement addAnAccount = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//*[text()='Add an account']")));
                addAnAccount.Click();
            }
            // Wait for the email input to become visible
            IWebElement emailInput = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("email")));
            emailInput.SendKeys(email);

            // Wait for the password input to become visible
            IWebElement passwordInput = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("password")));
            passwordInput.SendKeys(password);

            // Wait for the submit button to become clickable and click it

            IWebElement submitButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//button[@type='submit']")));
            submitButton.Click();

        }
        static bool CheckIfLoggedIn()
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
            Console.WriteLine("Checkuju jestli jsem logged in");
            // Wait for the login button and click it   
            try
            {
                IWebElement discordDetected = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[text()='Discord App Detected']")));
                if (discordDetected != null)
                {
                    return true;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            return false;
        }
        static void ShowToastNotification(string notificationTitle, string notificationBody)
        {
            // Initialize the toast notification content
            var toastContent = new ToastContentBuilder()
                .AddArgument("action", "viewApp")
                .AddText(notificationTitle)
                .AddText(notificationBody)
                .AddButton(new ToastButton()
                    .SetContent("Bring to Front")
                    .AddArgument("action", "bringToFront"))
                .AddAudio(new Uri("ms-winsoundevent:Notification.Default")) // Add sound
                .GetToastContent();

            // Show the toast notification
            var notification = new ToastNotification(toastContent.GetXml())
            {
                // Ensure the toast is displayed immediately
                ExpirationTime = DateTime.Now.AddMinutes(1)
            };
            ToastNotificationManagerCompat.CreateToastNotifier().Show(notification);

        }
    }
}
