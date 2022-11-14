using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BamBooShop.Dto;
using BamBooShop.Model;
using Microsoft.AspNetCore.Hosting;

namespace BamBooShop.Service
{
    public class GalleryService : IServiceBase<GalleryDto, int>
    {
        protected readonly MyContext context;
        protected IWebHostEnvironment hostEnvironment;
        protected readonly CloudImgUploadService cloudImgUpload;

        public GalleryService(MyContext context, IWebHostEnvironment hostEnvironment, CloudImgUploadService cloudImgUpload)
        {
            this.context = context;
            this.hostEnvironment = hostEnvironment;
            this.cloudImgUpload = cloudImgUpload;

        }

        public void DeleteById(int key, string userSession = null)
        {
            this.context.Galleries.Remove(context.Galleries.FirstOrDefault(x => x.Id == key));
            this.context.SaveChanges();
        }

        public List<GalleryDto> GetAll()
        {
            return this.context.Galleries.Select(x => new GalleryDto()
            {
                Id = x.Id,
                Image = x.Image,
                Type = x.Type,
                BanerCloudLink = x.BanerCloudLink,

            }).ToList();
        }

        public GalleryDto GetById(int key)
        {
            return this.context.Galleries
                .Where(x => x.Id == key)
                .Select(x => new GalleryDto()
                {
                    Id = x.Id,
                    Image = x.Image,
                    Type = x.Type,
                    BanerCloudLink = x.BanerCloudLink,
                }).FirstOrDefault();
        }

        public GalleryDto Insert(GalleryDto entity)
        {
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

            Gallery gallery = new Gallery()
            {
                Image = entity.Image,
                Type = entity.Type,
                BanerCloudLink = entity.BanerCloudLink,

            };

/*            gallery.Image = entity.Image + "-" + gallery.Id;
*/
            try
            {
                var uploadResult = this.cloudImgUpload.ImgUpload(imagePath, gallery.Image, cloudinary);
                entity.BanerCloudLink = uploadResult;
            }
            catch (Exception ex)
            {
                entity.BanerCloudLink = "";
            }

            gallery.BanerCloudLink = entity.BanerCloudLink;
            this.context.Galleries.Add(gallery);
            this.context.SaveChanges();
            return entity;

        }

        public void Update(int key, GalleryDto entity)
        {
            Gallery gallery = this.context.Galleries.FirstOrDefault(x => x.Id == key);

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

            if (gallery.Image != entity.Image)
            {
                var oldImage = gallery.Image;
                gallery.Image = entity.Image + "-" + entity.Id;

                var renameImage = this.cloudImgUpload.RenameImg(oldImage, gallery.Image, cloudinary);

                gallery.BanerCloudLink = this.cloudImgUpload.ImgUpload(imagePath, gallery.Image, cloudinary);
            }

            gallery.Image = entity.Image;
            gallery.Type = entity.Type;

            this.context.SaveChanges();
        }
    }
}