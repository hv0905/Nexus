﻿using Aiursoft.Pylon;
using Aiursoft.Pylon.Attributes;
using Aiursoft.Pylon.Models;
using Aiursoft.Pylon.Services.ToOSSServer;
using Aiursoft.Developer.Data;
using Aiursoft.Developer.Models;
using Aiursoft.Developer.Models.FilesViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Aiursoft.Pylon.Services;
using Aiursoft.Pylon.Models.Developer;
using Aiursoft.Pylon.Exceptions;

namespace Aiursoft.Developer.Controllers
{
    [AiurForceAuth]
    [LimitPerMin]
    public class FilesController : Controller
    {
        private readonly UserManager<DeveloperUser> _userManager;
        private readonly SignInManager<DeveloperUser> _signInManager;
        private readonly ILogger _logger;
        private readonly DeveloperDbContext _dbContext;
        private readonly OSSApiService _ossApiService;
        private readonly StorageService _storageService;
        private readonly AppsContainer _appsContainer;
        private readonly SecretService _secretService;

        public FilesController(
            UserManager<DeveloperUser> userManager,
            SignInManager<DeveloperUser> signInManager,
            ILoggerFactory loggerFactory,
            DeveloperDbContext dbContext,
            OSSApiService ossApiService,
            StorageService storageService,
            AppsContainer appsContainer,
            SecretService secretService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = loggerFactory.CreateLogger<FilesController>();
            _dbContext = dbContext;
            _ossApiService = ossApiService;
            _storageService = storageService;
            _appsContainer = appsContainer;
            _secretService = secretService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserAsync();
            var model = new IndexViewModel(user);
            return View(model);
        }

        public async Task<IActionResult> ViewFiles(int id)// Bucket Id
        {
            var cuser = await GetCurrentUserAsync();
            var bucketInfo = await _ossApiService.ViewBucketDetailAsync(id);
            if (bucketInfo.BelongingAppId == null)
            {
                return NotFound();
            }
            var app = await _dbContext.Apps.FindAsync(bucketInfo.BelongingAppId);
            var files = await _ossApiService.ViewAllFilesAsync(await _appsContainer.AccessToken(app.AppId, app.AppSecret), id);
            var model = new ViewFilesViewModel(cuser)
            {
                BucketId = files.BucketId,
                AllFiles = files.AllFiles,
                AppId = app.AppId,
                OpenToRead = bucketInfo.OpenToRead,
                BucketName = bucketInfo.BucketName
            };
            return View(model);
        }

        public async Task<IActionResult> GenerateLink(int id) // file Id
        {
            var cuser = await GetCurrentUserAsync();
            var fileinfo = await _ossApiService.ViewOneFileAsync(id);
            if (fileinfo.File == null)
            {
                return NotFound();
            }
            var bucketInfo = await _ossApiService.ViewBucketDetailAsync(fileinfo.File.BucketId);
            var model = new GenerateViewModel(cuser)
            {
                FileId = fileinfo.File.FileKey,
                FileName = fileinfo.File.RealFileName,
                BucketId = bucketInfo.BucketId
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ViewLink(GenerateViewModel input) // file Id
        {
            var cuser = await GetCurrentUserAsync();
            var fileinfo = await _ossApiService.ViewOneFileAsync(input.FileId);
            if (fileinfo.File == null)
            {
                return NotFound();
            }
            var bucketInfo = await _ossApiService.ViewBucketDetailAsync(fileinfo.File.BucketId);
            var app = await _dbContext.Apps.FindAsync(bucketInfo.BelongingAppId);
            var secret = await _secretService.GenerateAsync(input.FileId, await _appsContainer.AccessToken(app.AppId, app.AppSecret), input.AccessTimes);
            var model = new ViewLinkViewModel(cuser)
            {
                Address = secret.Value,
                BucketId = bucketInfo.BucketId
            };
            return View(model);
        }

        public async Task<IActionResult> DeleteFile(int id)
        {
            var cuser = await GetCurrentUserAsync();
            var fileinfo = await _ossApiService.ViewOneFileAsync(id);
            var bucketInfo = await _ossApiService.ViewBucketDetailAsync(fileinfo.File.BucketId);
            var app = await _dbContext.Apps.FindAsync(bucketInfo.BelongingAppId);

            if (bucketInfo.BelongingAppId != app.AppId)
            {
                return Unauthorized();
            }
            var model = new DeleteFileViewModel(cuser)
            {
                FileName = fileinfo.File.RealFileName,
                FileId = fileinfo.File.FileKey,
                BucketId = fileinfo.File.BucketId
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFile(DeleteFileViewModel model)
        {
            var cuser = await GetCurrentUserAsync();
            if (!ModelState.IsValid)
            {
                model.RootRecover(cuser, 3);
                return View(model);
            }
            var fileinfo = await _ossApiService.ViewOneFileAsync(model.FileId);
            var bucketInfo = await _ossApiService.ViewBucketDetailAsync(fileinfo.File.BucketId);
            var app = await _dbContext.Apps.FindAsync(bucketInfo.BelongingAppId);

            if (fileinfo == null || bucketInfo.BelongingAppId != app.AppId || fileinfo.File.BucketId != bucketInfo.BucketId)
            {
                return Unauthorized();
            }
            await _ossApiService.DeleteFileAsync(await _appsContainer.AccessToken(app.AppId, app.AppSecret), model.FileId);
            return RedirectToAction(nameof(ViewFiles), new
            {
                id = bucketInfo.BucketId
            });
        }

        public async Task<IActionResult> UploadFile(int id)// Bucket Id
        {
            var cuser = await GetCurrentUserAsync();
            var bucket = await _ossApiService.ViewBucketDetailAsync(id);
            var viewModel = new UploadFileViewModel(cuser)
            {
                BucketId = bucket.BucketId,
                AppId = bucket.BelongingAppId,
                BucketName = bucket.BucketName,
            };
            return View(viewModel);
        }

        [HttpPost]
        [FileChecker]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(UploadFileViewModel model)
        {
            var cuser = await GetCurrentUserAsync();
            if (!ModelState.IsValid)
            {
                model.RootRecover(cuser, 3);
                model.ModelStateValid = false;
                return View(model);
            }
            var app = await _dbContext.Apps.FindAsync(model.AppId);
            string accessToken = await _appsContainer.AccessToken(app.AppId, app.AppSecret);
            var file = Request.Form.Files.First();
            try
            {
                await _storageService.SaveToOSS(file, model.BucketId, model.AliveDays, SaveFileOptions.SourceName, accessToken);
            }
            catch (AiurUnexceptedResponse e) when (e.Code == ErrorType.HasDoneAlready)
            {
                ModelState.AddModelError(string.Empty, e.Response.Message);
                model.ModelStateValid = false;
                return View(model);
            }
            return RedirectToAction(nameof(ViewFiles), new { id = model.BucketId });
        }

        private async Task<DeveloperUser> GetCurrentUserAsync()
        {
            return await _dbContext.Users.Include(t => t.MyApps).SingleOrDefaultAsync(t => t.UserName == User.Identity.Name);
        }
    }
}
