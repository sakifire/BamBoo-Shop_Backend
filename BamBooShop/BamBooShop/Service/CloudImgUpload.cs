using BamBooShop.Model;
using BamBooShop.Util;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using System;

namespace BamBooShop.Service
{
    public class CloudImgUploadService
    {
        public Cloudinary cloudinaryLogin()
        {
            try
            {
                Account account = new Account(Constants.CloudinaryAccount.CloudName,
                                    Constants.CloudinaryAccount.APIKey,
                                    Constants.CloudinaryAccount.APISecret);
                Cloudinary cloudinary = new Cloudinary(account);
                cloudinary.Api.Secure = true;
                return cloudinary;
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Lỗi kết nối");               
            }
        }
        public string ImgUpload(string imgPath, string imgPublicId, Cloudinary cloudinary)
        {
            try
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(imgPath),
                    PublicId = "BamBooShop/" + imgPublicId,
                    Overwrite = true,
                };
                try
                {
                    var uploadResult = cloudinary.Upload(uploadParams);
                    return uploadResult.SecureUrl.ToString();

                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            catch
            {
                throw new ArgumentException("Không lưu được ảnh");
            }
        }
        public string RenameImg(string oldPublicId, string newPublicId, Cloudinary cloudinary)
        {
            try
            {              
                try
                {
                    var uploadResult = cloudinary.Rename("BamBooShop/" + oldPublicId, "BamBooShop/" + newPublicId);
                    return uploadResult.SecureUrl.ToString();

                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            catch
            {
                throw new ArgumentException("Lỗi khi đổi tên ảnh");
            }
        }
        public bool DeleteImg(string publicId, Cloudinary cloudinary)
        {
            try
            {
                var deletionParams = new DeletionParams("BamBooShop/" + publicId)
                {
                    ResourceType = ResourceType.Image
                };
                var deletionResult = cloudinary.Destroy(deletionParams);
                if (deletionResult.Result.ToLower() == "ok")  
                    return true;
                else return false;
            }
            catch
            {
                return false;
            }
        }

    }
}
