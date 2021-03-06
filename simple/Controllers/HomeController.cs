﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using simple.Models;
using Demeter.FormComponent;

namespace simple.Controllers
{
    public class HomeController : Controller
    {
        private readonly FormManager<Message> _messageManager;

        public HomeController(FormManager<Message> messageManager)
        {
            this._messageManager = messageManager;
        }

        public async Task<IActionResult> Index(string queryString = null)
        {   
            if(queryString == null)
            {
                ViewBag.Messages = (await this._messageManager.QueryAsync(query => query
                    .OrderByDescending(m => m.CreateOn)
                    .Take(20)
                    .ToList()
                ));
            }
            else
            {
                ViewBag.Messages = (await this._messageManager.QueryAsync(queryString, 20, item => item));
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Submit(string content)
        {
            var message = new Message
            {
                Content = content
            };

            var result = await this._messageManager.CreateAsync(message);

            if (result.Succeeded)
            {
                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("Error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await this._messageManager.DeleteAsync(id);
            
            if (result.Succeeded)
            {
                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("Error");
            }
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
