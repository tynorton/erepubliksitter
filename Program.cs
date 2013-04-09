using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using eRepublikSitter.Market;

namespace eRepublikSitter
{
    class Program
    {
        

        private static void LoadUsersFromFile()
        {
            s_userDetails = new List<UserDetails>();
            s_usersConfigDoc = new System.Xml.XmlDocument();
            s_usersConfigDoc.Load("Users.xml");

            XmlNodeList userNodes = s_usersConfigDoc.SelectNodes("/Users/User");
            foreach (XmlNode node in userNodes)
            {
                UserDetails credential = new UserDetails();

                foreach (XmlNode attributeNode in node.ChildNodes)
                {
                    if (attributeNode.Name == "CitizenName" && !string.IsNullOrEmpty(attributeNode.InnerText))
                        credential.CitizenName = attributeNode.InnerText;

                    if (attributeNode.Name == "Password" && !string.IsNullOrEmpty(attributeNode.InnerText))
                        credential.Password = attributeNode.InnerText;
                }

                if (credential.IsPopulated())
                    s_userDetails.Add(credential);
            }
        }

        static void Main(string[] args)
        {
            LoadUsersFromFile();

            foreach (UserDetails detail in s_userDetails)
            {
                UserActionProcessor actionProcessor = new UserActionProcessor(detail);
                actionProcessor.Process();
            }
        }

        
        private static XmlDocument s_usersConfigDoc;
        private static List<UserDetails> s_userDetails;
    }
}
