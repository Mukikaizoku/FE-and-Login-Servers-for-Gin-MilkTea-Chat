using System;
using System.Collections.Generic;

namespace ChatServer.Managers
{
    /// <summary>
    /// The CookieJarManager Class controls a static cookieJar dictionary which stores all active cookie values.
    /// </summary>
    class CookieJarManager
    {
        /// <summary>
        /// The CookieInfo struct contains the cookie value and expirationDate.
        /// </summary>
        struct CookieInfo
        {
            public int cookie;
            public DateTime expirationDate;

            public CookieInfo (int cookieNumber, DateTime expireDate)
            {
                cookie = cookieNumber;
                expirationDate = expireDate;
            }
        }

        private static Dictionary<string, CookieInfo> cookieJar = new Dictionary<string, CookieInfo>();

        /// <summary>
        /// The CheckExpirationDate method checks if a cookie is expired. Returns false if expired and true if still valid. Provides the cookie's information.
        /// </summary>
        /// <param name="id">The id of the cookie.</param>
        /// <param name="tempCookie">The cookie's information structure to be returned.</param>
        /// <returns></returns>
        private bool CheckExpirationDate (string id, out CookieInfo tempCookie)
        {
            DateTime now = DateTime.UtcNow;
            tempCookie = new CookieInfo();

            if (cookieJar.TryGetValue(id, out tempCookie))
            {
                if (tempCookie.expirationDate < now)    //Not expired yet
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// The ValidateCookie method takes a user id and their claimed cookie and determines it's validity by tasting the cookie jar. 
        /// Returns true on validation and false on invalid cookie, expired cookie, or an invalid argument.
        /// </summary>
        /// <param name="id">The client's ID.</param>
        /// <param name="testCookie">The suspect cookie value.</param>
        /// <returns>Returns true on validation and false on invalid cookie, expired cookie, or an invalid argument.</returns>
        public bool ValidateCookie (string id, int testCookie)
        {
            if (id == null || testCookie == 0)
            {
                return false;                                                      //Return false on Invalid Argument (null id or 0-cookie)
            }
            CookieInfo trueCookie;
            if (CheckExpirationDate(id, out trueCookie))
            {
                if (testCookie == trueCookie.cookie)
                {
                    RemoveCookie(id);
                    return true;                                                   //Cookie has been validated
                }
            }
            RemoveCookie(id);
            return false;                                                          //Cookie was invalid or expired, return false
        }

        /// <summary>
        /// The AddCookie method adds a CookieInfo structure to the cookie-jar with an appropriate time out value.
        /// Returns -1 on an invalid argument, 0 on a successful new id cookie add, and 1 on a successful cookie add update.
        /// </summary>
        /// <param name="id">The user's ID name.</param>
        /// <param name="cookieNumber">The cookie value.</param>
        /// <returns>Returns -1 on an invalid argument, 0 on a successful new id cookie add, and 1 on a successful cookie add update.</returns>
        public int AddCookie (string id, int cookieNumber)
        {
            if (id == null || cookieNumber == 0)
            {
                return -1;                                                      //Return -1 on Invalid Argument (null id or 0-cookie)
            }
            DateTime now = DateTime.UtcNow;
            now.AddMinutes(10);                                                 //Add a 10 minute expiration
            CookieInfo cookieInfo = new CookieInfo(cookieNumber, now);
            if (cookieJar.ContainsKey(id))                                      //To prevent an argument exception with the dictionary.Add, we check if the key is already contained
            {
                cookieJar.Remove(id);
                cookieJar.Add(id, cookieInfo);
                return 1;                                                       //Return 1 on success full cookie add update
            } else
            {
                cookieJar.Add(id, cookieInfo);
            }
            return 0;                                                           //Return 0 on successful new id cookie add

        }

        /// <summary>
        /// The RemoveCookie method removes a single cookie from the cookie jar.
        /// </summary>
        /// <param name="id">The user's ID name.</param>
        /// <returns>Returns true on success and false on failure.</returns>
        public bool RemoveCookie (string id)
        {
            return cookieJar.Remove(id);
        }

        /// <summary>
        /// The PrintCookieList method prints all cookies in the cookie jar to the console.
        /// </summary>
        public static void PrintCookieList ()
        {
            Console.WriteLine("Cookies:");
            foreach (KeyValuePair<string,CookieInfo> cookie in cookieJar)
            {
                Console.Write("\tID:\t\t" + cookie.Key);
                Console.Write("\tCookie:\t" + cookie.Value.cookie);
                Console.Write("\tExpDate:\t" + cookie.Value.expirationDate);
                Console.WriteLine();
            }
        }
    }
}
