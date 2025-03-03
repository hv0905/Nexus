﻿using Aiursoft.EE.Data;
using Aiursoft.EE.Models;
using Aiursoft.EE.Models.CourseViewModels;
using Aiursoft.Pylon;
using Aiursoft.Pylon.Attributes;
using Aiursoft.Pylon.Models;
using Aiursoft.Pylon.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aiursoft.EE.Controllers
{
    [LimitPerMin]
    public class CourseController : Controller
    {
        private readonly UserManager<EEUser> _userManager;
        private readonly SignInManager<EEUser> _signInManager;
        private readonly EEDbContext _dbContext;
        private readonly ServiceLocation _serviceLocation;
        private readonly ScriptsFilter _scriptsFilter;

        public CourseController(
            UserManager<EEUser> userManager,
            SignInManager<EEUser> signInManager,
            EEDbContext dbContext,
            ServiceLocation serviceLocation,
            ScriptsFilter scriptsFilter)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _dbContext = dbContext;
            _serviceLocation = serviceLocation;
            _scriptsFilter = scriptsFilter;
        }

        [AiurForceAuth]
        public IActionResult Create()
        {
            return View(new CreateViewModel());
        }

        [HttpPost]
        [AiurForceAuth]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateViewModel model)
        {
            var user = await GetCurrentUserAsync();
            if (!ModelState.IsValid)
            {
                model.ModelStateValid = false;
                return View(model);
            }
            var course = new Course
            {
                Description = _scriptsFilter.Filt(model.Description),
                CourseImage = $"{_serviceLocation.UI}/images/thumbnail.svg",
                DisplayOwnerInfo = model.DisplayOwnerInfo,
                WhatYouWillLearn = model.WhatYouWillLearn,
                Name = model.Name,
                Price = model.Price,
                OwnerId = user.Id
            };
            _dbContext.Courses.Add(course);
            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Detail), new { id = course.Id });
        }

        //For users who did not sign in but clicked subscribe
        [AiurForceAuth]
        public IActionResult DetailAuth(int id)// Course id
        {
            return RedirectToAction(nameof(Detail), new { id = id });
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id)//Course Id
        {
            var course = await _dbContext
                .Courses
                .Include(t => t.Owner)
                .Include(t => t.Sections)
                .SingleOrDefaultAsync(t => t.Id == id);
            var allChapters = await _dbContext
                .Chapters
                .Include(t => t.Context)
                .Where(t => t.Context.CourseId == course.Id)
                .ToListAsync();
            var user = await GetCurrentUserAsync();
            var Subscribed = user == null ? false : await _dbContext
                .Subscriptions
                .SingleOrDefaultAsync(t => t.CourseId == id && t.UserId == user.Id) != null;

            if (course == null)
            {
                return NotFound();
            }
            var model = new DetailViewModel
            {
                Id = id,
                TeacherDescription = course.Owner.LongDescription,
                Name = course.Name,
                Description = course.Description,
                Subscribed = Subscribed,
                IsOwner = user?.Id == course.OwnerId,
                AuthorName = course.Owner.NickName,
                DisplayOwnerInfo = course.DisplayOwnerInfo,
                Sections = course.Sections,
                WhatYouWillLearn = course.WhatYouWillLearn
            };
            return View(model);
        }

        [HttpPost]
        [AiurForceAuth("", "", false)]
        [APIExpHandler]
        [APIModelStateChecker]
        public async Task<IActionResult> Subscribe(int id)//Course Id
        {
            var user = await GetCurrentUserAsync();
            var course = await _dbContext
                .Courses
                .SingleOrDefaultAsync(t => t.Id == id);
            if (course == null)
            {
                return this.Protocol(ErrorType.NotFound, $"The target course with Id:{id} was not found!");
            }

            var sub = await _dbContext
                .Subscriptions
                .SingleOrDefaultAsync(t => t.CourseId == id && user.Id == user.Id);

            if (sub == null)
            {
                var newSubscription = new Subscription
                {
                    UserId = user.Id,
                    CourseId = id
                };
                _dbContext.Subscriptions.Add(newSubscription);
                await _dbContext.SaveChangesAsync();
                return this.Protocol(ErrorType.Success, "You have successfully subscribed this course!");
            }
            return this.Protocol(ErrorType.HasDoneAlready, "This course you have already subscribed!");
        }

        [HttpPost]
        [AiurForceAuth("", "", false)]
        [APIExpHandler]
        [APIModelStateChecker]
        public async Task<IActionResult> UnSubscribe(int id)//Course Id
        {
            var user = await GetCurrentUserAsync();
            var course = await _dbContext
                .Courses
                .SingleOrDefaultAsync(t => t.Id == id);
            if (course == null)
            {
                return this.Protocol(ErrorType.NotFound, $"The target course with Id:{id} was not found!");
            }
            var sub = await _dbContext
                .Subscriptions
                .SingleOrDefaultAsync(t => t.CourseId == id && user.Id == user.Id);

            if (sub != null)
            {
                _dbContext.Subscriptions.Remove(sub);
                await _dbContext.SaveChangesAsync();
                return this.Protocol(ErrorType.Success, "Successfully unsubscribed this course!");
            }
            return this.Protocol(ErrorType.HasDoneAlready, "You did not subscribe this course!");
        }

        private async Task<EEUser> GetCurrentUserAsync()
        {
            return await _userManager.GetUserAsync(User);
        }
    }
}
