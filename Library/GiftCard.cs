using System;
using System.Net;
using System.Xml;

namespace Recurly
{
    /// <summary>
    /// An gift card in Recurly.
    ///
    /// https://dev.recurly.com/docs/gift-card-object
    /// </summary>
    public class GiftCard : RecurlyEntity, IGiftCard
    {
        /// <summary>
        /// Unique ID assigned to this gift card.
        /// </summary>
        public long Id { get; private set; }
        
        private string _accountCode;
        private IAccount _account;

        /// <summary>
        /// Account details for the gifter.
        /// This can reference an existing account_code or create
        /// a new account using the Account objects params.
        /// An account_code is required. If this object only has a
        /// link to the account, it will fetch and cache it.
        /// </summary>
        public IAccount GifterAccount
        {
            get { return _account ?? (_account = Accounts.Get(_accountCode)); }
            set { _account = value; }
        }

        /// <summary>
        /// The product code of the gift card product.
        /// </summary>
        public string ProductCode { get; set; }

        /// <summary>
        /// The amount of the gift card.
        /// Must match an amount on the gift card product.
        /// </summary>
        public int UnitAmountInCents { get; set; }

        /// <summary>
        /// The currency of the unit_amount_in_cents.
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// The unique redemption code for the gift card,
        /// generated by Recurly.Will be 16 characters, alphanumeric,
        /// displayed uppercase, but accepted in any case at redemption.
        /// Used by the recipient_account to create a credit in the amount
        /// of the unit_amount_in_cents on their account.
        /// </summary>
        public string RedemptionCode { get; private set; }

        /// <summary>
        /// The remaining credit on the recipient_account associated
        /// with this gift card.Only shows once the gift card
        /// has been redeemed. Can be used to create gift card balance
        /// displays for your customers.
        /// </summary>
        public int? BalanceInCents { get; set; }

        /// <summary>
        /// Block of delivery information.
        /// </summary>
        public IDelivery Delivery { get; set; }

        /// <summary>
        /// When the gift card was purchased.
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// When the gift card object was updated,
        /// which happens upon purchase, delivery and redemption.
        /// </summary>
        public DateTime UpdatedAt { get; private set; }

        /// <summary>
        /// When the gift card was redeemed by the recipient.
        /// </summary>
        public DateTime? RedeemedAt { get; private set; }

        /// <summary>
        /// When the gift card was canceled.
        /// </summary>
        public DateTime? CanceledAt { get; private set; }

        /// <summary>
        /// When the gift card was sent to the recipient by Recurly via email,
        /// if method was email and the "Gift Card Delivery" email template was enabled.
        /// This will be empty for post delivery or email delivery 
        /// where the email template was disabled.
        /// </summary>
        public DateTime? DeliveredAt { get; private set; }

        
        private String _purchaseInvoiceId;
        private IInvoice _purchaseInvoice;

        private String _redemptionInvoiceId;
        private IInvoice _redemptionInvoice;


        /// <summary>
        /// The charge invoice for the gift card purchase.
        /// </summary>
        public IInvoice PurchaseInvoice
        {
            get
            {
                if (_purchaseInvoice == null && !_purchaseInvoiceId.IsNullOrEmpty())
                {
                    _purchaseInvoice = Invoices.Get(_purchaseInvoiceId);
                }
                return _purchaseInvoice;
            }
            set { _purchaseInvoice = value; }
        }

        /// <summary>
        /// The credit invoice for the gift card redemption.
        /// </summary>
        public IInvoice RedemptionInvoice
        {
            get
            {
                if (_redemptionInvoice == null && !_redemptionInvoiceId.IsNullOrEmpty())
                {
                    _redemptionInvoice = Invoices.Get(_redemptionInvoiceId);
                }
                return _redemptionInvoice;
            }
            set { _redemptionInvoice = value; }
        }

        internal const string UrlPrefix = "/gift_cards/";

        public GiftCard(string accountCode, IDelivery delivery, string productCode, int unitAmountInCents, string currency)
        {
            GifterAccount = new Account(accountCode);
            ProductCode = productCode;
            UnitAmountInCents = unitAmountInCents;
            Currency = currency;
            Delivery = delivery;
        }

        public GiftCard(IAccount gifterAccount, IDelivery delivery, string productCode, int unitAmountInCents, string currency)
        {
            GifterAccount = gifterAccount;
            ProductCode = productCode;
            UnitAmountInCents = unitAmountInCents;
            Currency = currency;
            Delivery = delivery;
        }

        public GiftCard(string redemptionCode)
        {
            RedemptionCode = redemptionCode;
        }

        internal GiftCard() {}

        internal GiftCard(XmlTextReader xmlReader)
        {
            ReadXml(xmlReader);
        }

        /// <summary>
        /// Create a new gift card in Recurly.
        /// </summary>
        public void Create()
        {
            Client.Instance.PerformRequest(Client.HttpRequestMethod.Post,
                UrlPrefix, WriteXml, ReadXml);

            BustAttributeCache();
        }

        /// <summary>
        /// Preview a new gift card in Recurly.
        /// Runs validations and allows the gifter
        /// to confirm that the delivery details provided are correct.
        /// Does not run transactions.
        /// </summary>
        public void Preview()
        {
            Client.Instance.PerformRequest(Client.HttpRequestMethod.Post,
                UrlPrefix + "preview", WriteXml, ReadXml);
        }

        /// <summary>
        /// Redeem this gift card on the account
        /// with the given account code.
        /// </summary>
        /// <param name="accountCode">The account code to redeem the card against</param>
        public void Redeem(string accountCode)
        {
            var account = new Account(accountCode);
            Client.Instance.PerformRequest(Client.HttpRequestMethod.Post,
                UrlPrefix + "/" + RedemptionCode + "/redeem", account.WriteGiftCardRedeemXml, ReadXml);
        }

        /// <summary>
        /// Nulls any cached attributes so we fetch fresh ones
        /// from the server
        /// </summary>
        private void BustAttributeCache()
        {
            _account = null;
        }

        #region Read and Write XML documents

        internal override void ReadXml(XmlTextReader reader)
        {
            while (reader.Read())
            {
                if (reader.Name == "gift_card" && reader.NodeType == XmlNodeType.EndElement)
                    break;

                if (reader.NodeType != XmlNodeType.Element) continue;

                DateTime dateVal;

                switch (reader.Name)
                {
                    case "id":
                        long id;
                        if (long.TryParse(reader.ReadElementContentAsString(), out id))
                            Id = id;
                        break;
                        
                    case "product_code":
                        ProductCode = reader.ReadElementContentAsString();
                        break;

                    case "currency":
                        Currency = reader.ReadElementContentAsString();
                        break;

                    case "redemption_code":
                        RedemptionCode = reader.ReadElementContentAsString();
                        break;

                    case "unit_amount_in_cents":
                        int amount;
                        if (Int32.TryParse(reader.ReadElementContentAsString(), out amount))
                            UnitAmountInCents = amount;
                        break;

                    case "balance_in_cents":
                        int balance;
                        if (Int32.TryParse(reader.ReadElementContentAsString(), out balance))
                            BalanceInCents = balance;
                        break;

                    case "redemption_invoice":
                        string redemptionUrl = reader.GetAttribute("href");
                        if (redemptionUrl != null)
                        {
                            _redemptionInvoiceId = Uri.UnescapeDataString(redemptionUrl.Substring(redemptionUrl.LastIndexOf("/") + 1));
                        }
                        break;

                    case "purchase_invoice":
                        string purchaseUrl = reader.GetAttribute("href");
                        if (purchaseUrl != null)
                        {
                            _purchaseInvoiceId = Uri.UnescapeDataString(purchaseUrl.Substring(purchaseUrl.LastIndexOf("/") + 1));
                        }
                        break;

                    case "gifter_account":
                        string href = reader.GetAttribute("href");
                        if (null != href)
                        {
                            _accountCode = Uri.UnescapeDataString(href.Substring(href.LastIndexOf("/") + 1));
                        }
                        else
                        {
                            GifterAccount = new Account(reader, "gifter_account");
                        }
                        break;

                    case "delivery":
                        Delivery = new Delivery(reader);
                        break;

                    case "created_at":
                        if (DateTime.TryParse(reader.ReadElementContentAsString(), out dateVal))
                            CreatedAt = dateVal;
                        break;

                    case "updated_at":
                        if (DateTime.TryParse(reader.ReadElementContentAsString(), out dateVal))
                            UpdatedAt = dateVal;
                        break;

                    case "redeemed_at":
                        if (DateTime.TryParse(reader.ReadElementContentAsString(), out dateVal))
                            RedeemedAt = dateVal;
                        break;

                    case "canceled_at":
                        if (DateTime.TryParse(reader.ReadElementContentAsString(), out dateVal))
                            CanceledAt = dateVal;
                        break;

                    case "delivered_at":
                        if (DateTime.TryParse(reader.ReadElementContentAsString(), out dateVal))
                            DeliveredAt = dateVal;
                        break;
                }
            }
        }

        internal override void WriteXml(XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("gift_card"); // Start: gift_card

            xmlWriter.WriteElementString("product_code", ProductCode);
            xmlWriter.WriteElementString("currency", Currency);
            xmlWriter.WriteElementString("unit_amount_in_cents", UnitAmountInCents.ToString());

            if (GifterAccount != null)
                Account.WriteXml(xmlWriter, GifterAccount, "gifter_account");

            var recurlyDelivery = Delivery as Delivery;

            if (recurlyDelivery != null)
                recurlyDelivery.WriteXml(xmlWriter);

            xmlWriter.WriteEndElement(); // End: gift_card
        }

        /// <summary>
        /// Redemption serializer
        /// </summary>
        /// <param name="xmlWriter"></param>
        internal void WriteRedemptionXml(XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("gift_card"); // Start: gift_card
            xmlWriter.WriteElementString("redemption_code", RedemptionCode);
            xmlWriter.WriteEndElement(); // End: gift_card
        }

        #endregion

        #region Object Overrides

        public override string ToString()
        {
            return "Recurly GiftCard: " + Id;
        }

        public override bool Equals(object obj)
        {
            var a = obj as IGiftCard;
            return a != null && Equals(a);
        }

        public bool Equals(IGiftCard giftCard)
        {
            return giftCard != null && Id == giftCard.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion
    }

    public sealed class GiftCards
    {
        internal const string UrlPrefix = "/gift_cards/";

        /// <summary>
        /// Lookup a Recurly gift card
        /// </summary>
        /// <param name="id">The long id of the gift card</param>
        /// <returns></returns>
        public static IGiftCard Get(long id)
        {
            var giftCard = new GiftCard();
            // GET /gift_cards/<id>
            var statusCode = Client.Instance.PerformRequest(Client.HttpRequestMethod.Get,
                UrlPrefix + Uri.EscapeDataString(id.ToString()),
                giftCard.ReadXml);

            return statusCode == HttpStatusCode.NotFound ? null : giftCard;
        }

        /// <summary>
        /// Lists gift cards
        /// </summary>
        /// <param name="gifterAccountCode">A gifter's account code to filter by (may be null)</param>
        /// <param name="recipientAccountCode">A recipients's account code to filter by (may be null)</param>
        /// <param name="filter">FilterCriteria used to apply server side sorting and filtering</param>
        /// <returns></returns>
        public static IRecurlyList<IGiftCard> List(string gifterAccountCode = null, string recipientAccountCode = null, FilterCriteria filter = null)
        {
            filter = filter ?? FilterCriteria.Instance;
            var parameters = filter.ToNamedValueCollection();

            if (gifterAccountCode != null)
                parameters["gifter_account_code"] = gifterAccountCode;
            if (recipientAccountCode != null)
                parameters["recipient_account_code"] = recipientAccountCode;

            return new GiftCardList(GiftCard.UrlPrefix + "?" + parameters.ToString());
        }
    }
}
