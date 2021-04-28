using System;
using System.Collections.Generic;

namespace BeyondAuth.Web.Models.Account
{
    public class ApplicationUser
    {
        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Phone number
        /// </summary>
        public string PhoneNumber { get; set; }
         
        /// <summary>
        /// User's first name
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// User's last name
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Date created
        /// </summary>
        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date last logged in
        /// </summary>
        public DateTime? LastLoggedIn { get; set; }

        /// <summary>
        /// User's street
        /// </summary>
        public string Street { get; set; }

        /// <summary>
        /// City
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// State (if in US)
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Postal Code
        /// </summary>
        public string PostalCode { get; set; }

        /// <summary>
        /// Country
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// If the user is deleted
        /// </summary>
        public bool Deleted { get; set; } = false;

        /// <summary>
        /// List with Subscriptions
        /// </summary>
        public List<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();

        public List<CreditInfo> Credits { get; set; } = new List<CreditInfo>();

        /// <summary>
        /// User transactions
        /// </summary>
        public List<ProcessedTransaction> Transactions { get; set; } = new List<ProcessedTransaction>();

        /// <summary>
        /// Special requests from users
        /// </summary>
        public List<UserRequest> UserRequests { get; set; } = new List<UserRequest>();

        /// <summary>
        /// Account was deactivated by an admin
        /// </summary>
        public bool Deactivated { get; set; }

        /// <summary>
        /// Account notes
        /// </summary>
        public string Notes { get; set; }
    }

    public class CreditInfo
    {
        public string SubscriptionId { get; set; }

        public DateTime? ExpiresUtc { get; set; }

        public int Remaining { get; set; }

        public CreditType Type { get; set; }
    }

    public enum CreditType
    {
        EmailSupport,
        ChatSupport,
        PhoneSupport,
        ExternalAuthProviders
    }

    public class UserRequest
    {
        /// <summary>
        /// Date request was submitted
        /// </summary>
        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Request type: Subscription Price Adjustment, Cancel Subscription
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// User-specified reason for the request
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Status of the request
        /// </summary>
        public RequestStatuses Status { get; set; }

        /// <summary>
        /// Date the request was processed
        /// </summary>
        public DateTime? DateProcessed { get; set; }
    }

    public enum RequestStatuses
    {
        Pending,
        Approved,
        Denied
    }

    public class UserSubscription
    {
        /// <summary>
        /// UserSubscription Id
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Referral
        /// </summary>
        public string Referral { get; set; }

        /// <summary>
        /// Coupon Code
        /// </summary>
        public string CouponCode { get; set; }

        /// <summary>
        /// SubscriptionId
        /// Pattern: Subscriptions/{string}
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Subscription price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Date created
        /// </summary>
        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date when user's subscription will expire
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// Date when user canceled the subscription
        /// </summary>
        public DateTime? CancelDate { get; set; }

        /// <summary>
        /// Subscription name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Payment processor subscription id
        /// </summary>
        public string RefId { get; set; }

        public SubscriptionStatus Status { get; set; }

        /// <summary>
        /// Card Type used for subscription
        /// </summary>
        public string CardType { get; set; }

        /// <summary>
        /// Masked credit card number used
        /// </summary>
        public string CardNumber { get; set; }

        /// <summary>
        /// Credit card expiration date
        /// </summary>
        public string CardExpirationDate { get; set; }

        /// <summary>
        /// Recurrence. Ex: Monthly or 7 Days
        /// </summary>
        public string Recurrence { get; set; }
    }

    public enum SubscriptionStatus
    {
        active = 0,
        expired = 1,
        suspended = 2,
        canceled = 3,
        terminated = 4,
        na = 5,
        unchanged = 10
    }

    public class ProcessedTransaction
    {
        /// <summary>
        /// Transaction Date
        /// </summary>
        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Name of product
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Payment processor message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Transaction status
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Transaction amount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Payment processor transaction id
        /// </summary>
        public string RefId { get; set; }

        /// <summary>
        /// Optional Payment processor subscription id
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Card provider
        /// </summary>
        public string CardType { get; set; }

        /// <summary>
        /// Masked card number
        /// </summary>
        public string CardNumber { get; set; }
    }
}
