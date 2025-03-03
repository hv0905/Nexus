﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Aiursoft.Pylon;
using Aiursoft.Pylon.Models;
using Aiursoft.Pylon.Attributes;

namespace Aiursoft.Probe.Controllers
{
    [LimitPerMin]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return this.Protocol(ErrorType.Success, "Welcome to Probe!");
        }
    }
}
