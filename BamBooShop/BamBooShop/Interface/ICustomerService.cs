using BamBooShop.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BamBooShop.Interface
{
    public interface ICustomerService
    {
        void RequestOTP(string email);
        bool ConfirmOTP(string email, string otp);
        void ForgotPassword(string email);
        object GetAccessToken(CustomerDto entity);
        List<OrderDto> GetOrders(string customerCode);
        List<CustomerDto> Get(string keySearch);
        void ChangePassword(string key, string oldPassword, string newPassword);
        void GetSocialNetworkAccessToken(CustomerDto entity);
        CustomerDto InsertSocialNetworkAccount(CustomerDto entity);
    }
}
