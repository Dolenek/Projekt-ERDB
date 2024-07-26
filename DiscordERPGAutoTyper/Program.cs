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

namespace DiscordERPGAutoTyper
{
    internal class Program
    {
        static IWebDriver driver;
        static Timer huntT;
        static Timer workT;
        static Timer farmT;
        static readonly string baseURL = "https://discord.com";
        static readonly string discordChannelAuto = "https://discord.com/channels/1180781501940518932/1264330502005985391";


        static Queue<string> messageQueue = new Queue<string>();
        static string hunt = "rpg hunt";
        static string work = "rpg chop";
        static string farm = "rpg farm";

        static int area = 3;

        
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

            huntT = new Timer(20000); // 20000 milliseconds = 20 seconds
            huntT.Elapsed += (sender, e) => QueueMessage(hunt); 
            huntT.AutoReset = true;
            huntT.Enabled = true;

            workT = new Timer(97000); 
            workT.Elapsed += (sender, e) => QueueMessage(work);
            workT.AutoReset = true;
            workT.Enabled = true;
            if (area >= 4)
            {
                farmT = new Timer(192000);
                farmT.Elapsed += (sender, e) => QueueMessage(farm);
                farmT.AutoReset = true;
                farmT.Enabled = true;
            }
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
            
            var options = new ChromeOptions();
            // Using a user profile

            string userProfile = @"C:\Users\thesa\AppData\Local\Google\Chrome\User Data";
            string profileName = "Profile 1"; // Or the name of the profile you want to use
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
            Console.WriteLine("Startuju v: " + DateTime.Now);
            //setup work
            switch (area)
            {   
                case 3 or 4 or 5:
                    work = "rpg axe";
                    Console.WriteLine("Používám: " + work);
                    break;
                case 6 or 7 or 8:
                    work = "rpg pickaxe";
                    Console.WriteLine("Používám: " + work);
                    break;
                case 9 or 10 or 11:
                    work = "rpg chainsaw";
                    Console.WriteLine("Používám: " + work);
                    break;
                default:
                    work = "rpg chop";
                    Console.WriteLine("Používám: "+work);
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
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
        private static string GetLastMessageContent()
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                IWebElement message = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("")));

                return "xd";
            }
            catch
            {

            }
            return ":(";
        }

        private static void CheckGuard()
        {
            
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
                Console.WriteLine("tady");
                IWebElement discordDetected = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[text()='Discord App Detected']")));
                Console.WriteLine("tady1");
                if (discordDetected != null)
                {
                    Console.WriteLine("tady2");
                    return true;
                }
                    
            }
            catch
            {

            }
            Console.WriteLine("tady3");
            return false;
        }
    }
}
