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

namespace DiscordERPGAutoTyper
{
    internal class Program
    {
        static IWebDriver driver;
        static Timer timer1;
        static Timer timer2;
        static readonly string baseURL = "https://discord.com";
        static readonly string discordChannelAuto = "https://discord.com/channels/1180781501940518932/1264330502005985391";

        static string hunt = "rpg hunt";
        static string work = "rpg chainsaw";
        static string farm = "rpg farm";
        
        static int huntCount = 0;
        static int workCount = 0;
        static int farmCount = 0;

        
        static void Main(string[] args)
        {
            string password = "pass";
            string email = "email";

            Initialize();
            //Login(email,password);
            GoToChannel(discordChannelAuto);
            // Set up the first timer to send "hi" every 20 seconds
            timer1 = new Timer(20000); // 20000 milliseconds = 20 seconds
            timer1.Elapsed += (sender, e) => SendMessage(hunt, huntCount); // po prvním sendu se to rozbije
            timer1.AutoReset = true;
            timer1.Enabled = true;

            // Set up the second timer to send "Hey" every 42 seconds
            /*timer2 = new Timer(42000); // 42000 milliseconds = 42 seconds
            timer2.Elapsed += (sender, e) => SendMessage("Hey");
            timer2.AutoReset = true;
            timer2.Enabled = true;*/

            // Keep the application running to allow the timers to fire
            Console.WriteLine("Press [Enter] to exit the program...");
            Console.ReadLine();

            // Clean up
            timer1.Stop();
            timer1.Dispose();
            //timer2.Stop();
            //timer2.Dispose();
            driver.Quit();
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
        void PrvniStart()
        {

        }
        private static void SendMessage(string command, int commandCount)
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                // Locate the chat box element

                IWebElement chatBox = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("span[data-slate-node='text']")));
                // Clear the chat box
                Console.WriteLine("vyčistil jsem pole");
                chatBox.Clear();

                // Type the message
                Console.WriteLine("píšu příkaz");
                chatBox.SendKeys(command);
                //System.Threading.Thread.Sleep(500);
                // Submit the message

                chatBox = wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("span[data-slate-node='text']")));
                Console.WriteLine("Posílám zprávu");
                chatBox.SendKeys(Keys.Enter);
                Console.WriteLine("Poslal jsem zprávu");
                Console.WriteLine(command + " number: " + commandCount++ + " sent at: " + DateTime.Now );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
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
    }
}
