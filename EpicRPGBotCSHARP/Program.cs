using System;
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
using System.Runtime.InteropServices;
using System.Reflection;

namespace EpicRPGBotCSharp
{
    internal class Program
    {
        private sealed class BotConfig
        {
            public string BaseUrl { get; private set; }
            public string DiscordChannelUrl { get; private set; }
            public int? Area { get; private set; }
            public string DiscordEmail { get; private set; }
            public string DiscordPassword { get; private set; }
            public string SourceUserDataDir { get; private set; }
            public string ProfileName { get; private set; }

            public static BotConfig LoadFromEnv()
            {
                var cfg = new BotConfig
                {
                    BaseUrl = GetEnv("BASE_URL"),
                    DiscordChannelUrl = GetEnv("DISCORD_CHANNEL_URL"),
                    DiscordEmail = GetEnv("DISCORD_EMAIL"),
                    DiscordPassword = GetEnv("DISCORD_PASSWORD"),
                    SourceUserDataDir = GetEnv("SOURCE_USER_DATA_DIR"),
                    ProfileName = GetEnv("PROFILE_NAME")
                };
                var areaString = GetEnv("AREA");
                if (!string.IsNullOrWhiteSpace(areaString) && int.TryParse(areaString, out var parsedArea))
                {
                    cfg.Area = parsedArea;
                }
                return cfg;
            }
        }
        private static BotConfig Config;
        static IWebDriver driver;

        static Timer huntT;
        static Timer workT;
        static Timer farmT;
        static Timer checkMessageT;

        static string baseURL = "https://discord.com";
        static string discordChannelAuto = "https://discord.com/channels/1180781501940518932/1264330502005985391";

        //Player
        static int area = 10;

        //Commands
        static string hunt = "rpg hunt h";
        static string work = "rpg chop";
        static string farm = "rpg farm";

        static int huntCooldown = 21000;
        static int workCooldown = 99000;
        static int farmCooldown = 196000;

        // Removed unused flags

        static int huntMarryTracker = 0;

        static Stopwatch commandDelay;

        //Events
        static int boostCount = 0;

        //Last Messages
        static string lastMessageId = string.Empty;

        static void Main(string[] args)
        {
            // Load environment from .env and apply overrides
            LoadEnv();
            Config = BotConfig.LoadFromEnv();
            ConfigureAssemblyResolve();
            if (!string.IsNullOrWhiteSpace(Config.BaseUrl)) baseURL = Config.BaseUrl;
            if (!string.IsNullOrWhiteSpace(Config.DiscordChannelUrl)) discordChannelAuto = Config.DiscordChannelUrl;
            if (Config.Area.HasValue) area = Config.Area.Value;
            string email = Config.DiscordEmail ?? string.Empty;
            string password = Config.DiscordPassword ?? string.Empty;

            KillAllChromeProcesses();

            Initialize();
            GoToChannel(discordChannelAuto);

            // Login is disabled; relying on existing Chrome profile session
            StartBot();

            Console.WriteLine("Press [Enter] to exit the program...");
            Console.ReadLine();

            // Clean up
            CleanUp();
        }
        private static void ConfigureAssemblyResolve()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                try
                {
                    var requested = new AssemblyName(args.Name);
                    if (!string.Equals(requested.Name, "Newtonsoft.Json", StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }

                    // 1) Look next to the executable
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string localPath = Path.Combine(baseDir, "Newtonsoft.Json.dll");
                    if (File.Exists(localPath))
                    {
                        return Assembly.LoadFrom(localPath);
                    }

                    // 2) Look in user's NuGet cache for highest version (net45 flavor)
                    string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    string nugetRoot = Path.Combine(userProfile, ".nuget", "packages", "newtonsoft.json");
                    if (Directory.Exists(nugetRoot))
                    {
                        string best = null;
                        Version bestVersion = null;
                        foreach (var dir in Directory.GetDirectories(nugetRoot))
                        {
                            var name = Path.GetFileName(dir);
                            if (Version.TryParse(name, out var v))
                            {
                                if (bestVersion == null || v > bestVersion)
                                {
                                    bestVersion = v;
                                    best = dir;
                                }
                            }
                        }
                        if (best != null)
                        {
                            string probe = Path.Combine(best, "lib", "net45", "Newtonsoft.Json.dll");
                            if (File.Exists(probe))
                            {
                                return Assembly.LoadFrom(probe);
                            }
                        }
                    }
                }
                catch { }
                return null;
            };
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

            try
            {
                // Also terminate any chromedriver leftovers
                foreach (var driverProc in Process.GetProcessesByName("chromedriver"))
                {
                    try { driverProc.Kill(); driverProc.WaitForExit(); Console.WriteLine($"Killed chromedriver {driverProc.Id}"); } catch { }
                }
            }
            catch { }

            Console.WriteLine("All Chrome processes have been terminated.");
        }
        static void Initialize()
        {
            Console.WriteLine("Initializing");

            //Chrome setup
            ChromeOptions options = new ChromeOptions();
            // Using a user profile

            string sourceUserDataDir = !string.IsNullOrWhiteSpace(Config?.SourceUserDataDir)
                ? Config.SourceUserDataDir
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data");
            string profileName = !string.IsNullOrWhiteSpace(Config?.ProfileName) ? Config.ProfileName : "Profile 1";

            // Create an isolated temp user-data-dir cloned from the user's profile to avoid locks
            string tempUserDataDir = Path.Combine(Path.GetTempPath(), "EpicRPG_Auto_ChromeProfile");
            try
            {
                if (Directory.Exists(tempUserDataDir))
                {
                    try { Directory.Delete(tempUserDataDir, true); } catch { }
                }
                Directory.CreateDirectory(tempUserDataDir);

                // Copy Local State and the selected profile folder
                string localStateSrc = Path.Combine(sourceUserDataDir, "Local State");
                string localStateDst = Path.Combine(tempUserDataDir, "Local State");
                if (File.Exists(localStateSrc))
                {
                    File.Copy(localStateSrc, localStateDst, true);
                }

                string profileSrc = Path.Combine(sourceUserDataDir, profileName);
                string profileDst = Path.Combine(tempUserDataDir, profileName);
                if (Directory.Exists(profileSrc))
                {
                    CopyDirectory(profileSrc, profileDst);
                }
                else
                {
                    Console.WriteLine($"Profile folder not found: {profileSrc}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to prepare temp user-data-dir: {ex.Message}");
            }

            options.AddArgument($"--user-data-dir={tempUserDataDir}");
            options.AddArgument($"--profile-directory={profileName}");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--start-maximized");
            options.PageLoadStrategy = PageLoadStrategy.Eager;

            // Ensure profile is not locked by leftover files
            TryUnlockChromeProfile(tempUserDataDir);

            driver = new ChromeDriver(options);

            // Configure timeouts for reliability
            try
            {
                driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1);
            }
            catch { }

        }
        static void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string dest = Path.Combine(destinationDir, Path.GetFileName(file));
                try { File.Copy(file, dest, true); } catch { }
            }
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                string destDir = Path.Combine(destinationDir, Path.GetFileName(dir));
                CopyDirectory(dir, destDir);
            }
        }
        static void TryUnlockChromeProfile(string userProfilePath)
        {
            try
            {
                // Small grace period after killing Chrome
                System.Threading.Thread.Sleep(1200);

                string[] possibleLockFiles = new[]
                {
                    "DevToolsActivePort",
                    "SingletonCookie",
                    "SingletonLock",
                    "SingletonSocket",
                    "SingletonSharedMemory"
                };

                foreach (var fileName in possibleLockFiles)
                {
                    string fullPath = Path.Combine(userProfilePath, fileName);
                    if (File.Exists(fullPath))
                    {
                        try { File.Delete(fullPath); Console.WriteLine($"Deleted leftover file: {fullPath}"); } catch { }
                    }
                }
            }
            catch { }
        }
        static string GetEnv(string key, string fallback = null)
        {
            try
            {
                var value = Environment.GetEnvironmentVariable(key);
                return string.IsNullOrWhiteSpace(value) ? fallback : value;
            }
            catch { return fallback; }
        }
        static void LoadEnv()
        {
            try
            {
                string envPath = FindFileUpwards(".env");
                if (string.IsNullOrEmpty(envPath) || !File.Exists(envPath)) return;
                foreach (var rawLine in File.ReadAllLines(envPath))
                {
                    var line = rawLine.Trim();
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                    int idx = line.IndexOf('=');
                    if (idx <= 0) continue;
                    string key = line.Substring(0, idx).Trim();
                    string value = line.Substring(idx + 1).Trim();
                    if ((value.StartsWith("\"") && value.EndsWith("\"")) || (value.StartsWith("'") && value.EndsWith("'")))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }
                    Environment.SetEnvironmentVariable(key, value);
                }
            }
            catch { }
        }
        static string FindFileUpwards(string fileName)
        {
            try
            {
                string current = AppDomain.CurrentDomain.BaseDirectory;
                for (int i = 0; i < 6; i++)
                {
                    string candidate = Path.Combine(current, fileName);
                    if (File.Exists(candidate)) return candidate;
                    var parent = Directory.GetParent(current);
                    if (parent == null) break;
                    current = parent.FullName;
                }
            }
            catch { }
            return null;
        }
        static void GoToChannel(string channel)
        {
            Console.WriteLine("Navigating to channel");
            Console.WriteLine($"{channel}");
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

            // Step 0: try hitting @me first to attach the web session
            try
            {
                driver.Navigate().GoToUrl("https://discord.com/channels/@me");
                Console.WriteLine($"Went to @me. URL: {driver.Url}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Preload @me failed: {ex.Message}");
            }

            // Step 1: navigate to the target channel
            bool reached = false;
            try
            {
                driver.Navigate().GoToUrl(channel);
                Console.WriteLine($"Requested channel. URL: {driver.Url}");
                reached = driver.Url.Contains("/channels/");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Navigate() error: {ex.Message}");
            }

            // Handle common interstitials (Open in browser / Continue)
            try
            {
                var shortWait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
                By[] buttons = new[]
                {
                    By.XPath("//*[contains(text(),'Open Discord in your browser')]")
                    , By.XPath("//*[contains(text(),'Continue to Discord')]")
                    , By.XPath("//*[contains(text(),'Continue in browser')]")
                };
                foreach (var locator in buttons)
                {
                    try
                    {
                        var el = shortWait.Until(ExpectedConditions.ElementToBeClickable(locator));
                        el.Click();
                        Console.WriteLine($"Clicked interstitial: {locator}");
                    }
                    catch { }
                }
            }
            catch { }

            if (!reached)
            {
                System.Threading.Thread.Sleep(1500);
                try { reached = driver.Url.Contains("/channels/"); Console.WriteLine($"After wait URL: {driver.Url}"); } catch { }
            }

            if (!reached)
            {
                try
                {
                    driver.Url = channel;
                    Console.WriteLine($"Fallback set Url. Current URL: {driver.Url}");
                    reached = driver.Url.Contains("/channels/");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fallback set Url error: {ex.Message}");
                }
            }

            if (!reached)
            {
                try
                {
                    var js = (IJavaScriptExecutor)driver;
                    js.ExecuteScript("window.location.href = arguments[0];", channel);
                    Console.WriteLine("Fallback JS redirect issued");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"JS redirect error: {ex.Message}");
                }
            }

            // Final confirmation wait (non-throwing)
            try
            {
                wait.Until(d => d.Url.Contains("/channels/"));
                Console.WriteLine($"Navigation confirmed. URL: {driver.Url}");
            }
            catch { }
        }
        static void CleanUp()
        {
            Console.WriteLine("EXECUTE ORDER 66");
            // Clean up
            try { if (huntT != null) { huntT.Stop(); huntT.Dispose(); } } catch { }
            try { if (workT != null) { workT.Stop(); workT.Dispose(); } } catch { }
            try { if (farmT != null) { farmT.Stop(); farmT.Dispose(); } } catch { }

            try { driver?.Quit(); } catch { }
        }
        static void StartBot()
        {
            Console.WriteLine("Starting at: " + DateTime.Now);
            //setup work
            switch (area)
            {
                case 3 or 4 or 5:
                    work = "rpg axe";
                    Console.WriteLine("Using: " + work);
                    break;
                case 6 or 7 or 8:
                    work = "rpg bowsaw";
                    Console.WriteLine("Using: " + work);
                    break;
                case 9 or 10 or 11 or 12 or 13:
                    work = "rpg chainsaw";
                    Console.WriteLine("Using: " + work);
                    break;
                default:
                    work = "rpg chop";
                    Console.WriteLine("Using: " + work);
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

            SetupCommandTimers();
        }
        static void SetupCommandTimers()
        {
            huntT = new Timer(huntCooldown);
            huntT.Elapsed += (sender, e) => SendCommand(hunt, 1);
            huntT.AutoReset = true;
            huntT.Enabled = true;


            workT = new Timer(workCooldown);
            workT.Elapsed += (sender, e) => SendCommand(work, 2);
            workT.AutoReset = true;
            workT.Enabled = true;


            if (area >= 4)
            {
                farmT = new Timer(farmCooldown);
                farmT.Elapsed += (sender, e) => SendCommand(farm, 3);
                farmT.AutoReset = true;
                farmT.Enabled = true;
                // farmRunning flag removed
            }
            checkMessageT = new Timer(2000);
            checkMessageT.Elapsed += (sender, e) => EventCheck(CheckLastMessage());
            checkMessageT.AutoReset = true;
            checkMessageT.Enabled = true;

            commandDelay = new Stopwatch();
            commandDelay.Start();
        }
        private static void SendMessage(string message)
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                // Locate the chat box element

                IWebElement chatBox = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("span[data-slate-node='text']")));

                // Type the message
                chatBox.SendKeys(message);
                // Submit the message

                chatBox = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("div[role='textbox']")));
                chatBox.SendKeys(Keys.Enter);

                Console.WriteLine(message + " sent at: " + DateTime.Now);
                //Check if cooldown
                System.Threading.Thread.Sleep(2001);

                EventCheck(CheckLastMessage());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SEND MESSAGE    An error occurred: {ex.Message}");
            }
        }
        private static void SendCommand(string command, int count)
        {
            
            try
            {
                
                if (command == "rpg hunt h" && huntMarryTracker == 0) //alternates between solo and together hunt
                {
                    huntMarryTracker = 1;
                }
                else if (command == "rpg hunt h" && huntMarryTracker == 1)
                {
                    command = "rpg hunt h";
                    huntMarryTracker = 0;
                }

                if (commandDelay.ElapsedMilliseconds > 2000) //checks if last command was sent more than 2secs ago.
                {
                    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                    // Locate the chat box element
                    IWebElement chatBox = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("span[data-slate-node='text']")));

                    // Type the message
                    chatBox.SendKeys(command);
                    count++;

                    // Submit the message
                    chatBox = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("div[role='textbox']")));
                    chatBox.SendKeys(Keys.Enter);
                    commandDelay.Restart();

                    Console.WriteLine(command + " number: " + count + " sent at: " + DateTime.Now);

                    //Check if cooldown
                    System.Threading.Thread.Sleep(2001);

                    EventCheck(CheckLastMessage());

                }
                else
                {
                    System.Threading.Thread.Sleep(2000);
                    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                    // Locate the chat box element
                    IWebElement chatBox = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("span[data-slate-node='text']")));

                    // Type the message
                    chatBox.SendKeys(command);
                    count++;
                    commandDelay.Restart();

                    // Submit the message
                    chatBox = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("div[role='textbox']")));
                    chatBox.SendKeys(Keys.Enter);

                    Console.WriteLine(command + " number: " + count + " sent at: " + DateTime.Now);

                    //Check if cooldown
                    System.Threading.Thread.Sleep(2001);

                    EventCheck(CheckLastMessage());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SEND MESSAGE    An error occurred: {ex.Message}");
            }
        }
        static void EventCheck(string message)
        {
            //Console.WriteLine("Event checking with message : " + message);
            if (message.Contains("Select the item of the image above or respond with the item name")) //Guard
            {
                ShowToastNotification("POLICIEEE", "Guard seen at: " + DateTime.Now);
                Console.WriteLine("AAAAAAAAAAA PANIC AAAAAAAAAAAAAAAAAAAA");
            }
            else if (message.Contains("TEST"))
            {
                Console.WriteLine("I hear you");
                ShowToastNotification("I hear you", "" + DateTime.Now);
                SendMessage("I hear you: " + DateTime.Now);
            }
            else if (message.Contains("BOT HELP"))
            {
                Console.WriteLine("BOT HELP");
                SendMessage("Change work - chop / axe / bowsaw / chainsaw ");
                SendMessage("Change farm - farm / potato / carrot / bread");
                SendMessage("bot farming - will start farming");
            }
            else if (message.Contains("STOP")) //stop everything
            {
                Console.WriteLine("STOP");
                StopCommands();
            }
            else if (message.Contains("START")) //start again
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
            else if (message.Contains("wait at least")) //msg Cooldown
            {
                Console.WriteLine("Cooldown wait x");
            }
            else if (message.Contains("CHANGE WORK"))
            {
                if (message.Contains("CHOP"))
                {
                    SendMessage("I am chopping treez");
                    work = "rpg chop";
                }
                else if (message.Contains("AXE"))
                {
                    SendMessage("I am using an axe");
                    work = "rpg axe";
                }
                else if (message.Contains("BOWSAW"))
                {
                    SendMessage("I am using a bowsaw");
                    work = "rpg bowsaw";
                }
                else if (message.Contains("CHAINSAW"))
                {
                    SendMessage("I am using a chainsaw");
                    work = "rpg chainsaw";
                }
            }
            else if (message.Contains("CHANGE FARM"))
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
            else if (message.Contains("BOT FARMING"))
            {
                SendMessage("I am farming");
                farmT = new Timer(farmCooldown);
                farmT.Elapsed += (sender, e) => SendCommand(farm, 3);
                farmT.AutoReset = true;
                farmT.Enabled = true;
                // farmRunning flag removed
            }
            else if (message.Contains("You were about to hunt a defenseless monster, but then you notice a zombie horde coming your way"))
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
            else if (message.Contains("IT'S RAINING COINS"))
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
            else if (message.Contains("A LOOTBOX SUMMONING HAS")) //lootboxes
            {
                Console.WriteLine("Lootbox event at: " + DateTime.Now);
                SendMessage("SUMMON");
                
            }
            else if (message.Contains("A LEGENDARY BOSS JUST SPAWNED"))
            {
                Console.WriteLine("Boss event at: " + DateTime.Now);
                SendMessage("TIME TO FIGHT");
            }

        }
        static void SolveTraining()
        {

        }
        static void StopCommands()
        {
            if (workT != null) workT.Stop();
            if (huntT != null) huntT.Stop();
            if (farmT != null) farmT.Stop();
        }
        static void StartCommands()
        {
            if (workT != null) workT.Start();
            if (huntT != null) huntT.Start();
            if (farmT != null) farmT.Start();
        }
        static string CheckLastMessage()
        {
            try
            {

                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(2));
                IList<IWebElement> messages = driver.FindElements(By.CssSelector("li[id^='chat-messages-']"));

                if (messages.Count == 0)
                {
                    //onsole.WriteLine("No messages found");
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
            Console.WriteLine("No message found in CheckLastMessage");
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
            Console.WriteLine("Checking if logged in");
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
            new ToastContentBuilder()
                .AddArgument("action", "viewApp")
                .AddText(notificationTitle)
                .AddText(notificationBody)
                .AddButton(new ToastButton()
                    .SetContent("Bring to Front")
                    .AddArgument("action", "bringToFront"))
                .AddAudio(new Uri("ms-winsoundevent:Notification.Default"))
                .Show();

        }
    }
}
