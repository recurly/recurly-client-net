﻿using System;
using System.Net;
using System.Xml;

namespace Recurly
{
    /// <summary>
    /// An account in Recurly.
    /// 
    /// http://docs.recurly.com/api/accounts
    /// </summary>
    public class Account
    {

        // The currently valid account states
        // Corrected to allow multiple states, per http://docs.recurly.com/api/accounts
        [Flags]
        public enum AccountState : short
        {
            Closed = 1,
            Active = 2,
            PastDue = 4
        }


        /// <summary>
        /// Account Code or unique ID for the account in Recurly
        /// </summary>
        public string AccountCode { get; private set; }
        public AccountState State { get; private set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CompanyName { get; set; }
        public string AcceptLanguage { get; set; }
        public string HostedLoginToken { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private BillingInfo _billingInfo;

        public BillingInfo BillingInfo
        {
            get
            {
                if (null != _billingInfo)
                    return _billingInfo;

                try
                {
                    _billingInfo = BillingInfo.Get(AccountCode);
                }
                catch (NotFoundException)
                {
                    _billingInfo = null;
                }

                return _billingInfo;
            }
            set
            {
                _billingInfo = value;
            }
        }

        internal const string UrlPrefix = "/accounts/";

        public Account(string accountCode)
        {
            AccountCode = accountCode;            
        }


        /// <summary>
        /// Creates a new account with required billing information
        /// </summary>
        /// <param name="accountCode"></param>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="creditCardNumber"></param>
        /// <param name="expirationMonth"></param>
        /// <param name="expirationYear"></param>
        public Account(string accountCode, string firstName, string lastName, string creditCardNumber, int expirationMonth, int expirationYear)
        {
            AccountCode = accountCode;
            _billingInfo = new BillingInfo(accountCode)
            {
                FirstName = firstName,
                LastName = lastName,
                CreditCardNumber = creditCardNumber,
                ExpirationMonth = expirationMonth,
                ExpirationYear = expirationYear
            };
        }

        internal Account(XmlTextReader xmlReader)
        {
            ReadXml(xmlReader);
        }

        private Account()
        { }

        /// <summary>
        /// Lookup a Recurly account
        /// </summary>
        /// <param name="accountCode"></param>
        /// <returns></returns>
        public static Account Get(string accountCode)
        {
            var account = new Account();

            var statusCode = Client.PerformRequest(Client.HttpRequestMethod.Get,
                UrlPrefix + Uri.EscapeUriString(accountCode),
                account.ReadXml);

            return statusCode == HttpStatusCode.NotFound ? null : account;
        }


        /// <summary>
        /// Delete an account's billing info.
        /// </summary>
        public void ClearBillingInfo()
        {
            Client.PerformRequest(Client.HttpRequestMethod.Delete,
                UrlPrefix + Uri.EscapeUriString(AccountCode) + "/billing_info");
            _billingInfo = null;
        }

        
        
        /// <summary>
        /// Create a new account in Recurly
        /// </summary>
        public void Create()
        {
            Client.PerformRequest(Client.HttpRequestMethod.Post, UrlPrefix, WriteXml, ReadXml);
        }

        /// <summary>
        /// Update an existing account in Recurly
        /// </summary>
        public void Update()
        {
            Client.PerformRequest(Client.HttpRequestMethod.Put,
                UrlPrefix + Uri.EscapeUriString(AccountCode),
                WriteXml);
        }

        /// <summary>
        /// Close the account and cancel any active subscriptions (if there is one).
        /// Note: This does not create a refund for any time remaining.
        /// </summary>
        public void Close()
        {
            Close(AccountCode);
            // TODO clear Open from the enum, add Closed
            State = AccountState.Closed;

        }

        /// <summary>
        /// Close the account and cancel any active subscriptions (if there is one).
        /// Note: This does not create a refund for any time remaining.
        /// </summary>
        /// <param name="accountCode">Account Code</param>
        public static void Close(string accountCode)
        {
            Client.PerformRequest(Client.HttpRequestMethod.Delete,
                UrlPrefix + Uri.EscapeUriString(accountCode));
        }

        /// <summary>
        /// Reopen an existing account in Recurly
        /// </summary>
        public void Reopen()
        {
            Reopen(AccountCode);
            // TODO Clear Closed, add Active
            State = AccountState.Active;
        }

        /// <summary>
        /// Reopen an existing account in recurly.
        /// </summary>
        /// <param name="accountCode">Account Code</param>
        public static void Reopen(string accountCode)
        {
            Client.PerformRequest(Client.HttpRequestMethod.Put,
                UrlPrefix + Uri.EscapeUriString(accountCode) + "/reopen");
        }


        // This method appears to not conform to the API given http://docs.recurly.com/api/accounts
        // TODO confirm if usage is correct
        /// <summary>
        /// Posts pending charges on an account
        /// </summary>
        public Invoice InvoicePendingCharges()
        {
            var i = new Invoice();
            Client.PerformRequest(Client.HttpRequestMethod.Post,
                UrlPrefix + Uri.EscapeUriString(AccountCode) + "/invoices",
                i.ReadXml);

            return i;
        }


        /// <summary>
        /// Gets all adjustments for this account, by type
        /// </summary>
        /// <param name="type">Adjustment type to retrieve. Optional, default: All.</param>
        /// <param name="state">State of the Adjustments to retrieve. Optional, default: Any.</param>
        /// <returns></returns>
        public AdjustmentList GetAdjustments(Adjustment.AdjustmentType type = Adjustment.AdjustmentType.all,
            Adjustment.AdjustmentState state = Adjustment.AdjustmentState.any)
        {
            var adjustments = new AdjustmentList();
            var statusCode = Client.PerformRequest(Client.HttpRequestMethod.Get,
                UrlPrefix + Uri.EscapeUriString(AccountCode) + "/adjustments/?"
                + (Adjustment.AdjustmentState.any == state ? "" : "state=" + state)
                + (Adjustment.AdjustmentType.all == type ? "" : "&type=" + type)
                , adjustments.ReadXmlList);

            return statusCode == HttpStatusCode.NotFound ? null : adjustments;
        }

        
        /// <summary>
        /// Returns a list of invoices for this account
        /// </summary>
        /// <returns></returns>
        public InvoiceList GetInvoices()
        {
            return InvoiceList.GetInvoices(AccountCode);
        }

       
        /// <summary>
        /// Returns a list of subscriptions for this account
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public SubscriptionList GetSubscriptions(Subscription.SubstriptionState state = Subscription.SubstriptionState.all)
        {
            return new SubscriptionList(UrlPrefix + Uri.EscapeUriString(AccountCode) + "/subscriptions/"
                + (state.Equals(Subscription.SubstriptionState.all) ? "" :  "?state=" + state));
        }


        /// <summary>
        /// Returns a list of transactions for this account, by transaction type
        /// </summary>
        /// <param name="state">Transactions of this state will be retrieved. Optional, default: All.</param>
        /// <param name="type">Transactions of this type will be retrieved. Optional, default: All.</param>
        /// <returns></returns>
        public TransactionList GetTransactions(TransactionList.TransactionState state = TransactionList.TransactionState.all,
            TransactionList.TransactionType type = TransactionList.TransactionType.all)
        {
            return new TransactionList(UrlPrefix + Uri.EscapeUriString(AccountCode) + "/transactions/?"
                + (state != TransactionList.TransactionState.all ? "state=" + state : "")
                + (type != TransactionList.TransactionType.all ? "&type=" + type : ""));
        }

        /// <summary>
        /// Returns a new adjustment (credit or charge) for this account
        /// </summary>
        /// <param name="description"></param>
        /// <param name="unitAmountInCents"></param>
        /// <param name="currency"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        public Adjustment CreateAdjustment(string description, int unitAmountInCents, string currency, int quantity=1)
        {
            return new Adjustment(AccountCode, description, currency, unitAmountInCents, quantity);
        }

        /// <summary>
        /// Redeems a coupon on this account
        /// </summary>
        /// <param name="couponCode"></param>
        /// <param name="currency"></param>
        /// <returns></returns>
        public CouponRedemption RedeemCoupon(string couponCode, string currency)
        {
            return CouponRedemption.Redeem(AccountCode, couponCode, currency);
        }


        /// <summary>
        /// Returns the active coupon redemption on this account
        /// </summary>
        /// <returns></returns>
        public CouponRedemption GetActiveCoupon()
        {
            var cr = new CouponRedemption();
            
            var statusCode = Client.PerformRequest(Client.HttpRequestMethod.Get,
                UrlPrefix + Uri.EscapeUriString(AccountCode) + "/redemption",
                cr.ReadXml);

            return statusCode == HttpStatusCode.NotFound ? null : cr;
        }

        #region Read and Write XML documents

        internal void ReadXml(XmlTextReader reader)
        {
            while (reader.Read())
            {
                if (reader.Name == "account" && reader.NodeType == XmlNodeType.EndElement)
                    break;

                if (reader.NodeType != XmlNodeType.Element) continue;

                switch (reader.Name)
                {
                    case "account_code":
                        AccountCode = reader.ReadElementContentAsString();
                        break;

                    case "state":
                        // TODO investigate in case of incoming data representing multiple states, as http://docs.recurly.com/api/accounts says is possible
                        State = (AccountState)Enum.Parse(typeof(AccountState), reader.ReadElementContentAsString(), true);
                        break;

                    case "username":
                        Username = reader.ReadElementContentAsString();
                        break;

                    case "email":
                        Email = reader.ReadElementContentAsString();
                        break;

                    case "first_name":
                        FirstName = reader.ReadElementContentAsString();
                        break;

                    case "last_name":
                        LastName = reader.ReadElementContentAsString();
                        break;

                    case "company_name":
                        CompanyName = reader.ReadElementContentAsString();
                        break;

                    case "accept_language":
                        AcceptLanguage = reader.ReadElementContentAsString();
                        break;

                    case "hosted_login_token":
                        HostedLoginToken = reader.ReadElementContentAsString();
                        break;

                    case "created_at":
                        CreatedAt = reader.ReadElementContentAsDateTime();
                        break;
                }
            }
        }

        internal void WriteXml(XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("account"); // Start: account

            xmlWriter.WriteElementString("account_code", AccountCode);

            // TODO flatten these
            if (!string.IsNullOrEmpty(Username))
                xmlWriter.WriteElementString("username", Username);
            if (!string.IsNullOrEmpty(Email))
                xmlWriter.WriteElementString("email", Email);
            if (!string.IsNullOrEmpty(FirstName))
                xmlWriter.WriteElementString("first_name", FirstName);
            if (!string.IsNullOrEmpty(LastName))
                xmlWriter.WriteElementString("last_name", LastName);
            if (!string.IsNullOrEmpty(CompanyName))
                xmlWriter.WriteElementString("company_name", CompanyName);
            if (!string.IsNullOrEmpty(AcceptLanguage))
                xmlWriter.WriteElementString("accept_language", AcceptLanguage);

            if (_billingInfo != null)
                _billingInfo.WriteXml(xmlWriter);

            xmlWriter.WriteEndElement(); // End: account
        }

        #endregion

        #region Object Overrides

        public override string ToString()
        {
            return "Recurly Account: " + AccountCode;
        }

        public override bool Equals(object obj)
        {
            var a = obj as Account;
            return a != null && Equals(a);
        }

        public bool Equals(Account account)
        {
            return account != null && AccountCode == account.AccountCode;
        }

        public override int GetHashCode()
        {
            return AccountCode.GetHashCode();
        }

        #endregion
    }
}
