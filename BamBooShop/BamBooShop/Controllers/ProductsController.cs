﻿using BamBooShop.Dto;
using BamBooShop.Interface;
using BamBooShop.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BamBooShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private ProductService _productService;
        //private IProductService _iProductService;
        public ProductsController(ProductService productService 
            //IProductService iProductService
            )
        {
            this._productService = productService;
            //this._iProductService = iProductService;
        }

        [HttpGet]
        public IActionResult Get(string keySearch, int? menuId)
        {
            ResponseAPI responseAPI = new ResponseAPI();
            try
            {
                responseAPI.Data = this._productService.Get(keySearch, menuId);
                return Ok(responseAPI);
            }
            catch (Exception ex)
            {
                responseAPI.Message = ex.Message;
                return BadRequest(responseAPI);
            }
        }

        [Route("search")]
        [HttpGet]
        public IActionResult Search(string keySearch, int take = 20, string orderBy = "", string price = "")
        {
            ResponseAPI responseAPI = new ResponseAPI();
            try
            {
                responseAPI.Data = this._productService.Search(keySearch, take, orderBy, price);
                return Ok(responseAPI);
            }
            catch (Exception ex)
            {
                responseAPI.Message = ex.Message;
                return BadRequest(responseAPI);
            }
        }

        [Route("get-all")]
        [HttpGet]
        public IActionResult GetAll()
        {
            ResponseAPI responseAPI = new ResponseAPI();
            try
            {
                responseAPI.Data = this._productService.GetAll();
                return Ok(responseAPI);
            }
            catch (Exception ex)
            {
                responseAPI.Message = ex.Message;
                return BadRequest(responseAPI);
            }
        }

        [Route("get-top-product-best-seller")]
        [HttpGet]
        public IActionResult getTopProductBestSeller()
        {
            ResponseAPI responseAPI = new ResponseAPI();
            try
            {
                responseAPI.Data = this._productService.getTopProductBestSeller();
                return Ok(responseAPI);
            }
            catch (Exception ex)
            {
                responseAPI.Message = ex.Message;
                return BadRequest(responseAPI);
            }
        }

        [Route("get-product-selling")]
        [HttpGet]
        public IActionResult GetProductSelling()
        {
            ResponseAPI responseAPI = new ResponseAPI();
            try
            {
                responseAPI.Data = this._productService.GetProductSelling();
                return Ok(responseAPI);
            }
            catch (Exception ex)
            {
                responseAPI.Message = ex.Message;
                return BadRequest(responseAPI);
            }
        }

        [Route("get-by-menu")]
        [HttpGet]
        public IActionResult GetByMenu(string menuAlias, string orderBy = "", string price = "", int take = 10)
        {
            ResponseAPI responseAPI = new ResponseAPI();
            try
            {
                responseAPI.Data = this._productService.GetByMenu(menuAlias, orderBy, price, take);
                return Ok(responseAPI);
            }
            catch (Exception ex)
            {
                responseAPI.Message = ex.Message;
                return BadRequest(responseAPI);
            }
        }

        [Route("get-by-alias")]
        [HttpGet]
        public IActionResult GetByAlias(string alias)
        {
            ResponseAPI responseAPI = new ResponseAPI();
            try
            {
                var productList = this._productService.GetByAlias(alias);
                responseAPI.Data = productList;
                if (productList==null)
                {
                    responseAPI.Message = "Thông tin sản phẩm không khả dụng";
                    return BadRequest(responseAPI);
                }    
                return Ok(responseAPI);
            }
            catch (Exception ex)
            {
                responseAPI.Message = ex.Message;
                return BadRequest(responseAPI);
            }
        }

        [Route("{id}")]
        [HttpGet]
        public IActionResult GetById(int id)
        {
            ResponseAPI responseAPI = new ResponseAPI();
            try
            {
                responseAPI.Data = this._productService.GetById(id);
                return Ok(responseAPI);
            }
            catch (Exception ex)
            {
                responseAPI.Message = ex.Message;
                return BadRequest(responseAPI);
            }
        }

        [HttpPost]
        public IActionResult Post(ProductDto product)
        {
            ResponseAPI responseAPI = new ResponseAPI();
            try
            {
                this._productService.Insert(product);
                return Ok(responseAPI);
            }
            catch (Exception ex)
            {
                responseAPI.Message = ex.Message;
                return BadRequest(responseAPI);
            }
        }

        [Route("{id}")]
        [HttpPut]
        public IActionResult Put(int id, ProductDto product)
        {
            ResponseAPI responseAPI = new ResponseAPI();
            try
            {
                this._productService.Update(id, product);
                return Ok(responseAPI);
            }
            catch (Exception ex)
            {
                responseAPI.Message = ex.Message;
                return BadRequest(responseAPI);
            }
        }

        [Route("{id}")]
        [HttpDelete]
        public IActionResult Delete(int id)
        {
            ResponseAPI responseAPI = new ResponseAPI();
            try
            {
                this._productService.DeleteById(id);
                return Ok(responseAPI);
            }
            catch (Exception ex)
            {
                responseAPI.Message = ex.Message;
                return BadRequest(responseAPI);
            }
        }

        [Route("search-product-autofill/{keySearch}")]
        [HttpGet]
        public IActionResult SearchAutoFill(string keySearch)
        {
            ResponseAPI responseAPI = new ResponseAPI();
            try
            {
                //responseAPI.Data = this._iProductService.SearchAutoFill(keySearch);
                responseAPI.Data = this._productService.SearchAutoFill(keySearch);
                return Ok(responseAPI);
            }
            catch (Exception ex)
            {
                responseAPI.Message = ex.Message;
                return BadRequest(responseAPI);
            }
        }
    }
}
