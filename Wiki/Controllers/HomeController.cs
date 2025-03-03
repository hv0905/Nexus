﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Aiursoft.Wiki.Models;
using Aiursoft.Wiki.Models.HomeViewModels;
using Aiursoft.Wiki.Data;
using Microsoft.EntityFrameworkCore;
using Aiursoft.Pylon.Attributes;
using Aiursoft.Pylon;
using Aiursoft.Pylon.Models;
using Markdig;
using Aiursoft.Pylon.Services;
using Newtonsoft.Json;
using Aiursoft.Wiki.Services;
using Microsoft.Extensions.Configuration;

namespace Aiursoft.Wiki.Controllers
{
    [LimitPerMin]
    public class HomeController : Controller
    {
        private readonly SignInManager<WikiUser> _signInManager;
        private readonly ILogger _logger;
        private readonly WikiDbContext _dbContext;
        private readonly Seeder _seeder;
        private readonly ServiceLocation _serviceLocation;
        private readonly IConfiguration _configuration;

        public HomeController(
            SignInManager<WikiUser> signInManager,
            ILoggerFactory loggerFactory,
            WikiDbContext context,
            Seeder seeder,
            ServiceLocation serviceLocation,
            IConfiguration configuration)
        {
            _signInManager = signInManager;
            _logger = loggerFactory.CreateLogger<HomeController>();
            _dbContext = context;
            _seeder = seeder;
            _serviceLocation = serviceLocation;
            _configuration = configuration;
        }

        [AiurForceAuth(preferController: "Home", preferAction: "Index", justTry: true)]
        public async Task<IActionResult> Index()//Title
        {
            var firstArticle = await _dbContext.Article.Include(t => t.Collection).FirstAsync();
            return Redirect($"/ReadDoc/{firstArticle.Collection.CollectionTitle}/{firstArticle.ArticleTitle}.md");
        }

        [Route(template: "/ReadDoc/{collectionTitle}/{articleTitle}.md")]
        public async Task<IActionResult> ReadDoc(string collectionTitle, string articleTitle)
        {
            var database = await _dbContext.Collections.Include(t => t.Articles).ToListAsync();
            var currentCollection = database.SingleOrDefault(t => t.CollectionTitle.ToLower() == collectionTitle.ToLower());
            if (currentCollection == null)
            {
                return NotFound();
            }
            var currentArticle = currentCollection.Articles.SingleOrDefault(t => t.ArticleTitle.ToLower() == articleTitle.ToLower());
            if (currentArticle == null)
            {
                return NotFound();
            }
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();
            var model = new WikiViewModel
            {
                Collections = database,
                CurrentCollection = currentCollection,
                CurrentArticle = currentArticle,
                RenderedContent = Markdown.ToHtml(currentArticle.ArticleContent, pipeline)
            };
            return View(model);
        }

        public async Task<IActionResult> ToJson()
        {
            var database = await _dbContext.Collections.Include(t => t.Articles).ToListAsync();
            return Json(database);
        }

        [HttpPost]
        public async Task<IActionResult> Seed(string secret)
        {
            var secretInConfig = _configuration["ContentUpdateSecret"];
            if (!string.Equals(secret, secretInConfig) || string.IsNullOrWhiteSpace(secretInConfig))
            {
                return NotFound();
            }
            if (_seeder.Seeding)
            {
                return this.Protocol(ErrorType.Pending, $"Seeding...");
            }
            await _seeder.Seed();
            return Json(new AiurProtocol
            {
                Code = ErrorType.Success,
                Message = "Seeded"
            });
        }

        [HttpPost]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            return this.SignOutRootServer(_serviceLocation.API, new AiurUrl(string.Empty, "Home", nameof(HomeController.Index), new { }));
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
