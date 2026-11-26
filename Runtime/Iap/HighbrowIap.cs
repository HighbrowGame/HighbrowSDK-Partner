using System;
using Highbrow.Log;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Highbrow.Iap
{
    /// <summary>
    /// Sends purchase logs using price and currency from the same Unity IAP product.
    /// </summary>
    public static class HighbrowIap
    {
#if HIGHBROW_UNITY_IAP_V4
        /// <summary>
        /// Sends a validated Unity IAP 4 purchase using the product's receipt and store metadata.
        /// </summary>
        public static void TrackPurchase(Product product, int productId = 0, DateTime? purchaseTime = null, string suid = null)
        {
            TrackPurchase(product, product?.receipt, productId, purchaseTime, suid);
        }
#endif

        /// <summary>
        /// Sends a validated purchase using its receipt or store order ID and the product's store metadata.
        /// </summary>
        public static void TrackPurchase(Product product, string receiptId, int productId = 0, DateTime? purchaseTime = null, string suid = null)
        {
            var metadata = product?.metadata;
            if (metadata == null || string.IsNullOrWhiteSpace(metadata.isoCurrencyCode))
            {
                Debug.LogError("[HighbrowIap] Purchase log requires Unity IAP product metadata with a store currency.");
                return;
            }

            HighbrowLog.TrackPurchase(
                receiptId: receiptId,
                storeCurrency: metadata.isoCurrencyCode,
                storePrice: (float)metadata.localizedPrice,
                priceId: product.definition.id,
                productId: productId,
                productName: metadata.localizedTitle,
                purchaseTime: purchaseTime,
                suid: suid);
        }
    }
}
