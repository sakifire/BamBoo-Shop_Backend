using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BamBooShop.Dto;
using BamBooShop.Model;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace BamBooShop.Service
{
    public class EmailTemplateService : IServiceBase<EmailTemplateDto, int>
    {
        protected readonly MyContext context;
        public EmailTemplateService(MyContext context)
        {
            this.context = context;
        }

        public virtual void DeleteById(int key, string userSession = null)
        {
            throw new NotImplementedException();
        }

        public virtual List<EmailTemplateDto> GetAll()
        {
            return this.context.EmailTemplates
                 .Select(x => new EmailTemplateDto()
                 {
                     Id = x.Id,
                     Subject = x.Subject,
                     Type = x.Type
                 })
                 .ToList();
        }

        public virtual EmailTemplateDto GetById(int key)
        {
            return this.context.EmailTemplates
                 .Where(x => x.Id == key)
                 .Select(x => new EmailTemplateDto()
                 {
                     Content = x.Content,
                     BCC = x.BCC,
                     CC = x.CC,
                     Id = x.Id,
                     KeyGuide = x.KeyGuide,
                     Subject = x.Subject,
                     Type = x.Type
                 })
                 .FirstOrDefault();
        }

        public virtual EmailTemplateDto Insert(EmailTemplateDto entity)
        {
            try
            {
                var emailTemplate = new EmailTemplate();
                emailTemplate.Subject = entity.Subject;
                emailTemplate.Type = entity.Type;
                emailTemplate.BCC = entity.BCC;
                emailTemplate.CC = entity.CC;
                emailTemplate.KeyGuide = entity.KeyGuide;
                emailTemplate.Content = entity.Content;

                this.context.EmailTemplates.Add(emailTemplate);
                this.context.SaveChanges();
                return entity;
            }catch(Exception ex)
            {
                throw new ArgumentException(ex.Message);

            }
        }

        public virtual void Update(int key, EmailTemplateDto entity)
        {
            EmailTemplate emailTemplate = this.context.EmailTemplates
                 .FirstOrDefault(x => x.Id == key);

            emailTemplate.Subject = entity.Subject;
            emailTemplate.CC = entity.CC;
            emailTemplate.BCC = entity.BCC;
            emailTemplate.Content = entity.Content;

            this.context.SaveChanges();
        }
    }
}