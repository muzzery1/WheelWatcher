using OpenQA.Selenium;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace WheelWatcher.Library
{
    internal class Interactions
    {
        /// <summary>
        /// Gets all elements if the 1st item in the elements to get is loaded
        /// </summary>
        /// <param name="eventLog">The event log to log data to</param>
        /// <param name="driver">Web browser driver to interact with</param>
        /// <param name="elementsToGet">The element to check is not visible or not</param>
        /// <returns>The web element to find. If the element is not loaded, it returns a failure</returns>
        public static ReadOnlyCollection<IWebElement> GetElementsIfLoaded(EventLog eventLog, IWebDriver driver, By elementsToGet)
        {
            //See if the element is visible. If it is not, throw an error
            if (!Validations.IsFirstElementLoaded(eventLog, driver, elementsToGet, out var elements))
            {
                var message = $"Unable to get elements {elementsToGet} as the 1st item in the elements to get was not loaded";
                eventLog.WriteEntry(message, EventLogEntryType.Error);
                Console.WriteLine(message);
            }

            //Return the elements
            return elements;
        }

        /// <summary>
        /// Gets all elements if the 1st item in the elements to get is loaded
        /// </summary>
        /// <param name="eventLog">The event log to log data to</param>
        /// <param name="driver">Web browser driver to interact with</param>
        /// <param name="elementToSearch">The element to start the search from</param>
        /// <param name="elementsToGet">The element to check is not visible or not</param>
        /// <returns>The web element to find. If the element is not loaded, it returns a failure</returns>
        public static ReadOnlyCollection<IWebElement> GetElementsIfLoaded(EventLog eventLog, IWebDriver driver, IWebElement elementToSearch, By elementsToGet)
        {
            //See if the element is visible. If it is not, throw an error
            if (!Validations.IsFirstElementLoaded(eventLog, driver, elementToSearch, elementsToGet, out var elements))
            {
                var message = $"Unable to get elements {elementsToGet} as the 1st item in the elements to get was not loaded";
                eventLog.WriteEntry(message, EventLogEntryType.Error);
                Console.WriteLine(message);
            }

            //Return the elements
            return elements;
        }

        /// <summary>
        /// Gets an element if it is loaded
        /// </summary>
        /// <param name="eventLog">The event log to log data to</param>
        /// <param name="driver">Web browser driver to interact with</param>
        /// <param name="elementToSearch">The element to start the search from</param>
        /// <param name="elementsToGet">The element to check is not visible or not</param>
        /// <returns>The web element to find. If the element is not loaded, it returns a failure</returns>
        public static IWebElement GetElementIfLoaded(EventLog eventLog, IWebDriver driver, IWebElement elementToSearch, By elementsToGet)
        {
            //See if the element is visible. If it is not, throw an error
            if (!Validations.IsElementLoaded(eventLog, driver, elementToSearch, elementsToGet, out var elements))
            {
                var message = $"Unable to get elements {elementsToGet} as the 1st item in the elements to get was not loaded";
                eventLog.WriteEntry(message, EventLogEntryType.Error);
                Console.WriteLine(message);
            }

            //Return the elements
            return elements;
        }
    }
}
