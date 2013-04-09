using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using eRepublikSitter.Market;
using eRepublikSitter.Wars;

namespace eRepublikSitter
{
    public class UserActionProcessor
    {
        public UserActionProcessor(UserDetails details)
        {
            this.m_userDetails = details;
        }

        public void Process()
        {
            this.m_cookieContainer = new CookieContainer();
            this.m_performedActions = new List<Actions>();

            Console.WriteLine("Processing " + this.m_userDetails.CitizenName + ": ");

            this.FetchAuthToken();
            this.Login();
            this.PopulateCountries();
            this.PopulateProfile();
            this.PerformActions();

            Console.WriteLine("Done\n");
        }

        #region Create Special Requests
        private HttpWebRequest CreateLoginRequest()
        {
            string postData = string.Format("_token={0}&citizen_name={1}&citizen_password={2}&commit=Login",
                this.m_authToken,
                HttpUtility.UrlEncode(this.m_userDetails.CitizenName),
                this.m_userDetails.Password);

            return WebRequestManager.CreateCustomPostRequest("http://www.erepublik.com/en/login", postData, this.m_cookieContainer);
        }
        #endregion

        private string GetAccountID(string marketUrl)
        {
            string responseText = WebRequestManager.SendGenericWebRequest(marketUrl, this.m_cookieContainer);

            string value = string.Empty;
            // Find all matches in web response.
            MatchCollection m1 = Regex.Matches(responseText, @"(<input type=\""hidden\"" name=\""account\"" id=\""account\"" .*?>)", RegexOptions.Singleline);

            // Loop over each match.
            foreach (Match m in m1)
            {
                string matchValue = m.Groups[1].Value;

                // Get value attribute.
                Match valueMatch = Regex.Match(matchValue, @"value=\""(.*?)\""", RegexOptions.Singleline);
                if (valueMatch.Success)
                {
                    value = valueMatch.Groups[1].Value;
                }
            }

            return value;
        }

        private string GetAuthToken()
        {
            string responseText = WebRequestManager.SendGenericWebRequest("http://www.erepublik.com", this.m_cookieContainer);

            string value = string.Empty;
            // Find all matches in web response.
            MatchCollection m1 = Regex.Matches(responseText, @"(<input type=\""hidden\"" id=\""_token\"" name=\""_token\"" .*?>)", RegexOptions.Singleline);

            // Loop over each match.
            foreach (Match m in m1)
            {
                string matchValue = m.Groups[1].Value;

                // Get value attribute.
                Match valueMatch = Regex.Match(matchValue, @"value=\""(.*?)\""", RegexOptions.Singleline);
                if (valueMatch.Success)
                {
                    value = valueMatch.Groups[1].Value;
                }
            }

            return value;
        }

        private void PopulateAdvisorFullText()
        {
            string advisorUrl = "http://www.erepublik.com/en/advisor_ajax/none/home/index/undefined";

            Console.Write("Checking for available actions to perform... ");
            this.m_advisorFullText = WebRequestManager.SendGenericWebRequest(advisorUrl, this.m_cookieContainer);
            Console.WriteLine("Done.");
        }

        private string GetActionString()
        {
            string value = string.Empty;

            // Find paragraph text in response
            Match m1 = Regex.Match(m_advisorFullText, @"(<p>.*?</p>)", RegexOptions.Singleline);
            string matchValue = m1.Groups[1].Value;

            // Strip HTML
            return Regex.Replace(matchValue, @"<(.|\n)*?>", string.Empty);
        }

        private void DetermineNextAction()
        {
            PopulateAdvisorFullText();

            string actionString = GetActionString();
            switch (actionString)
            {
                case ACTION_WORK_ADVICE_STRING:
                    PerformAction(Actions.Work);
                    break;
                case ACTION_TRAIN_ADVICE_STRING:
                    PerformAction(Actions.Train);
                    break;
                case ACTION_FIGHT_ADVICE_STRING:
                    PerformAction(Actions.Fight);
                    break;
                case ACTION_BUYFOOD_ADVICE_STRING:
                    PerformAction(Actions.BuyFood);
                    break;
                case NO_ACTIONS_LEFT_ADVICE_STRING:
                default:
                    PerformAction(Actions.NoAction);
                    break;
            }
        }

        #region Advisor Actions
        private void PerformAction(Actions action)
        {
            bool result = false;
            switch (action)
            {
                case Actions.Work:
                    Console.Write(string.Format(PERFORM_ACTION_FORMAT, "Working"));
                    result = PerformAdvisorAction_Work();
                    this.m_performedActions.Add(Actions.Work);
                    Console.WriteLine(result ? "Done." : "Failed.");
                    break;
                case Actions.Train:
                    Console.Write(string.Format(PERFORM_ACTION_FORMAT, "Training"));
                    result = PerformAdvisorAction_Train();
                    this.m_performedActions.Add(Actions.Train);
                    Console.WriteLine(result ? "Done." : "Failed.");
                    break;
                case Actions.Fight:
                    Console.Write(string.Format(PERFORM_ACTION_FORMAT, "Fighting"));
                    result = PerformAdvisorAction_Fight();
                    this.m_performedActions.Add(Actions.Fight);
                    Console.WriteLine(result ? "Done." : "Failed.");
                    break;
                case Actions.FindHigherPayingJob:
                    Console.Write(string.Format(PERFORM_ACTION_FORMAT, "Checking for a better job"));
                    result = PerformAdvisorAction_FindHigherPayingJob();
                    this.m_performedActions.Add(Actions.FindHigherPayingJob);
                    Console.WriteLine(result ? "Done." : "Failed.");
                    break;
                case Actions.BuyFood:
                    Console.Write(string.Format(PERFORM_ACTION_FORMAT, "Buying Food"));
                    result = PerformAdvisorAction_BuyFood();
                    this.m_performedActions.Add(Actions.BuyFood);
                    Console.WriteLine(result ? "Done." : "Failed.");
                    break;
                case Actions.NoAction:
                default:
                    Console.WriteLine("There are no matching actions left to do today. Exiting.");
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Edge Cases:
        /// No money left to pay you
        /// No materials left to build stuff
        /// </remarks>
        private bool PerformAdvisorAction_Work()
        {
            try
            {
                WebRequestManager.SendGenericWebRequest("http://www.erepublik.com/en/work", this.m_cookieContainer);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool PerformAdvisorAction_Train()
        {
            string trainActionUrl = "http://www.erepublik.com/en/my-places/train";
            string postData = string.Format("_token={0}", this.m_authToken);

            // Not sure why this is required - but will 404 if it's not here.
            WebRequestManager.SendGenericWebRequest("http://www.erepublik.com/en/my-places/army", this.m_cookieContainer);

            try
            {
                HttpWebRequest request = WebRequestManager.CreateCustomPostRequest(trainActionUrl, postData, this.m_cookieContainer);
                WebRequestManager.SendCustomWebRequest(request);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool PerformAdvisorAction_BuyFood()
        {
            List<string> allPagesFullText = new List<string>();
            int pageIndex = 1;

            string baseMarketUrl = string.Format(string.Format("http://www.erepublik.com/en/market/country-{0}-industry-{1}-quality-0", this.m_country.ID, FOOD_INDUSTRY_ID, pageIndex));
            string citizenAccountIDString = GetAccountID(baseMarketUrl);

            string baseFoodUrl = string.Format("http://www.erepublik.com/en/market/country-{0}-industry-{1}-quality-0-{2}", this.m_country.ID, FOOD_INDUSTRY_ID, citizenAccountIDString);

            // Pull data for page #1
            string foodMarketUrl = string.Format("{0}/{1}", baseFoodUrl, pageIndex);
            string foodMarketPageResponse = WebRequestManager.SendGenericWebRequest(foodMarketUrl, this.m_cookieContainer);
            allPagesFullText.Add(foodMarketPageResponse);
            pageIndex++;

            // Find the last page
            string lastPageRegex = string.Format(@"<a href=\""/en/market/country-{0}-industry-{1}-quality-0-{2}/(.*?)\"" class=\""last\"" .*?>", this.m_country.ID, FOOD_INDUSTRY_ID, citizenAccountIDString);
            Match lastPageMatch = Regex.Match(foodMarketPageResponse, lastPageRegex, RegexOptions.Multiline);
            string lastPageMatchValue = lastPageMatch.Groups[1].Value;
            int lastPageIndex = int.Parse(lastPageMatchValue);

            // Pull data for remaining pages, add to list
            for (int i = pageIndex; pageIndex <= lastPageIndex; pageIndex++)
            {
                foodMarketUrl = string.Format("{0}/{1}", baseFoodUrl, pageIndex);
                foodMarketPageResponse = WebRequestManager.SendGenericWebRequest(foodMarketUrl, this.m_cookieContainer);
                allPagesFullText.Add(foodMarketPageResponse);
            }

            List<FoodOffer> foodOffers = new List<FoodOffer>();
            foreach (string fullPageResponse in allPagesFullText)
            {
                string name = string.Empty;
                string amountId = string.Empty;
                int quality = 0;
                int stock = 0;
                decimal price = 0;

                string trimmedResponse = fullPageResponse.Replace("\n", "").Replace("\t", "");

                // Find all matches in web response.
                MatchCollection m1 = Regex.Matches(trimmedResponse, "(<tr><td valign=\"middle\" nowrap=\"nowrap\"><div class=\"entity\">(.*?)</tr>)", RegexOptions.Singleline);

                // Loop over each match.
                foreach (Match m in m1)
                {
                    string matchValue = m.Groups[1].Value;
                    Match nameMatch = Regex.Match(matchValue, "<a class=\"nameholder dotted\" title=\"(.*?)\".*?</a>", RegexOptions.Singleline);
                    Match qualityMatch = Regex.Match(matchValue, "<td><span class=\"qlmeter\"><span class=\"qllevel\" style=\"(.*?)\"><img src=\"/images/parts/ql-indicator_full.gif\" alt=\"Quality Level\"  title=\"Quality\" />", RegexOptions.Singleline);
                    Match stockMatch = Regex.Match(matchValue, "<td><span class=\"special\">(.*?)</span></td>", RegexOptions.Singleline);
                    Match priceMatch = Regex.Match(matchValue, "<td width=\"110\"><span class=\"special\">(.*?)<span class=\"currency\">", RegexOptions.Singleline);
                    Match amountIdMatch = Regex.Match(matchValue, "<input type=\"text\" name=\"amount_offer\" id=\"(.*?)\"", RegexOptions.Singleline);

                    if (nameMatch.Success)
                        name = nameMatch.Groups[1].Value;

                    if (qualityMatch.Success)
                    {
                        string qualityStyleValue = qualityMatch.Groups[1].Value;

                        switch (qualityStyleValue)
                        {
                            case "width: 20%":
                                quality = 1;
                                break;
                            case "width: 40%":
                                quality = 2;
                                break;
                            case "width: 60%":
                                quality = 3;
                                break;
                            case "width: 90%":
                                quality = 4;
                                break;
                            case "width: 100%":
                                quality = 5;
                                break;
                            default:
                                break;
                        }
                    }

                    if (stockMatch.Success)
                        stock = int.Parse(stockMatch.Groups[1].Value);

                    if (priceMatch.Success)
                        price = decimal.Parse(priceMatch.Groups[1].Value.Replace("</span><sup>", string.Empty).Replace("</sup> ", string.Empty));

                    if (amountIdMatch.Success)
                        amountId = amountIdMatch.Groups[1].Value;

                    int offerId = int.Parse(amountId.Split('_')[1]);

                    FoodOffer offer = new FoodOffer(offerId, this.m_country.ID, FOOD_INDUSTRY_ID, price, stock, name);
                    foodOffers.Add(offer);
                }
            }

            if (foodOffers.Count <= 0)
                throw new Exception("Unable to find any food offers");

            foodOffers.Sort(delegate(FoodOffer offer1, FoodOffer offer2) { return offer1.Price.CompareTo(offer2.Price); });

            FoodOffer bestOffer = foodOffers[0];

            string postData = string.Format("_token={0}&offer_id={1}&amount=1&account={2}", this.m_authToken, bestOffer.OfferID, citizenAccountIDString);

            try
            {
                HttpWebRequest request = WebRequestManager.CreateCustomPostRequest(foodMarketUrl, postData, this.m_cookieContainer);
                WebRequestManager.SendCustomWebRequest(request);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Edge Cases:
        /// The war was over before we got there
        /// Not enough wellness to fight
        /// </remarks>
        private bool PerformAdvisorAction_Fight()
        {
            List<War> warList = new List<War>();
            string allWarsForCountryUrl = "http://www.erepublik.com/en/wars";
            string postData = string.Format("_token={0}&fwt=all&fws=active&fwc={1}", this.m_authToken, this.m_country.ID);
            string fullPageResponse = string.Empty;

            try
            {
                HttpWebRequest request = WebRequestManager.CreateCustomPostRequest(allWarsForCountryUrl, postData, this.m_cookieContainer);
                fullPageResponse = WebRequestManager.SendCustomWebRequest(request);
            }
            catch
            {
                return false;
            }

            string trimmedResponse = this.ScrubXMLString(fullPageResponse);

            // Find all matches in web response.
            MatchCollection m1 = Regex.Matches(trimmedResponse, "(<div class=\"warholder\">(.*?)((</div>){2}))", RegexOptions.Singleline);

            // Loop over each match.
            foreach (Match m in m1)
            {
                string matchValue = m.Groups[1].Value;
                Match middleMatch = Regex.Match(matchValue, "<div class=\"middle\">(.*?)((</div>){1})", RegexOptions.Singleline);
                Match attackerMatch = Regex.Match(matchValue, "<div class=\"attacker\">(.*?)<div class=\"middle\">", RegexOptions.Singleline);
                Match defenderMatch = Regex.Match(matchValue, "<div class=\"defender\">(.*?)<div class=\"attacker\">", RegexOptions.Singleline);

                // Validate we parsed everything out correctly
                if (!middleMatch.Success)
                    throw new Exception("Unable to parse 'middle' element. Has the schema changed?\nParsing out of:\n" + matchValue);
                if (!attackerMatch.Success)
                    throw new Exception("Unable to parse 'attacker' element. Has the schema changed?\nParsing out of:\n" + matchValue);
                if (!defenderMatch.Success)
                    throw new Exception("Unable to parse 'defender' element. Has the schema changed?\nParsing out of:\n" + matchValue);

                Match attackerNameMatch = Regex.Match(attackerMatch.Value, "<div class=\"nameholder\">(.*?)</div>", RegexOptions.Singleline);
                Match attackerAllyMatch = Regex.Match(attackerMatch.Value, "(<ul class=\"allies .*?\">(.*?)</ul>)", RegexOptions.Singleline);
                Match defenderNameMatch = Regex.Match(defenderMatch.Value, "<div class=\"nameholder\">(.*?)</div>", RegexOptions.Singleline);
                Match defenderAllyMatch = Regex.Match(defenderMatch.Value, "(<ul class=\"allies .*?\">(.*?)</ul>)", RegexOptions.Singleline);

                if (!attackerNameMatch.Success)
                    throw new Exception("Unable to determine attacking country's name. Has the schema changed?\nParsing out of:\n" + attackerMatch.Value);
                if (!defenderNameMatch.Success)
                    throw new Exception("Unable to determine defending country's name. Has the schema changed?\nParsing out of:\n" + defenderMatch.Value);

                // Turn our results into XmlNode objects for easy value retrieval
                XmlNode middleNode = this.GetXmlNodeFromString(middleMatch.Value);
                XmlNode attackerNameNode = this.GetXmlNodeFromString(attackerNameMatch.Value);
                XmlNode defenderNameNode = this.GetXmlNodeFromString(defenderMatch.Value.Replace("<div class=\"attacker\">", string.Empty));

                // Find the number of active battles
                int numOfActiveBattles = 0;
                string activeBattlesString = middleNode.ChildNodes[1].InnerText;

                if (!activeBattlesString.Equals("no active battles"))
                {
                    activeBattlesString = activeBattlesString.Replace(" active battles", string.Empty);
                    numOfActiveBattles = int.Parse(activeBattlesString);
                }

                // Find the WarID
                int warId = 0;
                string warDetailsUrl = middleNode.FirstChild.Attributes["href"].Value;
                string warIdString = warDetailsUrl;
                warIdString = warIdString.Replace("/en/wars/show/", string.Empty);
                warId = int.Parse(warIdString);

                // Find the attacking country's name
                string attackerName = attackerNameNode.FirstChild.InnerText;

                // Find all attacking allied countries
                // Example <li>:
                // <li><a href="/en/country/Hungary"><div class="flagholder"><img title="Hungary" alt="Hungary" src="/images/flags/M/Hungary.gif" /></div>Hungary</a></li>
                List<string> attackerCountryNames = new List<string>();
                if (attackerAllyMatch.Success)
                {
                    XmlNode attackerAlliesNode = this.GetXmlNodeFromString(attackerAllyMatch.Value);
                    foreach (XmlNode li in attackerAlliesNode.FirstChild.ChildNodes)
                    {
                        string countryName = li.FirstChild.FirstChild.FirstChild.Attributes["title"].Value;

                        if (!string.IsNullOrEmpty(countryName))
                            attackerCountryNames.Add(countryName);
                    }
                }

                // Find all defending countries
                List<string> defenderCountryNames = new List<string>();
                if (defenderAllyMatch.Success)
                {
                    XmlNode defenderAlliesNode = this.GetXmlNodeFromString(defenderAllyMatch.Value.Replace("<div class=\"attacker\">", string.Empty));
                    foreach (XmlNode li in defenderAlliesNode.FirstChild.ChildNodes)
                    {
                        string countryName = li.FirstChild.FirstChild.FirstChild.Attributes["title"].Value;

                        if (!string.IsNullOrEmpty(countryName))
                            defenderCountryNames.Add(countryName);
                    }
                }

                War war = new War(warId);

                foreach (string countryName in attackerCountryNames)
                {
                    Country country = this.m_countries.Find(delegate(Country c) { return c.Name.Equals(countryName); });
                    war.Attackers.Add(country);
                }

                foreach (string countryName in defenderCountryNames)
                {
                    Country country = this.m_countries.Find(delegate(Country c) { return c.Name.Equals(countryName); });
                    war.Defenders.Add(country);
                }

                // If there is an active battle, go to war page and load battle details
                if (numOfActiveBattles > 0)
                {
                    string warDetailsFullUrl = "http://www.erepublik.com/" + warDetailsUrl;
                    string warDetailsPageResponseText = string.Empty;
                    try
                    {
                        HttpWebRequest request = WebRequestManager.CreateCustomPostRequest(warDetailsFullUrl, postData, this.m_cookieContainer);
                        warDetailsPageResponseText = WebRequestManager.SendCustomWebRequest(request);
                    }
                    catch (Exception ex)
                    {
                        return false;
                    }

                    MatchCollection activeBattlesMatches = Regex.Matches(warDetailsPageResponseText, "(<div class=\"holder largepadded\">(.*?)</p>.*?</div>.*?</div>.*?</div>)", RegexOptions.Singleline);

                    if (!attackerNameMatch.Success)
                        throw new Exception("Unable to determine attacking country's name. Has the schema changed?\nParsing out of:\n" + attackerMatch.Value);

                    Match noActiveBattlesMsg = Regex.Match(warDetailsPageResponseText, "(<p class=\"regular\">There are no active battles in this war</p>)", RegexOptions.Singleline);

                    // Move to next war if there are no active battles
                    if (noActiveBattlesMsg.Success)
                        continue;

                    foreach (Match battleMatch in activeBattlesMatches)
                    {
                        XmlNode battleMatchNode = this.GetXmlNodeFromString(this.ScrubXMLString(battleMatch.Value));

                        string regionName = battleMatchNode.FirstChild.FirstChild.FirstChild.InnerText;
                        string defensePointsString = battleMatchNode.ChildNodes[2].FirstChild.ChildNodes[1].InnerText;
                        int defensePoints = int.Parse(defensePointsString);

                        Battle battle = new Battle(warId, regionName, defensePoints);
                        war.ActiveBattles.Add(battle);
                    }
                }

                warList.Add(war);
            }

            string healUrl = "http://www.republik.com/en/hospital/Muntenia";
            string healPostData = string.Format("_token={0}&region={1}", this.m_authToken, "Muntenia");

            return false;
        }

        private bool PerformAdvisorAction_FindHigherPayingJob()
        {
            return false;
        }
        #endregion

        private string ScrubXMLString(string xml)
        {
            return xml.Replace("\n", "").Replace("\t", "");
        }

        private XmlNode GetXmlNodeFromString(string xml)
        {
            if (!string.IsNullOrEmpty(xml))
            {
                XmlReader reader = XmlReader.Create(new StringReader(xml));
                XmlDocument doc = new XmlDocument();
                XmlNode node = doc.ReadNode(reader);

                return node;
            }

            return null;
        }

        private void FetchAuthToken()
        {
            Console.Write("Fetching Auth Token... ");
            this.m_authToken = GetAuthToken();
            Console.WriteLine("Done.");
        }

        private void Login()
        {
            Console.Write("Logging In... ");
            HttpWebRequest loginRequest = CreateLoginRequest();
            string responseText = WebRequestManager.SendCustomWebRequest(loginRequest);
            Console.WriteLine("Done.");
        }

        private void PopulateCountries()
        {
            Console.Write("Populating Countries... ");
            this.m_countries = new List<Country>();

            string responseText = WebRequestManager.SendGenericWebRequest("http://www.erepublik.com/en/rankings/citizens", this.m_cookieContainer);

            string title = string.Empty;
            string id = string.Empty;

            // Find all matches in web response.
            MatchCollection m1 = Regex.Matches(responseText, @"(<a class=\""spaced_small\"" .*>)", RegexOptions.Multiline);

            // Loop over each match.
            foreach (Match m in m1)
            {
                string matchValue = m.Groups[1].Value;
                Match titleMatch = Regex.Match(matchValue, @"title=\""(.*?)\""", RegexOptions.Singleline);
                Match idMatch = Regex.Match(matchValue, @"href=\""/en/rankings/citizens/country/1/(.*?)\"">", RegexOptions.Singleline);

                if (titleMatch.Success)
                    title = titleMatch.Groups[1].Value;

                if (idMatch.Success)
                    id = idMatch.Groups[1].Value;

                if (titleMatch.Success && idMatch.Success)
                    this.m_countries.Add(new Country(title, int.Parse(id)));
            }
            Console.WriteLine("Done");

            Console.Write("Finding our country... ");
            
            //Match countryMatch = Regex.Match(responseText, @"<img class=\""flag\"" alt=\"".*?\"" title=\"".*?\"" src=\""/images/flags/.*?/(.*?).gif\"" />", RegexOptions.Singleline);
            Match countryMatch = Regex.Match(responseText, @"<a href=""/en/country/(.*?)"">", RegexOptions.Singleline);
            if (countryMatch.Success)
            {
                string countryName = countryMatch.Groups[1].Value;
                this.m_country = this.m_countries.Find(delegate(Country c) { return c.Name.Equals(countryName); });
                Console.WriteLine("Done");
            }
            else
            {
                Console.WriteLine("Failed");
            }

        }

        private void PopulateProfile()
        {
            //Console.Write("Populating Profile... ");

            //Console.WriteLine("Done");
        }

        private void PerformActions()
        {
            // Always attempt to do these actions - the advisor has broken caching and I don't handle all it's messages
            PerformAction(Actions.Work);
            PerformAction(Actions.Train);
            //PerformAdvisorAction(AdvisorActions.BuyFood);
            //PerformAdvisorAction(AdvisorActions.Fight);
        }

        private enum Actions
        {
            Work = 0,
            Train,
            Fight,
            BuyFood,
            FindHigherPayingJob,
            NoAction
        }

        private UserDetails m_userDetails;
        private string m_authToken;
        private string m_advisorFullText;
        private List<Country> m_countries;
        private Country m_country;
        private CookieContainer m_cookieContainer;
        private Profile m_profile;
        private List<Actions> m_performedActions;

        private const string PERFORM_ACTION_FORMAT = "{0}... ";
        private const int FOOD_INDUSTRY_ID = 1;
        private const string NO_ACTIONS_LEFT_ADVICE_STRING = "Now you can visit the forum or read the  news to stay in touch with what happens on eRepublik.";
        private const string ACTION_WORK_ADVICE_STRING = "You should work todayIt will help you increase both your skill and savings.";
        private const string ACTION_TRAIN_ADVICE_STRING = "You should train todayYour strength can make you a hero on the battlefield.";
        private const string ACTION_FIGHT_ADVICE_STRING = "3";
        private const string ACTION_BUYFOOD_ADVICE_STRING = "Buy food!No food means starvation. Hurry up and buy some.";
    }
}
