using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BamBooShop.Dto;
using BamBooShop.Interface;
using BamBooShop.Model;
using BamBooShop.Util;
using Microsoft.AspNetCore.Hosting;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Castle.Core.Configuration;

namespace BamBooShop.Service
{
    public class ProductService : IServiceBase<ProductDto, int>, IProductService
    {
        protected readonly MyContext context;
        protected IWebHostEnvironment hostEnvironment;
        protected readonly CloudImgUploadService cloudImgUpload;

        public ProductService(MyContext context, IWebHostEnvironment hostEnvironment, CloudImgUploadService cloudImgUpload)
        {
            this.context = context;
            this.hostEnvironment = hostEnvironment;
            this.cloudImgUpload = cloudImgUpload;
        }


        public void DeleteById(int key, string userSession = null)
        {
            using (var transaction = this.context.Database.BeginTransaction())
            {
                Product product = this.context.Products.FirstOrDefault(x => x.Id == key);
                if (product != null)
                {
                    product.IsDeleted = true;
                    this.context.Products.Update(product);
                }
                //this.context.ProductAttributes.RemoveRange(product.ProductAttributes);
                //this.context.ProductImages.RemoveRange(product.ProductImages);
                this.context.ProductRelateds.RemoveRange(product.ProductRelateds);
                //this.context.Reviews.RemoveRange(product.Reviews);
                //this.context.Products.Remove(product);

                this.context.SaveChanges();
                transaction.Commit();
            }
        }
        public void DeleteByListId(List<int> key, string userSession = null)
        {
            using (var transaction = this.context.Database.BeginTransaction())
            {
                foreach(var item in key)
                {
                    Product product = this.context.Products.FirstOrDefault(x => x.Id == item);
                    if (product != null)
                    {
                        product.IsDeleted = true;
                        this.context.Products.Update(product);
                    }
                    //this.context.ProductAttributes.RemoveRange(product.ProductAttributes);
                    //this.context.ProductImages.RemoveRange(product.ProductImages);
                    this.context.ProductRelateds.RemoveRange(product.ProductRelateds);
                    //this.context.Reviews.RemoveRange(product.Reviews);
                    //this.context.Products.Remove(product);
                }
                this.context.SaveChanges();
                transaction.Commit();
            }
        }
        public List<ProductDto> Get(string keySearch, int? menuId)
        {
            if (string.IsNullOrWhiteSpace(keySearch))
                keySearch = null;
            else keySearch = keySearch.ToLower();
            return this.context.Products
                //hmtien add 19/8
                .Where(x => !x.IsDeleted)
                .Where(x => keySearch == null || x.Name.ToLower().Contains(keySearch) || x.Alias.ToLower().Contains(keySearch))
                .Where(x => menuId == null || x.MenuId == menuId)
                .OrderBy(x => x.Menu.Index)
                .ThenBy(x => x.Index)
                .Select(x => new ProductDto()
                {
                    Id = x.Id,
                    Alias = x.Alias,
                    Description = x.Description,
                    DiscountPrice = x.DiscountPrice,
                    Selling = x.Selling,
                    Image = x.Image,
                    Index = x.Index,
                    MenuId = x.MenuId,
                    Price = x.Price,
                    Name = x.Name,
                    ShortDescription = x.ShortDescription,
                    Status = x.Status,
                    //hmtien add 25/8
                    Quantity = x.Quantity,
                    ImageCloudLink = x.ImageCloudLink,
                    Menu = x.Menu == null ? null : new MenuDto()
                    {
                        Name = x.Menu.Name
                    }
                })
                .ToList();
        }

        public List<ProductDto> GetAll()
        {
            return this.context.Products
                //hmtien add 19/8
                .Where(x => !x.IsDeleted)
                .Select(x => new ProductDto()
                {
                    Id = x.Id,
                    Name = x.Name,
                    Image = x.Image,
                    //hmtien add 25/8
                    Quantity = x.Quantity,
                    ImageCloudLink= x.ImageCloudLink
                })
                .ToList();
        }

        public List<OrderDetailDto> getTopProductBestSeller()
        {
            return this.context.OrderDetails
                .GroupBy(x => new { 
                    ProductId = x.ProductId, 
                    ProductName = x.ProductName, 
                    ProductImage = x.ProductImage,
                    MenuName = x.Product.Menu.Name,
                    Alias = x.Product.Alias
                })
                .Select(x => new OrderDetailDto()
                {
                    ProductId = x.Key.ProductId,
                    ProductName = x.Key.ProductName,
                    ProductImage = x.Key.ProductImage,
                    MenuName = x.Key.MenuName,
                    Alias = x.Key.Alias,
                    TotalProductBestSeller = x.Sum(i => i.Qty)
                })
                .OrderByDescending(x => x.TotalProductBestSeller)
                .Take(6)
                .ToList();
        }

        /// <summary>
        /// Get danh sách sản phẩm bán chạy hiển thị trên trang homepage
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public List<ProductDto> GetProductSelling()
        {
            List<ProductDto> products = this.context.Products
                //hmtien add 19/8
                .Where(x => !x.IsDeleted)
                .Where(x => x.Status == 10)
                .Where(x => x.Selling == true)
                .OrderBy(x => x.Index)
                .Select(x => new ProductDto()
                {
                    Id = x.Id,
                    Name = x.Name,
                    Image = x.Image,
                    Price = x.Price,
                    DiscountPrice = x.DiscountPrice,
                    Alias = x.Alias,
                    //hmtien add 20/8
                    //IsDeleted = x.IsDeleted,
                    //hmtien add 25/8
                    Quantity = x.Quantity,
                    ImageCloudLink = x.ImageCloudLink,
                    ProductAttributes = x.ProductAttributes.Select(y => new ProductAttributeDto()
                    {
                        Attribute = new AttributeDto()
                        {
                            Id = y.Attribute.Id,
                            Name = y.Attribute.Name
                        },
                        AttributeId = y.AttributeId,
                        Value = y.Value
                    }).ToList(),
                })
                .Take(8)
                .ToList();

            this.RestructureAttribute(products);
            return products;
        }

        /// <summary>
        /// Get danh sách sản phẩm theo filter
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public List<ProductDto> Search(string keySearch, int take, string orderBy = "", string price = "")
        {
            List<ProductDto> query = this.context.Products
                //hmtien add 19/8
                .Where(x => !x.IsDeleted)
                .Where(x => x.Status == 10)
                .OrderBy(x => x.Index)
                .Select(x => new ProductDto()
                {
                    Id = x.Id,
                    Name = x.Name,
                    Image = x.Image,
                    Price = x.Price,
                    DiscountPrice = x.DiscountPrice,
                    Alias = x.Alias,
                    //hmtien add 20/8
                    //IsDeleted = x.IsDeleted,
                    //hmtien add 25/8
                    ShortDescription = x.ShortDescription,
                    Quantity = x.Quantity,
                    ImageCloudLink = x.ImageCloudLink,
                    ProductAttributes = x.ProductAttributes.Select(y => new ProductAttributeDto()
                    {
                        Attribute = new AttributeDto()
                        {
                            Id = y.Attribute.Id,
                            Name = y.Attribute.Name
                        },
                        AttributeId = y.AttributeId,
                        Value = y.Value
                    }).ToList(),
                })
                .ToList();
            this.RestructureAttribute(query);

            if (!string.IsNullOrWhiteSpace(keySearch))
            {
                string lowerKeySeach = keySearch.ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(lowerKeySeach) || x.Alias.ToLower().Contains(lowerKeySeach)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                switch (orderBy)
                {
                    case "highlight":
                        break;
                    case "price-asc":
                        query = query.OrderBy(x => x.DiscountPrice).ToList();
                        break;
                    case "price-desc":
                        query = query.OrderByDescending(x => x.DiscountPrice).ToList();
                        break;
                    case "az":
                        query = query.OrderBy(x => x.Name).ToList();
                        break;
                    case "za":
                        query = query.OrderByDescending(x => x.Name).ToList();
                        break;
                }
            }

            if (!string.IsNullOrWhiteSpace(price))
            {
                switch (price)
                {
                    case "m30":
                        query = query.Where(x => x.DiscountPrice >= 30000000).ToList();
                        break;
                    case "f20t30":
                        query = query.Where(x => x.DiscountPrice >= 20000000 && x.DiscountPrice < 30000000).ToList();
                        break;
                    case "f10t20":
                        query = query.Where(x => x.DiscountPrice >= 10000000 && x.DiscountPrice < 20000000).ToList();
                        break;
                    case "f5t10":
                        query = query.Where(x => x.DiscountPrice >= 5000000 && x.DiscountPrice < 10000000).ToList();
                        break;
                    case "l5":
                        query = query.Where(x => x.DiscountPrice < 5000000).ToList();
                        break;
                }
            }

            return query.Take(take).ToList();
        }
        public List<string> SearchAutoFill(string keySearch)
        {
            try
            {
                List<string> productNames = new List<string>();
                if (!string.IsNullOrWhiteSpace(keySearch))
                {
                    keySearch = keySearch.ToLower();
                }
                else return productNames;

                productNames = this.context.Products
                    .Where(x => x.Name.ToLower().Contains(keySearch) || x.Alias.ToLower().Contains(keySearch))
                    .Take(10)
                    .Select(x => x.Name)
                    .ToList();
                return productNames;
            }
            catch (Exception ex)
            {
                return new List<string>();
            }

        }
        /// <summary>
        /// Get danh sách sản phẩm theo alias của danh mục menu
        /// </summary>
        /// <param name="menuAlias"></param>
        /// <param name="orderBy"></param>
        /// <param name="price"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public MenuDto GetByMenu(string menuAlias, string orderBy = "", string price = "", int take = 30)
        {
            MenuDto menu = this.context.Menus
                .Where(x => x.Alias == menuAlias)
                .Select(x => new MenuDto()
                {
                    Id = x.Id,
                    Name = x.Name,
                    Alias = x.Alias
                }).FirstOrDefault();

            List<ProductDto> query = this.context.Products
                //hmtien add 19/8
                .Where(x => !x.IsDeleted)
                .Where(x => x.Status == 10 && x.MenuId == menu.Id)
                .OrderBy(x => x.Index)
                .Select(x => new ProductDto()
                {
                    Id = x.Id,
                    Name = x.Name,
                    Image = x.Image,
                    Price = x.Price,
                    DiscountPrice = x.DiscountPrice,
                    Alias = x.Alias,
                    //hmtien add 20/8
                    //IsDeleted = x.IsDeleted,
                    //hmtien add 25/8
                    ShortDescription = x.ShortDescription,
                    Quantity = x.Quantity,
                    ImageCloudLink = x.ImageCloudLink,
                    ProductAttributes = x.ProductAttributes.Select(y => new ProductAttributeDto()
                    {
                        Attribute = new AttributeDto()
                        {
                            Id = y.Attribute.Id,
                            Name = y.Attribute.Name
                        },
                        AttributeId = y.AttributeId,
                        Value = y.Value
                    }).ToList(),
                })
                .ToList();

            this.RestructureAttribute(query);

            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                switch (orderBy)
                {
                    case "highlight":
                        break;
                    case "price-asc":
                        query = query.OrderBy(x => x.DiscountPrice).ToList();
                        break;
                    case "price-desc":
                        query = query.OrderByDescending(x => x.DiscountPrice).ToList();
                        break;
                    case "az":
                        query = query.OrderBy(x => x.Name).ToList();
                        break;
                    case "za":
                        query = query.OrderByDescending(x => x.Name).ToList();
                        break;
                }
            }

            if (!string.IsNullOrWhiteSpace(price))
            {
                switch (price)
                {
                    case "m30":
                        query = query.Where(x => x.DiscountPrice >= 30000000).ToList();
                        break;
                    case "f20t30":
                        query = query.Where(x => x.DiscountPrice >= 20000000 && x.DiscountPrice < 30000000).ToList();
                        break;
                    case "f10t20":
                        query = query.Where(x => x.DiscountPrice >= 10000000 && x.DiscountPrice < 20000000).ToList();
                        break;
                    case "f5t10":
                        query = query.Where(x => x.DiscountPrice >= 5000000 && x.DiscountPrice < 10000000).ToList();
                        break;
                    case "l5":
                        query = query.Where(x => x.DiscountPrice < 5000000).ToList();
                        break;
                }
            }

            query = query.Take(take).ToList();
            if (query.Count == 0)
                query = new List<ProductDto>();

            menu.Products = query;
            return menu;
        }

        public ProductDto GetById(int key)
        {
            ProductDto productDto = new ProductDto();
            productDto = this.context.Products
                 //hmtien add 19/8
                 .Where(x => x.Id == key && !x.IsDeleted)
                 .Select(x => new ProductDto()
                 {
                     Id = x.Id,
                     //hmtien add 25/8
                     Quantity = x.Quantity,
                     Alias = x.Alias,
                     Description = x.Description,
                     DiscountPrice = x.DiscountPrice,
                     Selling = x.Selling,
                     Image = x.Image,
                     Index = x.Index,
                     MenuId = x.MenuId,
                     Price = x.Price,
                     Name = x.Name,
                     ShortDescription = x.ShortDescription,
                     //hmtien add 20/8
                     //IsDeleted = x.IsDeleted,
                     Status = x.Status,
                     ImageCloudLink = x.ImageCloudLink,
                     ProductAttributes = x.ProductAttributes.Select(y => new ProductAttributeDto()
                     {
                         AttributeId = y.AttributeId,
                         Value = y.Value
                     }).ToList(),
                     ProductRelateds = x.ProductRelateds.Select(y => new ProductRelatedDto()
                     {
                         ProductRelatedId = y.ProductRelatedId
                     }).ToList(),
                     ProductImages = x.ProductImages.Select(y => new ProductImageDto()
                     {
                         Image = y.Image,
                         ImageCloudLink = y.ImageCloudLink
                     }).ToList()
                 })
                 .FirstOrDefault();

            return productDto;
        }

        public ProductDto GetByAlias(string alias)
        {
            ProductDto product = this.context.Products
                //hmtien add 19/8
                .Where(x => !x.IsDeleted)
                .Where(x => x.Alias == alias)
                .Select(x => new ProductDto()
                {
                    Id = x.Id,
                    Alias = x.Alias,
                    Description = x.Description,
                    DiscountPrice = x.DiscountPrice,
                    Selling = x.Selling,
                    Image = x.Image,
                    Index = x.Index,
                    MenuId = x.MenuId,
                    Price = x.Price,
                    Name = x.Name,
                    ShortDescription = x.ShortDescription,
                    Status = x.Status,
                    //hmtien add 20/8
                    //IsDeleted = x.IsDeleted,
                    //hmtien add 25/8
                    Quantity = x.Quantity,
                    Menu = new MenuDto()
                    {
                        Name = x.Name
                    },
                    ProductAttributes = x.ProductAttributes.Select(y => new ProductAttributeDto()
                    {
                        Attribute = new AttributeDto()
                        {
                            Id = y.Attribute.Id,
                            Name = y.Attribute.Name
                        },
                        AttributeId = y.AttributeId,
                        Value = y.Value
                    }).ToList(),

                    ProductRelateds = x.ProductRelateds.Select(y => new ProductRelatedDto()
                    {
                        ProductRelatedId = y.ProductRelatedId
                    })
                    .Take(4).ToList(),

                    ProductImages = x.ProductImages.Select(y => new ProductImageDto()
                    {
                        Image = y.Image
                    }).ToList(),

                    Reviews = x.Reviews.OrderByDescending(y => y.Created)
                        .Where(y => y.Status == Constants.ReviewStatus.DA_DUYET).Select(y => new ReviewDto()
                        {
                            Content = y.Content,
                            Created = y.Created,
                            CreatedBy = y.CreatedBy,
                            Status = y.Status,
                            Star = y.Star,
                        }).ToList()
                })
                .FirstOrDefault();
            if (product == null) return product;
            this.RestructureAttribute(new List<ProductDto>() { product });

            product.ProductRelateds.ForEach(x =>
            {
                x.Product = this.context.Products.Where(y => y.Id == x.ProductRelatedId)
                    //hmtien add 20/8
                    .Where(x => !x.IsDeleted)
                    .Select(y => new ProductDto()
                    {
                        Id = y.Id,
                        Name = y.Name,
                        Alias = y.Alias,
                        Image = y.Image,
                        Price = y.Price,
                        DiscountPrice = y.DiscountPrice,
                        Quantity = y.Quantity
                    }).FirstOrDefault();
            });
            if (product.Reviews.Count > 0)
            {
                product.RateAvg = Math.Round((double)product.Reviews.Sum(x => x.Star) / product.Reviews.Count, 1);
            }
            return product;
        }

        public ProductDto Insert(ProductDto entity)
        {
            /*
            Account account = new Account(
                Constants.CloudinaryAccount.CloudName,
                Constants.CloudinaryAccount.APIKey,
                Constants.CloudinaryAccount.APISecret);

            Cloudinary cloudinary = new Cloudinary(account);
            cloudinary.Api.Secure = true;
             */
            var cloudinary = this.cloudImgUpload.cloudinaryLogin();
            string imagePath = entity.Image;

            if (!string.IsNullOrWhiteSpace(entity.Image))
            {
                if (entity.Image.Contains("data:image/png;base64,"))
                {
                    string path = Path.Combine(this.hostEnvironment.ContentRootPath, $"Resources/Images");

                    string imgName = Guid.NewGuid().ToString("N") + ".png";

                    var bytes = Convert.FromBase64String(entity.Image.Replace("data:image/png;base64,", ""));
                    using (var imageFile = new FileStream(path + "/" + imgName, FileMode.Create))
                    {
                        imageFile.Write(bytes, 0, bytes.Length);
                        imageFile.Flush();
                    }
                    entity.Image = imgName;
                }
            }

            Product product = new Product()
            {
                Price = entity.Price,
                Name = entity.Name,
                MenuId = entity.MenuId,
                Index = entity.Index,
                Image = entity.Image,
                DiscountPrice = entity.DiscountPrice,
                Selling = entity.Selling,
                Description = entity.Description,
                Alias = "",
                ShortDescription = entity.ShortDescription,
                Status = entity.Status,
                //Isdeleted
                IsDeleted = false,
                //hmtien add 25/8
                Quantity = entity.Quantity,
                ImageCloudLink = entity.ImageCloudLink,
            };

            product.Alias = entity.Alias + "-" + product.Id;

            if (entity.ProductImages != null && entity.ProductImages.Count > 0)
            {
                List<ProductImage> productImages = new List<ProductImage>();
                foreach (var item in entity.ProductImages.Select((value, i) => new { i, value }))
                {
                    if (!string.IsNullOrWhiteSpace(item.value.Image))
                    {
                        if (item.value.Image.Contains("data:image/png;base64,"))
                        {
                            string path = Path.Combine(this.hostEnvironment.ContentRootPath, $"Resources/Images");
                            string imgName = Guid.NewGuid().ToString("N") + ".png";
                            var bytes = Convert.FromBase64String(item.value.Image.Replace("data:image/png;base64,", ""));
                            using (var imageFile = new FileStream(path + "/" + imgName, FileMode.Create))
                            {
                                imageFile.Write(bytes, 0, bytes.Length);
                                imageFile.Flush();
                            }
                            var filePath = item.value.Image;
                            item.value.Image = imgName;
                            /*
                            var _uploadParams = new ImageUploadParams()
                            {
                                File = new FileDescription(filePath),
                                PublicId = "BamBooShop/" + product.Alias + "-" + item.i,
                                Overwrite = true,
                                //NotificationUrl = "https://mysite.example.com/my_notification_endpoint"
                            };
                            var _uploadResult = cloudinary.Upload(_uploadParams);

                            item.value.ImageCloudLink = _uploadResult.SecureUrl.ToString();
                            */
                            var _uploadResult = this.cloudImgUpload.ImgUpload(filePath, product.Alias + "-" + item.i, cloudinary);
                            item.value.ImageCloudLink = _uploadResult;
                        }
                        productImages.Add(new ProductImage()
                        {
                            Image = item.value.Image,
                            ImageCloudLink = item.value.ImageCloudLink
                        });
                    }
                }

                product.ProductImages = productImages;
            }
            if (entity.ProductAttributes != null && entity.ProductAttributes.Count > 0)
            {
                product.ProductAttributes = entity.ProductAttributes.Select(x => new ProductAttribute()
                {
                    AttributeId = x.AttributeId,
                    Value = x.Value
                }).ToList();
            }
            if (entity.ProductRelateds != null && entity.ProductRelateds.Count > 0)
            {
                product.ProductRelateds = entity.ProductRelateds.Select(x => new ProductRelated()
                {
                    ProductRelatedId = x.ProductRelatedId,
                }).ToList();
            }



            //cloud image upload
            //cloudinary file upload
            /*
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(imagePath),
                PublicId = "BamBooShop/" + product.Alias,
                Overwrite = true,
                //NotificationUrl = "https://mysite.example.com/my_notification_endpoint"
            };
            try
            {
                var uploadResult = cloudinary.Upload(uploadParams);
                entity.ImageCloudLink = uploadResult.SecureUrl.ToString();

            }
            catch (Exception ex)
            {
                entity.ImageCloudLink = "";
            }

            product.ImageCloudLink = entity.ImageCloudLink;
            //
            */
            try
            {
                var uploadResult = this.cloudImgUpload.ImgUpload(imagePath, product.Alias, cloudinary);
                entity.ImageCloudLink = uploadResult;
            }catch(Exception ex)
            {
                entity.ImageCloudLink = "";   
            }
            product.ImageCloudLink = entity.ImageCloudLink;
            this.context.Products.Add(product);
            this.context.SaveChanges();

            return entity;
        }

        public void Update(int key, ProductDto entity)
        {
            using (var transaction = this.context.Database.BeginTransaction())
            {
                /*
                Account account = new Account(
                   Constants.CloudinaryAccount.CloudName,
                   Constants.CloudinaryAccount.APIKey,
                   Constants.CloudinaryAccount.APISecret);

                Cloudinary cloudinary = new Cloudinary(account);
                cloudinary.Api.Secure = true;
                */

                var cloudinary = this.cloudImgUpload.cloudinaryLogin();

                string imagePath = entity.Image;
                if (!string.IsNullOrWhiteSpace(entity.Image))
                {
                    if (entity.Image.Contains("data:image/png;base64,"))
                    {
                        string path = Path.Combine(this.hostEnvironment.ContentRootPath, $"Resources/Images");
                        string imgName = Guid.NewGuid().ToString("N") + ".png";
                        var bytes = Convert.FromBase64String(entity.Image.Replace("data:image/png;base64,", ""));
                        using (var imageFile = new FileStream(path + "/" + imgName, FileMode.Create))
                        {
                            imageFile.Write(bytes, 0, bytes.Length);
                            imageFile.Flush();
                        }
                        entity.Image = imgName;
                    }
                }

                Product product = this.context.Products.FirstOrDefault(x => x.Id == key);
                product.MenuId = entity.MenuId;
                product.Name = entity.Name;

                if (product.Alias != entity.Alias) // change name and alias
                {
                    var oldAlias = product.Alias;
                    product.Alias = entity.Alias + "-" + key;

                    //var renameImage = cloudinary.Rename("BamBooShop/" + oldAlias, "BamBooShop/" + product.Alias);
                    var renameImage = this.cloudImgUpload.RenameImg(oldAlias, product.Alias, cloudinary);

                    int length = this.context.ProductImages.Where(x => x.ProductId == product.Id).Count();
                    for (int i = 0; i < length; i++)
                    {
                        //var _renameImage = cloudinary.Rename("BamBooShop/" + oldAlias + "-" + i, "BamBooShop/" + product.Alias+ "-" + i);
                        var _renameImage = this.cloudImgUpload.RenameImg(oldAlias + "-" + i, product.Alias + "-" + i, cloudinary);
                    }
                }
                if (product.Image != entity.Image)
                {
                    /*
                    //cloudinary file upload
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(imagePath),
                        PublicId = "BamBooShop/" + product.Alias,
                        Overwrite = true,
                        //NotificationUrl = "https://mysite.example.com/my_notification_endpoint"
                    };

                    var uploadResult = cloudinary.Upload(uploadParams);
                    entity.ImageCloudLink = uploadResult.SecureUrl.ToString();
                    product.ImageCloudLink = entity.ImageCloudLink;
                    */
                    //
                    product.ImageCloudLink = this.cloudImgUpload.ImgUpload(imagePath, product.Alias, cloudinary);
                }
                product.Image = entity.Image;
                product.Index = entity.Index;
                product.Status = entity.Status;
                product.Price = entity.Price;
                product.DiscountPrice = entity.DiscountPrice;
                product.Selling = entity.Selling;
                product.ShortDescription = entity.ShortDescription;
                product.Description = entity.Description;
                //hmtien add 25/8
                product.Quantity = entity.Quantity;
                this.context.ProductAttributes.RemoveRange(product.ProductAttributes);
                //delete img
                int productImagesDeletedLength = this.context.ProductImages.Where(x => x.ProductId == product.Id).ToList().Count();
                //
                this.context.ProductImages.RemoveRange(product.ProductImages);
                this.context.ProductRelateds.RemoveRange(product.ProductRelateds);

                if (entity.ProductAttributes != null && entity.ProductAttributes.Count > 0)
                {
                    this.context.ProductAttributes.AddRange(entity.ProductAttributes.Select(x => new ProductAttribute()
                    {
                        AttributeId = x.AttributeId,
                        ProductId = key,
                        Value = x.Value
                    }));
                }

                if (entity.ProductImages != null && entity.ProductImages.Count > 0)
                {
                    List<ProductImage> productImages = new List<ProductImage>();
                    List<ProductImage> oldProductImage = this.context.ProductImages.Where(x => x.ProductId == product.Id).ToList();
                    foreach (var item in entity.ProductImages.Select((value, i) => new { i, value }))
                    {
                        if (!string.IsNullOrWhiteSpace(item.value.Image))
                        {
                            if (item.value.Image.Contains("data:image/png;base64,")) // new image
                            {
                                var filePath = item.value.Image;

                                string path = Path.Combine(this.hostEnvironment.ContentRootPath, $"Resources/Images");
                                string imgName = Guid.NewGuid().ToString("N") + ".png";
                                var bytes = Convert.FromBase64String(item.value.Image.Replace("data:image/png;base64,", ""));
                                using (var imageFile = new FileStream(path + "/" + imgName, FileMode.Create))
                                {
                                    imageFile.Write(bytes, 0, bytes.Length);
                                    imageFile.Flush();
                                }
                                item.value.Image = imgName;

                                /*
                                var _uploadParams = new ImageUploadParams()
                                {
                                    File = new FileDescription(filePath),
                                    PublicId = "BamBooShop/" + product.Alias + "-" + item.i,
                                    Overwrite = true,
                                    //NotificationUrl = "https://mysite.example.com/my_notification_endpoint"
                                };
                                var _uploadResult = cloudinary.Upload(_uploadParams);
                                item.value.ImageCloudLink = _uploadResult.SecureUrl.ToString();
                                */
                                
                                var _uploadResult = this.cloudImgUpload.ImgUpload(filePath, product.Alias + "-" + item.i, cloudinary);
                                item.value.ImageCloudLink = _uploadResult;
                                
                                productImages.Add(new ProductImage()
                                {
                                    Image = item.value.Image,
                                    ImageCloudLink = item.value.ImageCloudLink
                                });
                            }
                            else //old image
                            {
                                productImages.Add(new ProductImage()
                                {
                                    Image = item.value.Image,
                                    ImageCloudLink = oldProductImage[item.i].ImageCloudLink
                                });
                            }
                            
                        }
                    }

                    product.ProductImages = productImages;
                }
                if (entity.ProductRelateds != null && entity.ProductRelateds.Count > 0)
                {
                    this.context.ProductRelateds.AddRange(entity.ProductRelateds.Select(x => new ProductRelated()
                    {
                        ProductId = key,
                        ProductRelatedId = x.ProductRelatedId
                    }));
                }
                if (productImagesDeletedLength > entity.ProductImages.Count())
                {
                    for (int i = entity.ProductImages.Count(); i < productImagesDeletedLength; i++)
                    {
                        /*
                        var deletionParams = new DeletionParams("BamBooShop/" + product.Alias + "-" + i)
                        {
                            ResourceType = ResourceType.Image
                        };
                        var deletionResult = cloudinary.Destroy(deletionParams);
                        */
                        var delettionResult = this.cloudImgUpload.DeleteImg(product.Alias + "-" + i, cloudinary);
                    }

                }
                this.context.SaveChanges();
                transaction.Commit();
            }
        }

        /// <summary>
        /// Cấu trúc lại thông tin thuộc tính của model sản phẩm
        /// </summary>
        /// <param name="products"></param>
        private void RestructureAttribute(List<ProductDto> products)
        {
            if (products.Count() > 0)
            {
                foreach (var product in products)
                {
                    if (product != null)
                    {
                        product.Attributes = product.ProductAttributes.Select(x => new AttributeDto()
                        {
                            Name = x.Attribute.Name,
                            Id = x.Attribute.Id
                        }).Distinct().ToList();

                        product.Attributes.ForEach(x =>
                        {
                            x.ProductAttributes = product.ProductAttributes
                                .Where(y => y.AttributeId == x.Id)
                                .Select(y => y.Value)
                                .FirstOrDefault()?.Split(',')
                                .Select(y => new ProductAttributeDto()
                                {
                                    Value = y,
                                }).ToList() ?? new List<ProductAttributeDto>();
                        });
                        product.ProductAttributes = null;
                    }
                    else return;

                };
            }
            else return;

        }
    }
}
