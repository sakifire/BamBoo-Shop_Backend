﻿using BamBooShop.Dto;
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
    public class OrdersController : ControllerBase
    {
        private OrderService _orderService;
        private IHttpContextAccessor _contextAccessor;
        public OrdersController(OrderService orderService, IHttpContextAccessor contextAccessor)
        {
            this._orderService = orderService;
            this._contextAccessor = contextAccessor;
        }

        [HttpGet]
        public IActionResult Get(string keySearch, int status, DateTime? fDate, DateTime? tDate)
        {
            ResponseAPI responseAPI = new ResponseAPI();
            try
            {
                responseAPI.Data = this._orderService.Get(keySearch, status, fDate, tDate);
                return Ok(responseAPI);
            }
            catch (Exception ex)
            {
                responseAPI.Message = ex.Message;
                return BadRequest(responseAPI);
            }
        }

        [Route("get-wip")]
        [HttpGet]
        public IActionResult GetWIP(string keySearch, int status, DateTime? fDate, DateTime? tDate)
        {
            ResponseAPI responseAPI = new ResponseAPI();
            try
            {
                responseAPI.Data = this._orderService.GetWIP(keySearch, status, fDate, tDate);
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
                responseAPI.Data = this._orderService.GetById(id);
                return Ok(responseAPI);
            }
            catch (Exception ex)
            {
                responseAPI.Message = ex.Message;
                return BadRequest(responseAPI);
            }
        }

        [Route("change-status")]
        [HttpGet]
        public IActionResult ChangeStatus(int id, int status)
        {
            ResponseAPI responseAPI = new ResponseAPI();
            try
            {
                this._orderService.ChangeStatus(id, status);
                return Ok(responseAPI);
            }
            catch (Exception ex)
            {
                responseAPI.Message = ex.Message;
                return BadRequest(responseAPI);
            }
        }

        [HttpPost]
        public IActionResult Post(OrderDto order)
        {
            ResponseAPI responseAPI = new ResponseAPI();
            try
            {
                order.CustomerCode = SystemAuthorization.GetCurrentUser(this._contextAccessor);
                var status =this._orderService.InsertOrder(order);
                if (status)
                {
                    responseAPI.Message = "Đặt hàng thành công";
                    return Ok(responseAPI);
                }
                else {
                    responseAPI.Message = "Số lượng không khả dụng";
                    return BadRequest(responseAPI);
                }
            }
            catch (Exception ex)
            {
                responseAPI.Message = ex.Message;
                return BadRequest(responseAPI);
            }
        }

        [Route("{id}")]
        [HttpPut]
        public IActionResult Put(int id, OrderDto order)
        {
            ResponseAPI responseAPI = new ResponseAPI();
            try
            {
                this._orderService.Update(id, order);
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
                this._orderService.DeleteById(id);
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
