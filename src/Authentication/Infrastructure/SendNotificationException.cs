using System;
using System.Runtime.Serialization;

namespace Authentication.Infrastructure
{
    public class SendNotificationException : Exception
    {
        public SendNotificationException(string message) : base(message)
        {

        }
    }
}