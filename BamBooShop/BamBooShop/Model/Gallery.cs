using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BamBooShop.Model
{
    [Table("Gallery")]
    public class Gallery
    {
        public int Id { get; set; }
        public string Image { get; set; }
        public int Type { get; set; }
        public string BanerCloudLink { get; set; }

    }
}
