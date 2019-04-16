﻿using Grand.Core;
using Grand.Core.Domain.Orders;
using Grand.Services.Catalog;
using Grand.Services.Configuration;
using Grand.Services.Discounts;
using Grand.Services.Orders;
using System;
using System.Linq;

namespace Grand.Plugin.DiscountRequirements.ShoppingCart
{
    public partial class ShoppingCartDiscountRequirementRule : IDiscountRequirementRule
    {
        private readonly IWorkContext _workContext;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly ISettingService _settingService;

        public ShoppingCartDiscountRequirementRule(ISettingService settingService)
        {
            this._workContext = Core.Infrastructure.EngineContext.Current.Resolve<IWorkContext>();
            this._priceCalculationService = Core.Infrastructure.EngineContext.Current.Resolve<IPriceCalculationService>();
            this._settingService = settingService;
        }

        /// <summary>
        /// Check discount requirement
        /// </summary>
        /// <param name="request">Object that contains all information required to check the requirement (Current customer, discount, etc)</param>
        /// <returns>true - requirement is met; otherwise, false</returns>
        public DiscountRequirementValidationResult CheckRequirement(DiscountRequirementValidationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            var result = new DiscountRequirementValidationResult();

            var spentAmountRequirement = _settingService.GetSettingByKey<decimal>(string.Format("DiscountRequirement.ShoppingCart-{0}", request.DiscountRequirementId));

            if (spentAmountRequirement == decimal.Zero)
            {
                result.IsValid = true;
                return result;
            }
            var cart = _workContext.CurrentCustomer.ShoppingCartItems
                    .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                    .LimitPerStore(request.Store.Id)
                    .ToList();

            if (cart.Count == 0)
            {
                result.IsValid = false;
                return result;
            }
            decimal spentAmount = 0;
            
            foreach (var ca in cart)
            {
                bool calculateWithDiscount = false;
                spentAmount += _priceCalculationService.GetSubTotal(ca, calculateWithDiscount);
            }

            result.IsValid = spentAmount > spentAmountRequirement;
            return result;
        }

        /// <summary>
        /// Get URL for rule configuration
        /// </summary>
        /// <param name="discountId">Discount identifier</param>
        /// <param name="discountRequirementId">Discount requirement identifier (if editing)</param>
        /// <returns>URL</returns>
        public string GetConfigurationUrl(string discountId, string discountRequirementId)
        {
            //configured in RouteProvider.cs
            string result = "Admin/ShoppingCartAmount/Configure/?discountId=" + discountId;
            if (!String.IsNullOrEmpty(discountRequirementId))
                result += string.Format("&discountRequirementId={0}", discountRequirementId);
            return result;
        }
        public string FriendlyName => "SubTotal in Shopping Cart x.xx ";
        public string SystemName => "DiscountRequirement.ShoppingCart";

    }
}