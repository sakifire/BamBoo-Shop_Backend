using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BamBooShop.Dto;
using BamBooShop.Interface;
using BamBooShop.Model;
using BamBooShop.Util;
using Castle.Core.Resource;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;

namespace BamBooShop.Service
{
    public class CustomerService : IServiceBase<CustomerDto, string>
    {
        protected readonly MyContext context;
        //private readonly JwtGenerator _jwtGenerator;

        public CustomerService(MyContext context)
        {
            this.context = context;
        }
        public class AuthenticateRequest
        {
            [Required]
            public string IdToken { get; set; }
        }
        /// <summary>
        /// Gửi yêu cầu OTP vào email khi đăng ký tài khoản
        /// </summary>
        /// <param name="email"></param>
        public void RequestOTP(string email)
        {
            if (this.context.Customers.Any(x => x.Email == email))
                throw new ArgumentException("Email existed");
            EmailSignUp emailSignUp = this.context.EmailSignUps.FirstOrDefault(x => x.Email == email);

            string otp = new Random().Next(100000, 999999).ToString();
            if (emailSignUp == null)
            {
                emailSignUp = new EmailSignUp()
                {
                    Email = email,
                    OTP = otp
                };
                this.context.EmailSignUps.Add(emailSignUp);
            }
            else
            {
                emailSignUp.OTP = otp;
            }
            EmailConfiguration emailConfiguration = this.context.EmailConfigurations.FirstOrDefault();
            EmailTemplate emailTemplate = this.context.EmailTemplates.FirstOrDefault(x => x.Id == 6);

            string bodyMail = emailTemplate.Content.Replace(Constants.EmailKeyGuide.OTP, otp);
            DataHelper.SendMail(emailConfiguration, emailTemplate.Subject, bodyMail, new List<string>()
            {
                email
            }, emailTemplate.CC?.Split(';').ToList(), emailTemplate.BCC?.Split(';').ToList());
            this.context.SaveChanges();
        }

        /// <summary>
        /// Xác thực mã OTP khi đăng ký tài khoản
        /// </summary>
        /// <param name="email"></param>
        /// <param name="otp"></param>
        /// <returns></returns>
        public bool ConfirmOTP(string email, string otp)
        {
            return this.context.EmailSignUps.Any(x => x.Email == email && x.OTP == otp);
        }

        /// <summary>
        /// Gửi yêu cầu cấp lại mật khẩu
        /// </summary>
        /// <param name="email"></param>
        public void ForgotPassword(string email)
        {
            if (!this.context.Customers.Any(x => x.Email == email))
                throw new ArgumentException("Email hasn't been regitered yet");
            Customer customer = this.context.Customers.FirstOrDefault(x => x.Email == email);

            string newPassword = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

            customer.Password = DataHelper.SHA256Hash(customer.Email + "_" + newPassword);

            EmailConfiguration emailConfiguration = this.context.EmailConfigurations.FirstOrDefault();
            EmailTemplate emailTemplate = this.context.EmailTemplates.FirstOrDefault(x => x.Id == 5);

            string bodyMail = emailTemplate.Content.Replace(Constants.EmailKeyGuide.NewPassword, newPassword);
            DataHelper.SendMail(emailConfiguration, emailTemplate.Subject, bodyMail, new List<string>()
            {
                email
            }, emailTemplate.CC?.Split(';').ToList(), emailTemplate.BCC?.Split(';').ToList());
            this.context.SaveChanges();
        }

        /// <summary>
        /// Đăng nhập
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public object GetAccessToken(CustomerDto entity)
        {
            Customer customer = this.context.Customers
                .FirstOrDefault(x => x.Email == entity.Email);

            if (customer == null)
                throw new ArgumentException("Email or Password is incorrect");

            string passwordCheck = DataHelper.SHA256Hash(entity.Email + "_" + entity.Password);

            if (customer.Password != passwordCheck)
                throw new ArgumentException("Email or Password is incorrect");

            customer.LastLogin = DateTime.Now;
            this.context.SaveChanges();

            DateTime expirationDate = DateTime.Now.Date.AddMinutes(Constants.JwtConfig.ExpirationInMinutes);
            long expiresAt = (long)(expirationDate - new DateTime(1970, 1, 1)).TotalSeconds;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(Constants.JwtConfig.SecretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                        new Claim(ClaimTypes.UserData, customer.Code),
                        new Claim(ClaimTypes.Expiration, expiresAt.ToString())
                }),
                Expires = expirationDate,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return new
            {
                customer.Email,
                customer.FullName,
                Token = tokenHandler.WriteToken(token),
                ExpiresAt = expiresAt
            };
        }

        /// <summary>
        /// Lấy danh sách đơn hàng của khách hàng
        /// </summary>
        /// <param name="customerCode"></param>
        /// <returns></returns>
        public List<OrderDto> GetOrders(string customerCode)
        {
            return this.context.Orders
                .Where(x => x.CustomerCode == customerCode)
                .OrderByDescending(x => x.Created)
                .Select(x => new OrderDto()
                {
                    Id = x.Id,
                    Address = x.Address,
                    CustomerCode = x.CustomerCode,
                    PhoneNumber = x.PhoneNumber,
                    Status = x.Status,
                    TotalAmount = x.TotalAmount,
                    IsPaid = x.IsPaid,
                    OrderDetails = x.OrderDetails.Select(y => new OrderDetailDto()
                    {
                        Id = y.Id,
                        OrderId = y.OrderId,
                        ProductDiscountPrice = y.ProductDiscountPrice,
                        ProductImage = y.ProductImage,
                        ProductName = y.ProductName,
                        ProductPrice = y.ProductPrice,
                        Qty = y.Qty,
                        Attribute = y.Attribute,
                        Reviews = y.Reviews.Select(z => new ReviewDto()
                        {
                            Content = z.Content,
                            Star = z.Star,
                            CreatedBy = z.CreatedBy,
                            Status = z.Status,
                            Created = z.Created
                        }).ToList()
                    }).ToList(),
                    Created = x.Created
                })
                .ToList();
        }

        public virtual void DeleteById(string key, string userSession = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get danh sách khách hàng theo từ khóa
        /// </summary>
        /// <param name="keySearch"></param>
        /// <returns></returns>
        public List<CustomerDto> Get(string keySearch)
        {
            if (string.IsNullOrWhiteSpace(keySearch))
                keySearch = null;
            else keySearch = keySearch.ToLower();

            return this.context.Customers
                 .Where(x => keySearch == null || x.FullName.ToLower().Contains(keySearch)
                        || x.Email.ToLower().Contains(keySearch) || x.PhoneNumber.Contains(keySearch))
                 .Select(x => new CustomerDto()
                 {
                     Code = x.Code,
                     Address = x.Address,
                     Avatar = x.Avatar,
                     Dob = x.Dob,
                     Email = x.Email,
                     FullName = x.FullName,
                     Gender = x.Gender,
                     PhoneNumber = x.PhoneNumber
                 })
                 .ToList();
        }


        public List<CustomerDto> GetTopOrderCustomer()
        {

            return this.context.Customers
                 .Select(x => new CustomerDto()
                 {
                     Code = x.Code,
                     Address = x.Address,
                     Avatar = x.Avatar,
                     Dob = x.Dob,
                     Email = x.Email,
                     FullName = x.FullName,
                     Gender = x.Gender,
                     PhoneNumber = x.PhoneNumber,
                     TotalAmountOrder = x.Orders.Select(y => y.TotalAmount).Sum()
                 })
                 .OrderByDescending(x => x.TotalAmountOrder)
                 .Take(10).ToList();
        }

        public virtual List<CustomerDto> GetAll()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get thông tin khách hàng theo id
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual CustomerDto GetById(string key)
        {
            return this.context.Customers
                  .Where(x => x.Code == key)
                  .Select(x => new CustomerDto()
                  {
                      Code = x.Code,
                      Address = x.Address,
                      Avatar = x.Avatar,
                      Dob = x.Dob,
                      Email = x.Email,
                      FullName = x.FullName,
                      Gender = x.Gender,
                      PhoneNumber = x.PhoneNumber,
                      Orders = x.Orders.OrderByDescending(y => y.Created).Select(y => new OrderDto()
                      {
                          Id = y.Id,
                          Address = y.Address,
                          Created = y.Created,
                          Note = y.Note,
                          PhoneNumber = y.PhoneNumber,
                          Status = y.Status,
                          TotalAmount = y.TotalAmount,
                          OrderDetails = y.OrderDetails.Select(z => new OrderDetailDto()
                          {
                              Attribute = z.Attribute,
                              ProductDiscountPrice = z.ProductDiscountPrice,
                              ProductImage = z.ProductImage,
                              ProductName = z.ProductName,
                              ProductPrice = z.ProductPrice,
                              Qty = z.Qty
                          }).ToList()
                      }).ToList()
                  })
                  .FirstOrDefault();
        }

        /// <summary>
        /// Thêm mới tài khoản khách hàng
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual CustomerDto Insert(CustomerDto entity)
        {
            if (!this.context.EmailSignUps.Any(x => x.Email == entity.Email && x.OTP == entity.OTP))
                throw new ArgumentException("Wrong OTP");

            if (this.context.Customers.Any(x => x.Email == entity.Email))
                throw new ArgumentException("Email existed");

            if (this.context.Customers.Any(x => x.PhoneNumber == entity.PhoneNumber))
                throw new ArgumentException("PhoneNumber existed");

            Customer customer = new Customer()
            {
                Code = Guid.NewGuid().ToString("N"),
                FullName = entity.FullName,
                PhoneNumber = entity.PhoneNumber,
                Email = entity.Email,
                Password = DataHelper.SHA256Hash(entity.Email + "_" + entity.Password)
            };

            this.context.Customers.Add(customer);
            this.context.SaveChanges();

            return entity;
        }

        /// <summary>
        /// Cập nhật thông tin khách hàng
        /// </summary>
        /// <param name="key"></param>
        /// <param name="entity"></param>
        public virtual void Update(string key, CustomerDto entity)
        {
            Customer customer = this.context.Customers
                .FirstOrDefault(x => x.Code == key);

            if (customer != null)
            {
                if (this.context.Customers.Any(x => x.Code != entity.Code && x.AuthToken== null && x.Email == entity.Email))
                    throw new ArgumentException("Email existed");

                if (this.context.Customers.Any(x => x.Code != entity.Code && x.PhoneNumber == entity.PhoneNumber))
                    throw new ArgumentException("This PhoneNumber has been already used");

                customer.FullName = entity.FullName;
                customer.Email = entity.Email;
                customer.PhoneNumber = entity.PhoneNumber;
                customer.Address = entity.Address;
                customer.Dob = entity.Dob;
                customer.Gender = entity.Gender;

                this.context.SaveChanges();
            }
        }

        /// <summary>
        /// Thay đổi mật khẩu
        /// </summary>
        /// <param name="key"></param>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        public void ChangePassword(string key, string oldPassword, string newPassword)
        {
            Customer customer = this.context.Customers
                 .FirstOrDefault(x => x.Code == key);

            string passwordCheck = DataHelper.SHA256Hash(customer.Email + "_" + oldPassword);

            if (customer.Password != passwordCheck)
                throw new ArgumentException("Old Password is incorrect");
            else
            {
                string newCheck = DataHelper.SHA256Hash(customer.Email + "_" + newPassword);
                customer.Password = newCheck;

                this.context.SaveChanges();
            }
        }

        /// <summary>
        /// Đăng nhập qua social network
        /// </summary>
        /// <param name="entity"></param>
        public void GetSocialNetworkAccessToken(CustomerDto entity)
        {
            //Customer customer = this.context.Customers
            //    .FirstOrDefault(x => x.Email == entity.Email);

            //if (customer == null)
            //    throw new ArgumentException("Tài khoản hoặc mật khẩu không đúng");

            //string passwordCheck = DataHelper.SHA256Hash(entity.Email + "_" + entity.Password);

            //if (customer.Password != passwordCheck)
            //    throw new ArgumentException("Tài khoản hoặc mật khẩu không đúng");

            //customer.LastLogin = DateTime.Now;
            //this.context.SaveChanges();

            //DateTime expirationDate = DateTime.Now.Date.AddMinutes(Constants.JwtConfig.ExpirationInMinutes);
            //long expiresAt = (long)(expirationDate - new DateTime(1970, 1, 1)).TotalSeconds;

            //var tokenHandler = new JwtSecurityTokenHandler();
            //var key = Encoding.ASCII.GetBytes(Constants.JwtConfig.SecretKey);
            //var tokenDescriptor = new SecurityTokenDescriptor
            //{
            //    Subject = new ClaimsIdentity(new Claim[]
            //    {
            //            new Claim(ClaimTypes.UserData, customer.Code),
            //            new Claim(ClaimTypes.Expiration, expiresAt.ToString())
            //    }),
            //    Expires = expirationDate,
            //    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            //};
            //var token = tokenHandler.CreateToken(tokenDescriptor);

            //return new
            //{
            //    customer.Email,
            //    customer.FullName,
            //    Token = tokenHandler.WriteToken(token),
            //    ExpiresAt = expiresAt
            //};
        }

        /// <summary>
        /// Thêm mới tài khoản Social network
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual SocialNetworkCustomerDto InsertSocialNetworkAccount(SocialNetworkCustomerDto entity)
        {
            try
            {
                var isExistCustomer = this.context.Customers
                        .FirstOrDefault(x => x.Email == entity.Email);
                if (isExistCustomer != null)
                {
                    throw new ArgumentException("This Email has already been used");
                }
                else
                {
                    Customer customer = new Customer()
                    {
                        Code = Guid.NewGuid().ToString("N"),
                        FullName = entity.FullName,
                        //PhoneNumber = "",
                        Email = entity.Email,
                        Password = DataHelper.SHA256Hash(entity.Email),
                        AuthToken = entity.AuthToken,
                        Avatar = entity.Avatar,
                        IdToken = entity.IdToken
                    };

                    this.context.Customers.Add(customer);
                    this.context.SaveChanges();

                    return entity;
                }
            }
            catch(Exception ex)
            {
                throw new ArgumentException("This Email has already been used");
            }


        }

        /// <summary>
        /// Login bằng Social network
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// 
        public object LoginWithGoogleAccount(CustomerDto entity)
        {
            try
            {
                // gg configure
                GoogleJsonWebSignature.ValidationSettings settings = new GoogleJsonWebSignature.ValidationSettings();

                settings.Audience = new List<string>() { "251499186409-9rhhvhr9o1jgnrj4luf7gcro2q5l26r6.apps.googleusercontent.com" };

                GoogleJsonWebSignature.Payload payload = GoogleJsonWebSignature.ValidateAsync(entity.IdToken, settings).Result;
                //
                Customer customer = this.context.Customers
                    .FirstOrDefault(x => x.Email == payload.Email && x.IdToken != null && x.AuthToken != null);
                if(customer == null)
                {
                    throw new ArgumentException("This Email is not exist");
                }
                customer.LastLogin = DateTime.Now;
                customer.IdToken = entity.IdToken;
                customer.AuthToken = entity.AuthToken;
                this.context.SaveChanges();

                DateTime expirationDate = DateTime.Now.Date.AddMinutes(Constants.JwtConfig.ExpirationInMinutes);
                long expiresAt = (long)(expirationDate - new DateTime(1970, 1, 1)).TotalSeconds;

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(Constants.JwtConfig.SecretKey);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.Sid, payload.Email.ToString()),
                        new Claim(ClaimTypes.UserData, customer.Code),
                        new Claim(ClaimTypes.Expiration, expiresAt.ToString())
                    }),
                    Expires = expirationDate,
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);

                return new
                {
                    customer.Email,
                    customer.FullName,
                    Token = tokenHandler.WriteToken(token),
                    ExpiresAt = expiresAt
                };
            }
            catch(Exception ex)
            {
                throw new ArgumentException("Error");
            }
        }
    }
}