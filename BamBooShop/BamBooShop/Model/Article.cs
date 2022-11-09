﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BamBooShop.Model
{
    [Table("Article")]
    public class Article
    {
        public int Id { get; set; }
        public int? MenuId { get; set; }
        public string Title { get; set; }
        public string Alias { get; set; }
        public string Image { get; set; }
        public int? Index { get; set; }
        public string ShortDescription { get; set; }
        public string ImageCloudLink { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public DateTime Created { get; set; }

        public virtual Menu Menu { get; set; }
    }
}
