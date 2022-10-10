using System.Collections.Generic;

namespace BamBooShop.Interface
{
    public interface IProductService
    {
        List<string> SearchAutoFill(string keySearch);
    }
}
