using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace WheelWatcher.Library
{
    internal class Validations
    {
        /// <summary>
        /// Tries to find an element from a given element
        /// </summary>
        /// <param name="elementToSearch">The element to start the search from</param>
        /// <param name="locator">The element to check is visible or not</param>
        /// <returns>The element found</returns>
        public static Func<IWebDriver, IWebElement> ElementIsLoaded(IWebElement elementToSearch, By locator)
        {
            return delegate (IWebDriver driver)
            {
                try
                {
                    return elementToSearch.FindElement(locator);
                }
                catch (StaleElementReferenceException)
                {
                    return null;
                }
            };
        }

        /// <summary>
        /// Tries to find an element from a given element
        /// </summary>
        /// <param name="elementToSearch">The element to start the search from</param>
        /// <param name="locator">The element to check is visible or not</param>
        /// <returns>The elements found</returns>
        public static Func<IWebDriver, ReadOnlyCollection<IWebElement>> PresenceOfAllElementsLocatedBy(IWebElement elementToSearch, By locator)
        {
            return delegate (IWebDriver _)
            {
                try
                {
                    ReadOnlyCollection<IWebElement> readOnlyCollection = elementToSearch.FindElements(locator);
                    return readOnlyCollection.Any() ? readOnlyCollection : null;
                }
                catch (StaleElementReferenceException)
                {
                    return null;
                }
            };
        }

        /// <summary>
        /// Checks within the specified timeout to see if the element passed in is not visible on screen
        /// </summary>
        /// <param name="eventLog">The event log to log data to</param>
        /// <param name="driver">Web browser driver to interact with</param>
        /// <param name="elementToFind">The element to check is not visible or not</param>
        /// <param name="element">The element that was found</param>
        /// <returns>Returns true if the element passed in is not visible on screen within the allotted timeout, else false</returns>
        public static bool IsElementLoaded(EventLog eventLog, IWebDriver driver, By elementToFind, out IWebElement element) =>
            WaitForElementToMeetCondition(eventLog, driver, ExpectedConditions.ElementExists(elementToFind), out element);

        /// <summary>
        /// Checks within the specified timeout to see if the element passed in is not visible on screen
        /// </summary>
        /// <param name="eventLog">The event log to log data to</param>
        /// <param name="driver">Web browser driver to interact with</param>
        /// <param name="elementToSearch">The element to start the search from</param>
        /// <param name="elementToFind">The element to check is not visible or not</param>
        /// <param name="element">The element that was found</param>
        /// <returns>Returns true if the element passed in is not visible on screen within the allotted timeout, else false</returns>
        public static bool IsElementLoaded(EventLog eventLog, IWebDriver driver, IWebElement elementToSearch, By elementToFind, out IWebElement element) =>
            WaitForElementToMeetCondition(eventLog, driver, ElementIsLoaded(elementToSearch, elementToFind), out element);

        /// <summary>
        /// Checks within the specified timeout to see if the first element passed in is loaded on screen
        /// </summary>
        /// <param name="eventLog">The event log to log data to</param>
        /// <param name="driver">Web browser driver to interact with</param>
        /// <param name="elementToFind">The element to check is visible or not</param>
        /// <param name="elements">The elements that were found after waiting for the first element returned it to meet the specified condition</param>
        /// <returns>Returns true if the element passed in is visible on screen within the allotted timeout, else false</returns>
        public static bool IsFirstElementLoaded(EventLog eventLog, IWebDriver driver, By elementToFind, out ReadOnlyCollection<IWebElement> elements) =>
            WaitForElementToMeetCondition(eventLog, driver, ExpectedConditions.PresenceOfAllElementsLocatedBy(elementToFind), out elements);

        /// <summary>
        /// Checks within the specified timeout to see if the first element passed in is loaded on screen
        /// </summary>
        /// <param name="eventLog">The event log to log data to</param>
        /// <param name="driver">Web browser driver to interact with</param>
        /// <param name="elementToSearch">The element to start the search from</param>
        /// <param name="elementToFind">The element to check is visible or not</param>
        /// <param name="elements">The elements that were found after waiting for the first element returned it to meet the specified condition</param>
        /// <returns>Returns true if the element passed in is visible on screen within the allotted timeout, else false</returns>
        public static bool IsFirstElementLoaded(EventLog eventLog, IWebDriver driver, IWebElement elementToSearch, By elementToFind, out ReadOnlyCollection<IWebElement> elements) =>
            WaitForElementToMeetCondition(eventLog, driver, PresenceOfAllElementsLocatedBy(elementToSearch, elementToFind), out elements);

        /// <summary>
        /// Checks within the specified timeout to wait for an element to meet the specified condition
        /// </summary>
        /// <param name="eventLog">The event log to log data to</param>
        /// <param name="driver">Web browser driver to interact with</param>
        /// <param name="condition">The condition that you want the element to wait for</param>
        /// <param name="element">The element that was found after waiting for it to meet the specified condition</param>
        /// <returns>Returns true if the element passed in is visible on screen within the allotted timeout, else false</returns>
        public static bool WaitForElementToMeetCondition<T>(EventLog eventLog, IWebDriver driver,
            Func<IWebDriver, T> condition, out T element)
        {
            var timeout = new TimeSpan(0, 0, 3);

            element = default;
            try
            {
                //Set timeout to incoming value whilst looking for the element
                //driver.Manage().Timeouts().ImplicitWait = timeout;

                //Set a wait for the timeout period
                var wait = new WebDriverWait(driver, timeout);

                //Wait for the element to meet the incoming condition
                element = wait.Until(condition);
            }
            catch (WebDriverTimeoutException)
            {
                //If it was a timeout, log and return false as the condition was not met
                eventLog.WriteEntry($"Element not found after {timeout}", EventLogEntryType.Warning);

                return false;
            }
            catch (StaleElementReferenceException)
            {
                //If it was a StaleElementReferenceException, log and return false
                //If using the WaitUntilElement interaction methods, then this will
                //mean a retry and might cause the issue to be resolved on the next run
                return false;
            }
            catch (Exception exception)
            {
                eventLog.WriteEntry($"Error getting element:{Environment.NewLine}{Environment.NewLine}{exception}", EventLogEntryType.Error);
            }
            //finally
            //{
            //    //Set timeout back to Context default, or 1 minute
            //    driver.Manage().Timeouts().ImplicitWait = new TimeSpan(0, 0, 60);
            //}

            return true;
        }
    }
}
